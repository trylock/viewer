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
    /// This service provides access to the table of attributes.
    /// </summary>
    public interface IAttributes : IDisposable
    {
        /// <summary>
        /// Add a new attribute to the file <paramref name="file"/>
        /// </summary>
        /// <param name="attribute">Added attribute</param>
        /// <param name="file">ID of a file</param>
        void Insert(Attribute attribute, long file);
        
        /// <summary>
        /// Set thumbnail attribute of <paramref name="file"/>
        /// </summary>
        /// <param name="thumbnail">Encoded thumbnail of <paramref name="file"/></param>
        /// <param name="file">ID of a file</param>
        void SetThumbnail(byte[] thumbnail, long file);

        /// <summary>
        /// Find all attributes in file <paramref name="path"/>
        /// </summary>
        /// <param name="path">Full path to a file</param>
        /// <param name="lastWriteTime">
        /// The last write time to the file. This is used to determine whether the records in the
        /// database are valid.
        /// </param>
        /// <returns>Collection of attributes</returns>
        IEnumerable<Attribute> FindAttributes(string path, DateTime lastWriteTime);
    }

    public class Attributes : IAttributes
    {
        private readonly SQLiteConnection _connection;
        private readonly InsertAttributeCommand _insertAttribute;
        private readonly UpdateThumbnailCommand _updateThumbnail;
        private readonly LoadEntityCommand _loadEntity;

        public Attributes(SQLiteConnection connection)
        {
            _connection = connection;
            _insertAttribute = new InsertAttributeCommand(_connection);
            _updateThumbnail = new UpdateThumbnailCommand(_connection);
            _loadEntity = new LoadEntityCommand(_connection);
        }

        private class InsertAttributeCommand : IValueVisitor, IDisposable
        {
            private readonly SQLiteParameter _name = new SQLiteParameter(":name");
            private readonly SQLiteParameter _type = new SQLiteParameter(":type");
            private readonly SQLiteParameter _source = new SQLiteParameter(":source");
            private readonly SQLiteParameter _value = new SQLiteParameter(":value");
            private readonly SQLiteParameter _owner = new SQLiteParameter(":owner");

            private readonly SQLiteCommand _command;

            public InsertAttributeCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "INSERT INTO attributes (name, source, type, value, file_id)" +
                                       "VALUES (:name, :source, :type, :value, :owner)";
                _command.Parameters.Add(_name);
                _command.Parameters.Add(_type);
                _command.Parameters.Add(_source);
                _command.Parameters.Add(_value);
                _command.Parameters.Add(_owner);
            }

            public void Execute(Attribute attr, long owner)
            {
                _name.Value = attr.Name;
                _type.Value = (int)attr.Value.Type;
                _source.Value = (int)attr.Source;
                _owner.Value = owner;
                _value.ResetDbType();
                attr.Value.Accept(this);
                _command.ExecuteNonQuery();
            }

            void IValueVisitor.Visit(IntValue attr)
            {
                _value.Value = attr.Value;
            }

            void IValueVisitor.Visit(RealValue attr)
            {
                _value.Value = attr.Value;
            }

            void IValueVisitor.Visit(StringValue attr)
            {
                _value.Value = attr.Value;
            }

            void IValueVisitor.Visit(DateTimeValue attr)
            {
                _value.Value = attr.Value?.ToString(DateTimeValue.Format);
            }

            void IValueVisitor.Visit(ImageValue attr)
            {
                _value.Value = attr.Value;
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        private class UpdateThumbnailCommand : IDisposable
        {
            private readonly SQLiteParameter _source = new SQLiteParameter(":source");
            private readonly SQLiteParameter _type = new SQLiteParameter(":type");
            private readonly SQLiteParameter _value = new SQLiteParameter(":value");
            private readonly SQLiteParameter _owner = new SQLiteParameter(":owner");
            private readonly SQLiteCommand _command;

            public UpdateThumbnailCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = @" 
                INSERT OR REPLACE INTO attributes (name, source, type, value, file_id)
                VALUES('thumbnail', :source, :type, :value, :owner)";
                _command.Parameters.Add(_source);
                _command.Parameters.Add(_type);
                _command.Parameters.Add(_value);
                _command.Parameters.Add(_owner);
            }

            public void Execute(long owner, byte[] thumbnail)
            {
                _owner.Value = owner;
                _value.Value = thumbnail;
                _type.Value = (int)AttributeType.Image;
                _source.Value = (int)AttributeSource.Metadata;
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }
        
        private class LoadEntityCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteParameter _lastWriteTime = new SQLiteParameter(":lastWriteTime");
            private readonly SQLiteCommand _command;

            public LoadEntityCommand(SQLiteConnection connection)
            {
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

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        public void Insert(Attribute attribute, long file)
        {
            _insertAttribute.Execute(attribute, file);
        }

        public void SetThumbnail(byte[] thumbnail, long file)
        {
            _updateThumbnail.Execute(file, thumbnail);
        }

        public IEnumerable<Attribute> FindAttributes(string path, DateTime lastWriteTime)
        {
            using (var reader = _loadEntity.Execute(path, lastWriteTime))
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
                    yield return new Attribute(name, value, (AttributeSource) source);
                }
            }
        }

        public void Dispose()
        {
            _updateThumbnail?.Dispose();
            _insertAttribute?.Dispose();
            _connection?.Dispose();
        }
    }
}
