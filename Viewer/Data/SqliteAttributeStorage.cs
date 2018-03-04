using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats.Attributes;

namespace Viewer.Data
{
    public class SqliteAttributeStorage : IAttributeStorage
    {
        private SQLiteConnection _connection;
        
        public SqliteAttributeStorage(SQLiteConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Load attributes from database.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>
        ///     Valid attributes of the file or null if the attributes in cache are not valid.
        /// </returns>
        public AttributeCollection Load(string path)
        {
            var attrs = new AttributeCollection(path);
            
            // load valid attributes
            SQLiteDataReader reader;
            using (var query = new SQLiteCommand(_connection))
            {
                query.CommandText = @"
                SELECT a.name, a.source, a.type, a.value, length(a.value) as size
                FROM files AS f
                    INNER JOIN attributes AS a 
                        ON (f.id = a.owner)
                WHERE 
                    f.path = :path AND
                    f.lastWriteTime >= :lastWriteTime";

                query.Parameters.Add(new SQLiteParameter(":path", path));
                query.Parameters.Add(new SQLiteParameter(":lastWriteTime", attrs.LastWriteTime));
                reader = query.ExecuteReader();
            }
            
            // add attributes to the collection
            while (reader.Read())
            {
                var name = reader.GetString(0);
                var source = reader.GetInt32(1);
                var type = reader.GetInt32(2);
                var valueSize = reader.GetInt64(4);

                switch ((AttributeType)type)
                {
                    case AttributeType.Int:
                        attrs.SetAttribute(new IntAttribute(name, (AttributeSource)source, reader.GetInt32(3)));
                        break;
                    case AttributeType.Double:
                        attrs.SetAttribute(new DoubleAttribute(name, (AttributeSource)source, reader.GetDouble(3)));
                        break;
                    case AttributeType.String:
                        attrs.SetAttribute(new StringAttribute(name, (AttributeSource)source, reader.GetString(3)));
                        break;
                    case AttributeType.DateTime:
                        attrs.SetAttribute(new DateTimeAttribute(name, (AttributeSource)source, reader.GetDateTime(3)));
                        break;
                    case AttributeType.Image:
                        var buffer = new byte[valueSize];
                        var length = reader.GetBytes(3, 0, buffer, 0, buffer.Length);
                        Debug.Assert(buffer.Length == length);
                        var image = Image.FromStream(new MemoryStream(buffer));
                        attrs.SetAttribute(new ImageAttribute(name, (AttributeSource)source, image));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (attrs.Count <= 0)
            {
                return null;
            }

            return attrs;
        }

        public void Store(string path, AttributeCollection attrs)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                // remove file (and transitively all attributes)
                RemoveFile(path);
                
                long id = StoreFile(path);

                // add new attributes
                foreach (var attr in attrs)
                {
                    StoreAttribute(id, attr);
                }

                transaction.Commit();
            }
        }

        public void Flush()
        {
        }

        private void RemoveFile(string path)
        {
            using (var query = new SQLiteCommand(_connection))
            {
                query.CommandText = @"DELETE FROM files WHERE path = :path";
                query.Parameters.Add(new SQLiteParameter(":path", path));
            }
        }

        private long StoreFile(string path)
        {
            var fi = new FileInfo(path);
            using (var command = new SQLiteCommand(_connection))
            {
                command.CommandText = "INSERT INTO files (path, lastWriteTime, lastAccessTime) VALUES (:path, :lastWriteTime, CURRENT_TIMESTAMP)";
                command.Parameters.Add(new SQLiteParameter(":path", path));
                command.Parameters.Add(new SQLiteParameter(":lastWriteTime", fi.LastWriteTime));
                command.ExecuteNonQuery();
                return _connection.LastInsertRowId;
            }
        }

        /// <summary>
        /// This visitor prepares given sql command for inserting a new attribute.
        /// It adds the attribute name and value parameters for each possible value type.
        /// </summary>
        private class InsertVisitor : IAttributeVisitor
        {
            private SQLiteCommand _command;

            public const string TypeName = ":type";
            public const string ValueName = ":value";

            public InsertVisitor(SQLiteCommand cmd)
            {
                _command = cmd;
            }

            public void Visit(IntAttribute attr)
            {
                _command.Parameters.Add(new SQLiteParameter(TypeName, (int)AttributeType.Int));
                _command.Parameters.Add(new SQLiteParameter(ValueName, attr.Value));
            }

            public void Visit(DoubleAttribute attr)
            {
                _command.Parameters.Add(new SQLiteParameter(TypeName, (int)AttributeType.Double));
                _command.Parameters.Add(new SQLiteParameter(ValueName, attr.Value));
            }

            public void Visit(StringAttribute attr)
            {
                _command.Parameters.Add(new SQLiteParameter(TypeName, (int)AttributeType.String));
                _command.Parameters.Add(new SQLiteParameter(ValueName, attr.Value));
            }

            public void Visit(DateTimeAttribute attr)
            {
                _command.Parameters.Add(new SQLiteParameter(TypeName, (int)AttributeType.DateTime));
                _command.Parameters.Add(new SQLiteParameter(ValueName, attr.Value.ToString(DateTimeAttribute.Format)));
            }

            public void Visit(ImageAttribute attr)
            {
                _command.Parameters.Add(new SQLiteParameter(TypeName, (int)AttributeType.Image));
                _command.Parameters.Add(new SQLiteParameter(ValueName, ImageToByteArray(attr.Value)));
            }

            private byte[] ImageToByteArray(Image img)
            {
                using (var imageDataStream = new MemoryStream())
                {
                    img.Save(imageDataStream, ImageFormat.Jpeg);
                    return imageDataStream.ToArray();
                }
            }
        }

        private void StoreAttribute(long fileId, Attribute attr)
        {
            using (var command = new SQLiteCommand(_connection))
            {
                command.CommandText = "INSERT INTO attributes (name, source, type, value, owner) VALUES (:name, :source, :type, :value, :owner)";
                command.Parameters.Add(new SQLiteParameter(":name", attr.Name));
                command.Parameters.Add(new SQLiteParameter(":source", (int)attr.Source));
                command.Parameters.Add(new SQLiteParameter(":owner", fileId));
                var visitor = new InsertVisitor(command);
                attr.Accept(visitor);
                command.ExecuteNonQuery();
            }
        }
    }
}
