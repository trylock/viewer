using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.IO;
using Viewer.Core.Collections;

namespace Viewer.Data.SQLite
{
    /// <summary>
    /// Basically a histogram of attribute name subsets
    /// </summary>
    public class AttributeSubtreeStatistics
    {
        /// <summary>
        /// Index of the empty subset. <see cref="AttributeSubsets"/> will always contain an empty
        /// subset.
        /// </summary>
        public const int EmptySubsetIndex = 0;

        private readonly List<long> _subsetCount = new List<long>();

        public SubsetCollection<string> AttributeSubsets { get; } = new SubsetCollection<string>();

        public AttributeSubtreeStatistics()
        {
            // empty subset is always at index 0
            AttributeSubsets.Add(Enumerable.Empty<string>());
        }

        public long GetSubsetCount(int index)
        {
            if (index < 0 || index >= _subsetCount.Count)
            {
                return 0;
            }
            return _subsetCount[index];
        }

        public void AddSubset(IEnumerable<string> subset, long count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            var index = AttributeSubsets.Add(subset);

            // make sure there is enough space in the subsetCount array
            for (var i = _subsetCount.Count; i < AttributeSubsets.Count; ++i)
            {
                _subsetCount.Add(0);
            }

            // increment count of this subset
            _subsetCount[index] += count;
        }
    }

    public interface IAttributeStatistics : IDisposable
    {
        /// <summary>
        /// Get statistics for the directory subtree rooted at <paramref name="path"/>
        /// </summary>
        /// <param name="path">Path to a directory</param>
        /// <returns>
        /// Statistics for directory subtree rooted at <paramref name="path"/>. If there are no
        /// statistics for this subtree, null will be returned.
        /// </returns>
        AttributeSubtreeStatistics GetStatistics(string path);
    }

    public interface IAttributeStatisticsFactory
    {
        /// <summary>
        /// Fetch attribute statistics for <paramref name="attributeNames"/>
        /// </summary>
        /// <param name="attributeNames">Names of attributes</param>
        /// <returns>Statistics for <paramref name="attributeNames"/></returns>
        IAttributeStatistics Create(IEnumerable<string> attributeNames);
    }
    
    public class AttributeStatistics : IAttributeStatistics
    {
        private readonly SQLiteConnection _connection;
        private readonly FileDistributionCommand _fetchCommand;

        private Dictionary<string, AttributeSubtreeStatistics> _statistics;
        
        public AttributeStatistics(SQLiteConnection connection, IEnumerable<string> attributeNames)
        {
            var attrs = BindAttributeNames(attributeNames);
            _connection = connection;
            _fetchCommand = new FileDistributionCommand(_connection, attrs.Snippet, attrs.Parameters);
        }

        private class FileDistributionCommand : IDisposable
        {
            private readonly SQLiteCommand _command;
            private bool _hasNames;

            public FileDistributionCommand(
                SQLiteConnection connection, 
                string namesSnippet, 
                SQLiteParameter[] names)
            {
                _hasNames = names.Length > 0;
                _command = connection.CreateCommand();
                _command.Parameters.AddRange(names);
                _command.CommandText = @"
                    WITH RECURSIVE directories AS (
                        -- compute aggregations for top-level folders (i.e., folders which contain photos)
                        SELECT
                            getParentPath(path) AS path, 
                            group_concat(name) AS name
                        FROM (
                            SELECT
                                f.path AS path, 
                                a.name AS name
                            FROM attributes AS a
                                INNER JOIN files AS f
                                    ON (a.file_id = f.id)
                            WHERE a.source = 0 AND (" + namesSnippet + @")
                            ORDER BY a.name
                        )
                        GROUP BY path

                        UNION ALL
        
                        -- compute aggregations for all parent folders
                        SELECT getParentPath(d.path) AS path, d.name
                        FROM directories AS d
                        WHERE getParentPath(d.path) IS NOT NULL AND
                              getParentPath(d.path) <> d.path
                    )

                    SELECT path, name, count(*) AS count
                    FROM directories
                    GROUP BY path, name
                ";
            }

            public IEnumerable<(string Path, string Group, long Count)> Execute()
            {
                if (!_hasNames)
                {
                    yield break;
                }

                using (var reader = _command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dir = reader.GetString(0);
                        var group = reader.GetString(1);
                        var count = reader.GetInt64(2);

                        yield return (dir, group, count);
                    }
                }
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }
        
        private static (SQLiteParameter[] Parameters, string Snippet) 
            BindAttributeNames(IEnumerable<string> names)
        {
            var parameters = new List<SQLiteParameter>();
            var sb = new StringBuilder();
            var index = 0;
            foreach (var name in names)
            {
                if (index > 0)
                {
                    sb.Append(" OR ");
                }

                sb.Append("a.name = :param");
                sb.Append(index);

                // add the parameter
                parameters.Add(new SQLiteParameter(":param" + index, name));

                ++index;
            }

            return (parameters.ToArray(), sb.ToString());
        }
        
        public void Dispose()
        {
            _fetchCommand?.Dispose();
            _connection?.Dispose();
        }

        public AttributeSubtreeStatistics GetStatistics(string path)
        {
            if (_statistics == null)
            {
                // fetch the distribution to main memory
                _statistics = new Dictionary<string, AttributeSubtreeStatistics>(
                    StringComparer.CurrentCultureIgnoreCase);
                foreach (var item in _fetchCommand.Execute())
                {
                    if (!_statistics.TryGetValue(item.Path, out var statistics))
                    {
                        statistics = new AttributeSubtreeStatistics();
                        _statistics.Add(item.Path, statistics);
                    }

                    if (string.IsNullOrEmpty(item.Group))
                    {
                        statistics.AddSubset(Enumerable.Empty<string>(), item.Count);
                    }
                    else
                    {
                        statistics.AddSubset(item.Group.Split(','), item.Count);
                    }
                }

                // there is no point in keeping the connection open once we have the data
                Dispose();
            }

            if (!_statistics.TryGetValue(path, out var result))
            {
                return null;
            }

            return result;
        }
    }

    [Export(typeof(IAttributeStatisticsFactory))]
    public class AttributeStatisticsFactory : IAttributeStatisticsFactory
    {
        private readonly SQLiteConnectionFactory _connectionFactory;

        [ImportingConstructor]
        public AttributeStatisticsFactory(SQLiteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IAttributeStatistics Create(IEnumerable<string> attributeNames)
        {
            return new AttributeStatistics(_connectionFactory.Create(), attributeNames);
        }
    }
}
