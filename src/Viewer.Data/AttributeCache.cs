using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.SQLite;
using Viewer.IO;

namespace Viewer.Data
{
    public class AttributeGroup
    {
        /// <summary>
        /// File path where <see cref="Attributes"/> have been seen.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// List of attributes in this file
        /// </summary>
        public List<string> Attributes { get; set; } = new List<string>();
    }
    
    public interface IFileAggregate : IDisposable
    {
        /// <summary>
        /// Count files in directories which start with <paramref name="path"/>
        /// </summary>
        /// <param name="path">Prefix of a file name</param>
        /// <returns>
        /// Number of files in directories which start with <paramref name="path"/>
        /// </returns>
        long Count(string path);
    }

    /// <summary>
    /// AttributeCache keeps track of recently seen attributes and their values.
    /// </summary>
    public interface IAttributeCache
    {
        /// <summary>
        /// Get all attribute names which start with <paramref name="namePrefix"/>.
        /// </summary>
        /// <param name="namePrefix">
        /// Prefix of an attribute name. It must not be null but it may be an empty string.
        /// </param>
        /// <returns>Attribute names which start with <paramref name="namePrefix"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="namePrefix"/> is null</exception>
        IEnumerable<string> GetNames(string namePrefix);

        /// <summary>
        /// Get all values of attribute named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of an attribute</param>
        /// <returns>Available values of attribute named <paramref name="name"/></returns>
        IEnumerable<BaseValue> GetValues(string name);

        /// <summary>
        /// This function will find files when an attribute from the
        /// <paramref name="attributeNames"/> list has been seen.
        /// </summary>
        /// <param name="attributeNames">Names of attributes</param>
        /// <returns>
        /// Attribute group with attributes named <paramref name="attributeNames"/>
        /// </returns>
        IEnumerable<AttributeGroup> GetAttributes(IReadOnlyList<string> attributeNames);

        /// <summary>
        /// Create an object which can be queried for file count in some directories.
        /// </summary>
        /// <returns>File Aggregate object</returns>
        IFileAggregate CreateFileAggregate();
    }

    [Export(typeof(IAttributeCache))]
    public class AttributeCache : IAttributeCache
    {
        private readonly SQLiteConnectionFactory _connectionFactory;

        [ImportingConstructor]
        public AttributeCache(SQLiteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IEnumerable<string> GetNames(string namePrefix)
        {
            if (namePrefix == null)
                throw new ArgumentNullException(nameof(namePrefix));

            return GetNamesImpl(namePrefix);
        }

        private IEnumerable<string> GetNamesImpl(string namePrefix)
        {
            using (var connection = _connectionFactory.Create())
            using (var command = new SQLiteCommand(connection))
            {
                // "source = 0" means custom (user) attributes
                command.CommandText = @"
                SELECT DISTINCT name 
                FROM attributes 
                WHERE source = 0 AND name LIKE :name
                ORDER BY name";
                command.Parameters.Add(new SQLiteParameter(":name", namePrefix + '%'));

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        yield return name;
                    }
                }
            }
        }

        public IEnumerable<AttributeGroup> GetAttributes(IReadOnlyList<string> names)
        {
            // create a SQL expression "(a.name = :param0 OR a.name = :param1 OR ...)"
            var attributeNameSnippet = new StringBuilder();
            attributeNameSnippet.Append('(');
            for (var i = 0; i < names.Count; ++i)
            {
                attributeNameSnippet.Append("a.name = :param");
                attributeNameSnippet.Append(i);

                if (i + 1 < names.Count)
                {
                    attributeNameSnippet.Append(" OR ");
                }
            }
            attributeNameSnippet.Append(')');
            
            // find files for each attribute 
            using (var connection = _connectionFactory.Create())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                SELECT a.name, f.path
                FROM attributes AS a
                    INNER JOIN files AS f 
                        ON (f.id = a.owner)
                WHERE a.source = 0 AND " + attributeNameSnippet + @"
                ORDER BY f.path";

                for (var i = 0; i < names.Count; ++i)
                {
                    command.Parameters.Add(new SQLiteParameter(":param" + i, names[i]));
                }
                
                using (var reader = command.ExecuteReader())
                {
                    var group = new AttributeGroup();
                    while (reader.Read())
                    {
                        var path = reader.GetString(1);
                        if (group.FilePath != null && group.FilePath != path) 
                        {
                            // this row belongs to a new group
                            // return the previous group and begin a new one
                            yield return group;
                            group = new AttributeGroup();
                        }

                        // add this attribute name to current group
                        var name = reader.GetString(0);
                        group.FilePath = path;
                        group.Attributes.Add(name);
                    }

                    // return the last group 
                    if (group.FilePath != null)
                    {
                        yield return group;
                    }
                }
            }
        }

        private class FileAggregate : IFileAggregate
        {
            private readonly SQLiteConnection _connection;
            private readonly SQLiteCommand _command;
            private readonly SQLiteParameter _prefix;

            public FileAggregate(SQLiteConnection connection)
            {
                _connection = connection;
                _prefix = new SQLiteParameter(":prefix");
                _command = _connection.CreateCommand();
                _command.CommandText = "SELECT COUNT(*) FROM files WHERE path LIKE :prefix";
                _command.Parameters.Add(_prefix);
            }

            public long Count(string path)
            {
                _prefix.Value = path + '%';
                return (long) _command.ExecuteScalar();
            }

            public void Dispose()
            {
                _connection?.Dispose();
                _command?.Dispose();
            }
        }

        public IFileAggregate CreateFileAggregate()
        {
            return new FileAggregate(_connectionFactory.Create());
        }
        
        public IEnumerable<BaseValue> GetValues(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return GetValuesImpl(name);
        }

        public IEnumerable<BaseValue> GetValuesImpl(string name)
        {
            using (var connection = _connectionFactory.Create())
            using (var command = new SQLiteCommand(connection))
            {
                // "source = 0" means custom (user) attributes
                command.CommandText = @"
                SELECT DISTINCT value, type 
                FROM attributes 
                WHERE source = 0 AND name = :name
                ORDER BY value, type";
                command.Parameters.Add(new SQLiteParameter(":name", name));

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var type = (AttributeType) reader.GetInt32(1);
                        switch (type)
                        {
                            case AttributeType.Int:
                                yield return new IntValue(reader.GetInt32(0));
                                break;
                            case AttributeType.Double:
                                yield return new RealValue(reader.GetDouble(0));
                                break;
                            case AttributeType.String:
                                yield return new StringValue(reader.GetString(0));
                                break;
                            case AttributeType.DateTime:
                                yield return new DateTimeValue(reader.GetDateTime(0));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }
    }
}
