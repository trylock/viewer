using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Viewer.Data.Formats.Attributes;
using Viewer.IO;

namespace Viewer.Data.Storage
{
    [Export(typeof(ICacheAttributeStorage))]
    [Export(typeof(SqliteAttributeStorage))]
    public class SqliteAttributeStorage : ICacheAttributeStorage
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public SQLiteConnection Connection { get; }
        
        [ImportingConstructor]
        public SqliteAttributeStorage(SQLiteConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <inheritdoc />
        /// <summary>
        /// Load attributes from database.
        /// </summary>
        /// <param name="inputPath"></param>
        /// <returns>
        ///     Valid attributes of the file or null if the attributes in cache are not valid.
        /// </returns>
        public IEntity Load(string inputPath)
        {
            var lastWriteTime = DateTime.MinValue;
            try
            {
                var fi = new FileInfo(inputPath);
                lastWriteTime = fi.LastWriteTime;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            IEntity entity = new FileEntity(inputPath);

            // load valid attributes
            SQLiteDataReader reader;
            using (var query = new SQLiteCommand(Connection))
            {
                query.CommandText = @"
                SELECT a.name, a.source, a.type, a.value, length(a.value) as size
                FROM files AS f
                    INNER JOIN attributes AS a 
                        ON (f.id = a.owner)
                WHERE 
                    f.path = :path AND
                    f.lastWriteTime >= :lastWriteTime";

                query.Parameters.Add(new SQLiteParameter(":path", entity.Path));
                query.Parameters.Add(new SQLiteParameter(":lastWriteTime", lastWriteTime));
                reader = query.ExecuteReader();
            }
            
            // add attributes to the collection
            int attributeCount = 0;
            while (reader.Read())
            {
                ++attributeCount;
                var name = reader.GetString(0);
                var source = reader.GetInt32(1);
                var type = reader.GetInt32(2);
                var valueSize = reader.GetInt64(4);

                switch ((AttributeType)type)
                {
                    case AttributeType.Int:
                        entity = entity.SetAttribute(new Attribute(name, new IntValue(reader.GetInt32(3)), (AttributeSource)source));
                        break;
                    case AttributeType.Double:
                        entity = entity.SetAttribute(new Attribute(name, new RealValue(reader.GetDouble(3)), (AttributeSource)source));
                        break;
                    case AttributeType.String:
                        entity = entity.SetAttribute(new Attribute(name, new StringValue(reader.GetString(3)), (AttributeSource)source));
                        break;
                    case AttributeType.DateTime:
                        entity = entity.SetAttribute(new Attribute(name, new DateTimeValue(reader.GetDateTime(3)), (AttributeSource)source));
                        break;
                    case AttributeType.Image:
                        var buffer = new byte[valueSize];
                        var length = reader.GetBytes(3, 0, buffer, 0, buffer.Length);
                        Debug.Assert(buffer.Length == length);
                        entity = entity.SetAttribute(new Attribute(name, new ImageValue(buffer), (AttributeSource)source));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return attributeCount > 0 ? entity : null;
        }

        public void Store(IEntity entity)
        {
            // otherwise, we have to update attributes
            using (var transaction = Connection.BeginTransaction())
            {
                RemoveFile(entity.Path);
                var id = InsertFile(entity.Path);
                
                foreach (var attr in entity)
                {
                    StoreAttribute(id, attr);
                }

                transaction.Commit();
            }
        }

        public void StoreThumbnail(IEntity entity)
        {
            var thumbnail = entity.GetValue<ImageValue>("thumbnail");
            if (thumbnail == null)
            {
                return;
            }

            // finding file ID and updating it 
            using (var transaction = Connection.BeginTransaction(IsolationLevel.Serializable))
            {
                long? id;
                using (var query = new SQLiteCommand(Connection))
                {
                    query.CommandText = "SELECT id FROM files WHERE path = :path";
                    query.Parameters.Add(new SQLiteParameter("path", entity.Path));
                    id = query.ExecuteScalar() as long?;
                    if (id == null)
                    {
                        return;
                    }
                }

                using (var command = new SQLiteCommand(Connection))
                {
                    command.CommandText = @"
                    INSERT OR REPLACE INTO attributes (name, source, type, value, owner)
                    VALUES ('thumbnail', :source, :type, :value, :owner)";
                    command.Parameters.Add(new SQLiteParameter("source", AttributeSource.Metadata));
                    command.Parameters.Add(new SQLiteParameter("type", AttributeType.Image));
                    command.Parameters.Add(new SQLiteParameter("value", thumbnail.Value));
                    command.Parameters.Add(new SQLiteParameter("owner", (long) id));
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        public void Move(IEntity entity, string newPath)
        {
            MoveFile(PathUtils.NormalizePath(entity.Path), PathUtils.NormalizePath(newPath));
        }

        public void Remove(IEntity entity)
        {
            RemoveFile(PathUtils.NormalizePath(entity.Path));
        }
        
        public void Touch(IEntity entity)
        {
            using (var query = new SQLiteCommand(Connection))
            {
                query.CommandText = @"
                    UPDATE files 
                    SET lastAccessTime = datetime('now') 
                    WHERE path = :path";
                query.Parameters.Add(new SQLiteParameter(":path", entity.Path));
                query.ExecuteNonQuery();
            }
        }

        public void Clean(TimeSpan lastAccessTimeThreshold, int fileCountThreashold)
        {
            using (var transaction = Connection.BeginTransaction(IsolationLevel.Serializable))
            {
                var threshold = DateTime.Now - lastAccessTimeThreshold;
                using (var query = new SQLiteCommand(Connection))
                {
                    query.CommandText = @"
                    SELECT lastAccessTime 
                    FROM files 
                    ORDER BY lastAccessTime DESC
                    LIMIT 1
                    OFFSET :offset";
                    query.Parameters.Add(new SQLiteParameter(":offset", fileCountThreashold));
                    if (query.ExecuteScalar() is DateTime result && 
                        threshold.CompareTo(result) > 0)
                    {
                        threshold = result;
                    }
                }

                using (var query = new SQLiteCommand(Connection))
                {
                    query.CommandText = @"DELETE FROM files WHERE lastAccessTime < :threshold";
                    query.Parameters.Add(new SQLiteParameter(":threshold", threshold));
                    query.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        private DateTime GetLastWriteTime(string path)
        {
            var lastWriteTime = DateTime.MinValue;
            try
            {
                var fi = new FileInfo(path);
                lastWriteTime = fi.LastWriteTime;
            }
            catch (IOException e)
            {
                Logger.Warn(e);
            }
            return lastWriteTime;
        }
        
        /// <summary>
        /// Make sure there is a record for file <paramref name="path"/> and return its id.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private long InsertFile(string path)
        {
            var lastWriteTime = GetLastWriteTime(path);
            using (var command = new SQLiteCommand(Connection))
            {
                command.CommandText = @"
                INSERT INTO files (path, lastWriteTime, lastAccessTime) 
                VALUES (:path, :lastWriteTime, datetime('now'))";
                command.Parameters.Add(new SQLiteParameter(":path", path));
                command.Parameters.Add(new SQLiteParameter(":lastWriteTime", lastWriteTime));
                command.ExecuteNonQuery();
            }

            return Connection.LastInsertRowId;
        }
        
        private void RemoveFile(string path)
        {
            using (var query = new SQLiteCommand(Connection))
            {
                query.CommandText = @"DELETE FROM files WHERE path = :path";
                query.Parameters.Add(new SQLiteParameter(":path", path));
                query.ExecuteNonQuery();
            }
        }

        private void MoveFile(string oldPath, string newPath)
        {
            using (var transation = Connection.BeginTransaction())
            {
                RemoveFile(newPath);

                using (var query = new SQLiteCommand(Connection))
                {
                    query.CommandText = @"UPDATE files SET path = :newPath WHERE path = :oldPath";
                    query.Parameters.Add(new SQLiteParameter(":oldPath", oldPath));
                    query.Parameters.Add(new SQLiteParameter(":newPath", newPath));
                    query.ExecuteNonQuery();
                }

                transation.Commit();
            }
        }

        /// <summary>
        /// This visitor prepares given sql command for inserting a new attribute.
        /// It adds the attribute name and value parameters for each possible value type.
        /// </summary>
        private class InsertVisitor : IValueVisitor
        {
            private readonly SQLiteCommand _command;

            public const string TypeName = ":type";
            public const string ValueName = ":value";

            public InsertVisitor(SQLiteCommand cmd)
            {
                _command = cmd;
            }

            public void Prepare(Attribute attr)
            {
                _command.Parameters.Add(new SQLiteParameter(TypeName, (int)attr.Value.Type));
                attr.Value.Accept(this);
            }

            void IValueVisitor.Visit(IntValue attr)
            {
                _command.Parameters.Add(new SQLiteParameter(ValueName, attr.Value));
            }

            void IValueVisitor.Visit(RealValue attr)
            {
                _command.Parameters.Add(new SQLiteParameter(ValueName, attr.Value));
            }

            void IValueVisitor.Visit(StringValue attr)
            {
                _command.Parameters.Add(new SQLiteParameter(ValueName, attr.Value));
            }

            void IValueVisitor.Visit(DateTimeValue attr)
            {
                _command.Parameters.Add(new SQLiteParameter(ValueName, attr.Value?.ToString(DateTimeValue.Format)));
            }

            void IValueVisitor.Visit(ImageValue attr)
            {
                _command.Parameters.Add(new SQLiteParameter(ValueName, attr.Value));
            }
        }

        private void StoreAttribute(long fileId, Attribute attr)
        {
            using (var command = new SQLiteCommand(Connection))
            {
                command.CommandText = "INSERT INTO attributes (name, source, type, value, owner) VALUES (:name, :source, :type, :value, :owner)";
                command.Parameters.Add(new SQLiteParameter(":name", attr.Name));
                command.Parameters.Add(new SQLiteParameter(":source", (int)attr.Source));
                command.Parameters.Add(new SQLiteParameter(":owner", fileId));
                var visitor = new InsertVisitor(command);
                visitor.Prepare(attr);
                command.ExecuteNonQuery();
            }
        }
    }
}
