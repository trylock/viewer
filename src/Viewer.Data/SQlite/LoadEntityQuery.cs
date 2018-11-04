using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats.Attributes;

namespace Viewer.Data.SQLite
{
    /// <summary>
    /// Load entity from the cache database
    /// </summary>
    /// <remarks>
    /// The Dispose method will dispose both the sqlite command and its connection.
    /// </remarks>
    public class LoadEntityQuery : IDisposable
    {
        private readonly SQLiteConnection _connection;
        private readonly SQLiteParameter _path = new SQLiteParameter(":path");
        private readonly SQLiteParameter _lastWriteTime = new SQLiteParameter(":lastWriteTime");
        private readonly SQLiteCommand _command;

        public LoadEntityQuery(SQLiteConnection connection)
        {
            _connection = connection;
            _command = connection.CreateCommand();
            _command.CommandText = @"
                    SELECT a.name, a.source, a.type, a.value, length(a.value) as size
                    FROM files AS f
                        INNER JOIN attributes AS a 
                            ON (f.id = a.file_id)
                    WHERE 
                        f.path = :path AND
                        f.last_file_write_time >= :lastWriteTime";
            _command.Parameters.Add(_path);
            _command.Parameters.Add(_lastWriteTime);
        }

        public SQLiteDataReader Execute(string path, DateTime lastWriteTime)
        {
            _path.Value = path;
            _lastWriteTime.Value = lastWriteTime.ToUniversalTime();
            return _command.ExecuteReader();
        }

        public IEnumerable<Attribute> Fetch(string path, DateTime lastWriteTime)
        {
            using (var reader = Execute(path, lastWriteTime))
            {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var source = reader.GetInt32(1);
                    var type = reader.GetInt32(2);
                    var valueSize = reader.GetInt64(4);

                    // parse value
                    BaseValue value = null;
                    switch ((AttributeType)type)
                    {
                        case AttributeType.Int:
                            value = new IntValue(reader.GetInt32(3));
                            break;
                        case AttributeType.Double:
                            value = new RealValue(reader.GetDouble(3));
                            break;
                        case AttributeType.String:
                            value = new StringValue(reader.GetString(3));
                            break;
                        case AttributeType.DateTime:
                            value = new DateTimeValue(reader.GetDateTime(3));
                            break;
                        case AttributeType.Image:
                            var buffer = new byte[valueSize];
                            var length = reader.GetBytes(3, 0, buffer, 0, buffer.Length);
                            Trace.Assert(buffer.Length == length);
                            value = new ImageValue(buffer);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // return the attribute
                    yield return new Attribute(name, value, (AttributeSource)source);
                }
            }
        }

        public void Dispose()
        {
            _command?.Dispose();
            _connection?.Dispose();
        }
    }
}
