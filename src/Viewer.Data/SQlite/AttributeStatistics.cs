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
    public class AttributeSubtreeStatistics
    {
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
        /// Fetch subset counts for all folders in the subtree rooted at <paramref name="path"/>
        /// </summary>
        /// <param name="path">Path to the root of a subtree</param>
        /// <returns>Map directory names to their subset counts</returns>
        Dictionary<string, AttributeSubtreeStatistics> IndexSubsetCounts(string path);
    }

    public interface IAttributeStatisticsFactory
    {
        /// <summary>
        /// Create attribute statistics service for <paramref name="attributeNames"/>
        /// </summary>
        /// <param name="attributeNames">Names of custom (user) attributes in the query</param>
        /// <returns>Attribute statistics for given attribute names</returns>
        IAttributeStatistics Create(IEnumerable<string> attributeNames);
    }

    public class AttributeStatistics : IAttributeStatistics
    {
        private readonly SQLiteConnection _connection;
        private readonly FolderAttributesCommand _countSubsetsCommand;

        public AttributeStatistics(SQLiteConnection connection, IEnumerable<string> attributeNames)
        {
            var names = BindAttributeNames(attributeNames);
            _connection = connection;
            _countSubsetsCommand = new FolderAttributesCommand(
                _connection, 
                names.Snippet, 
                names.Parameters);
        }

        private class FolderAttributesCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteCommand _command;

            public FolderAttributesCommand(
                SQLiteConnection connection, 
                string namesSnippet, 
                SQLiteParameter[] names)
            {
                _command = connection.CreateCommand();
                _command.Parameters.AddRange(names);
                _command.Parameters.Add(_path);
                _command.CommandText = @"
                    with recursive directories as (
                        -- compute aggregations for top-level folders (i.e., folders which contain photos)
                        select 
                            getParentPath(path) as path, 
                            group_concat(name) as name
                        from (
                            select 
                                fc.path as path, 
                                a.name as name
                            from files as f
                                inner join files_closure as c
                                    on (c.parent_id = f.id)
                                inner join attributes as a
                                    on (a.file_id = c.child_id)
                                inner join files as fc
                                    on (fc.id = c.child_id)
                            where f.path = :path and a.source = 0 and (" + namesSnippet + @")
                            order by a.name
                        )
                        group by path

                        union all
        
                        -- compute aggregations for all parent folders
                        select getParentPath(d.path) as path, d.name
                        from directories as d
                        where getParentPath(d.path) is not null and 
                              getParentPath(d.path) <> d.path
                    )
                    select path, name, count(*) as count
                    from directories
                    group by path, name
                ";
            }

            public IEnumerable<(string Path, string Group, long Count)> Execute(string path)
            {
                _path.Value = path;

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
                    sb.Append(" or ");
                }

                sb.Append("a.name = :param");
                sb.Append(index);

                // add the parameter
                parameters.Add(new SQLiteParameter(":param" + index, name));

                ++index;
            }

            return (parameters.ToArray(), sb.ToString());
        }

        public Dictionary<string, AttributeSubtreeStatistics> IndexSubsetCounts(string path)
        {
            var index = new Dictionary<string, AttributeSubtreeStatistics>(
                StringComparer.CurrentCultureIgnoreCase);

            foreach (var item in _countSubsetsCommand.Execute(path))
            {
                if (!index.TryGetValue(item.Path, out var statistics))
                {
                    statistics = new AttributeSubtreeStatistics();
                    index.Add(item.Path, statistics);
                }

                statistics.AddSubset(item.Group.Split(','), item.Count);
            }
            
            return index;
        }

        public void Dispose()
        {
            _countSubsetsCommand?.Dispose();
            _connection?.Dispose();
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
