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

    public interface IAttributeStatistics 
    {
        /// <summary>
        /// Fetch subset counts for all folders which contain a file with an attribute from
        /// <paramref name="attributeNames"/>
        /// </summary>
        /// <param name="attributeNames">Names of attributes to fetch</param>
        /// <returns>Map directory names to their subset counts</returns>
        Dictionary<string, AttributeSubtreeStatistics> GetSubsetCounts(
            IEnumerable<string> attributeNames);
    }

    [Export(typeof(IAttributeStatistics))]
    public class AttributeStatistics : IAttributeStatistics
    {
        private readonly SQLiteConnectionFactory _connectionFactory;

        [ImportingConstructor]
        public AttributeStatistics(SQLiteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private class FolderAttributesCommand : IDisposable
        {
            private readonly SQLiteCommand _command;

            public FolderAttributesCommand(
                SQLiteConnection connection, 
                string namesSnippet, 
                SQLiteParameter[] names)
            {
                _command = connection.CreateCommand();
                _command.Parameters.AddRange(names);
                _command.CommandText = @"
                    with recursive directories as (
                        -- compute aggregations for top-level folders (i.e., folders which contain photos)
                        select 
                            getParentPath(path) as path, 
                            group_concat(name) as name
                        from (
                            select 
                                f.path as path, 
                                a.name as name
                            from attributes as a
                                inner join files as f
                                    on (a.file_id = f.id)
                            where a.source = 0 and (" + namesSnippet + @")
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

            public IEnumerable<(string Path, string Group, long Count)> Execute()
            {
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

        public Dictionary<string, AttributeSubtreeStatistics> GetSubsetCounts(IEnumerable<string> names)
        {
            var attrs = BindAttributeNames(names);
            using (var connection = _connectionFactory.Create())
            using (var command = new FolderAttributesCommand(connection, attrs.Snippet, attrs.Parameters))
            {
                var index = new Dictionary<string, AttributeSubtreeStatistics>(
                    StringComparer.CurrentCultureIgnoreCase);

                foreach (var item in command.Execute())
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
        }
    }
}
