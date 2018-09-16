using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.SQLite;

namespace Viewer.Data
{
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
