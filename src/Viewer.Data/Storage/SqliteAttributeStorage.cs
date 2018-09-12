using System;
using System.Collections;
using System.Collections.Concurrent;
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
using System.Threading;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using NLog;
using Viewer.Data.Formats;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Exif;
using Viewer.Data.SQLite;
using Viewer.IO;

namespace Viewer.Data.Storage
{
    [Export]
    public class SqliteAttributeStorage : IDeferredAttributeStorage
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private abstract class Request
        {
        }

        private sealed class StoreRequest : Request
        {
            public IEntity Entity { get; }

            public DateTime LastWriteTime { get; }

            public StoreRequest(IEntity entity, DateTime lastWriteTime)
            {
                Entity = entity;
                LastWriteTime = lastWriteTime;
            }
        }

        private sealed class StoreThumbnailRequest : Request
        {
            public byte[] Thumbnail { get; }

            public StoreThumbnailRequest(byte[] thumbnail)
            {
                Thumbnail = thumbnail;
            }
        }

        private sealed class TouchRequest : Request
        {
            public DateTime AccessTime { get; }

            public TouchRequest(DateTime accessTime)
            {
                AccessTime = accessTime;
            }
        }

        private sealed class DeleteRequest : Request
        {
        }

        private readonly Dictionary<string, Request> _requests;
        private readonly SQLiteConnectionFactory _connectionFactory;
        private readonly IStorageConfiguration _configuration;
        private readonly IAttributeReaderFactory _fileAttributeReaderFactory;

        private readonly object _readConnectionLock = new object();
        private readonly SQLiteConnection _readConnection;
        private readonly LoadEntityCommand _loadCommand;

        [ImportingConstructor]
        public SqliteAttributeStorage(
            SQLiteConnectionFactory connectionFactory, 
            IStorageConfiguration configuration,
            [Import(typeof(FileAttributeReaderFactory))] IAttributeReaderFactory fileAttributesReaderFactory)
        {
            _fileAttributeReaderFactory = fileAttributesReaderFactory;
            _connectionFactory = connectionFactory;
            _requests = new Dictionary<string, Request>(StringComparer.CurrentCultureIgnoreCase);
            _configuration = configuration;
            _readConnection = _connectionFactory.Create();
            _loadCommand = new LoadEntityCommand(_readConnection);
        }
        
        public IEntity Load(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            path = PathUtils.NormalizePath(path);

            // check if there is a pending change in main memory
            lock (_requests)
            {
                if (_requests.TryGetValue(path, out var req))
                {
                    if (req is StoreRequest store)
                    {
                        return store.Entity;
                    }

                    if (req is DeleteRequest)
                    {
                        return null;
                    }

                    if (req is TouchRequest)
                    {
                        _requests[path] = new TouchRequest(DateTime.Now);
                    }
                }
                else
                {
                    _requests[path] = new TouchRequest(DateTime.Now);
                }
            }

            var fi = new FileInfo(path);
            var lastWriteTime = fi.LastWriteTime;
            IEntity entity = new FileEntity(path);
            
            // Get file metadata attributes. These are the only attributes which can change
            // even if the LastWriteTime has not changed.
            var fileAttributes = new List<Attribute>();
            try
            {
                var attributes = _fileAttributeReaderFactory.CreateFromSegments(fi, Enumerable.Empty<JpegSegment>());
                for (;;)
                {
                    var attr = attributes.Read();
                    if (attr == null)
                    {
                        break;
                    }
                    fileAttributes.Add(attr);
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            // otherwise, load the entity from the database
            // make sure this is the only thread which uses _readConnection
            lock (_readConnectionLock)
            {
                // load valid attributes
                using (var reader = _loadCommand.Execute(path, lastWriteTime))
                { 
                    // add attributes to the collection
                    int attributeCount = 0;
                    while (reader.Read())
                    {
                        ++attributeCount;
                        var name = reader.GetString(0);
                        var source = reader.GetInt32(1);
                        var type = reader.GetInt32(2);
                        var valueSize = reader.GetInt64(4);

                        switch ((AttributeType) type)
                        {
                            case AttributeType.Int:
                                entity = entity.SetAttribute(new Attribute(name,
                                    new IntValue(reader.GetInt32(3)), (AttributeSource) source));
                                break;
                            case AttributeType.Double:
                                entity = entity.SetAttribute(new Attribute(name,
                                    new RealValue(reader.GetDouble(3)), (AttributeSource) source));
                                break;
                            case AttributeType.String:
                                entity = entity.SetAttribute(new Attribute(name,
                                    new StringValue(reader.GetString(3)), (AttributeSource) source));
                                break;
                            case AttributeType.DateTime:
                                entity = entity.SetAttribute(new Attribute(name,
                                    new DateTimeValue(reader.GetDateTime(3)), (AttributeSource) source));
                                break;
                            case AttributeType.Image:
                                var buffer = new byte[valueSize];
                                var length = reader.GetBytes(3, 0, buffer, 0, buffer.Length);
                                Trace.Assert(buffer.Length == length);
                                entity = entity.SetAttribute(new Attribute(name,
                                    new ImageValue(buffer), (AttributeSource) source));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    var result = attributeCount > 0 ? entity : null;
                    if (result != null)
                    {
                        foreach (var attr in fileAttributes)
                        {
                            result.SetAttribute(attr);
                        }
                    }

                    return result;
                }
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

        public void Store(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var request = new StoreRequest(entity.Clone(), GetLastWriteTime(entity.Path));
            lock (_requests)
            {
                _requests[entity.Path] = request;
            }
        }

        public void StoreThumbnail(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var thumbnail = entity.GetAttribute(ExifAttributeReaderFactory.ThumbnailAttrName);
            var thumbnailValue = thumbnail?.Value as ImageValue;
            if (thumbnailValue == null || thumbnailValue.IsNull)
            {
                return;
            }

            lock (_requests)
            {
                if (_requests.TryGetValue(entity.Path, out var req))
                {
                    if (req is DeleteRequest)
                    {
                        return; // storing thumbnail of a deleted file, this is no-op
                    }

                    if (req is StoreRequest store)
                    {
                        // update pending entity's thumbnail
                        store.Entity.SetAttribute(thumbnail);
                        return;
                    }
                }
                _requests[entity.Path] = new StoreThumbnailRequest(thumbnailValue.Value);
            }
        }

        public void Delete(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            lock (_requests)
            {
                _requests[entity.Path] = new DeleteRequest();
            }
        }

        public void Move(IEntity entity, string newPath)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (newPath == null)
                throw new ArgumentNullException(nameof(newPath));

            newPath = PathUtils.NormalizePath(newPath);

            lock (_requests)
            {
                if (_requests.TryGetValue(entity.Path, out var req))
                {
                    if (req is DeleteRequest)
                    {
                        // the entity has been deleted, this is no-op
                        return;
                    }

                    // apply the request to the entity at the new location
                    _requests.Remove(entity.Path);
                    _requests[newPath] = req;
                }
            }

            using (var connection = _connectionFactory.Create())
            using (var transaction = connection.BeginTransaction())
            using (var deleteFileCommand = new DeleteFileCommand(connection))
            {
                // delete file at the newPath if there is any
                deleteFileCommand.Execute(newPath);

                // move file to newPath
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE files SET path = :newPath WHERE path = :oldPath";
                    command.Parameters.Add(new SQLiteParameter(":oldPath", entity.Path));
                    command.Parameters.Add(new SQLiteParameter(":newPath", newPath));
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        #region SQL commands

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
                            ON (f.id = a.owner)
                    WHERE 
                        f.path = :path AND
                        f.lastWriteTime >= :lastWriteTime";
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
                _command.CommandText = "INSERT INTO attributes (name, source, type, value, owner)" +
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
                _type.Value = (int) attr.Value.Type;
                _source.Value = (int) attr.Source;
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

        private class InsertFileCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteParameter _lastWriteTime = new SQLiteParameter(":lastWriteTime");
            private readonly SQLiteCommand _command;

            public InsertFileCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "INSERT INTO files (path, lastAccessTime, lastWriteTime)" +
                                       "VALUES (:path, datetime('now'), :lastWriteTime)";
                _command.Parameters.Add(_path);
                _command.Parameters.Add(_lastWriteTime);
            }

            public void Execute(string path, DateTime lastWriteTime)
            {
                _path.Value = path;
                _lastWriteTime.Value = lastWriteTime.ToUniversalTime();
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        private class DeleteFileCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteCommand _command;

            public DeleteFileCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "DELETE FROM files WHERE path = :path";
                _command.Parameters.Add(_path);
            }

            public void Execute(string path)
            {
                _path.Value = path;
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }
        
        private class TouchFileCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteParameter _accessTime = new SQLiteParameter(":accessTime");
            private readonly SQLiteCommand _command;

            public TouchFileCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "UPDATE files SET lastAccessTime = :accessTime WHERE path = :path";
                _command.Parameters.Add(_path);
                _command.Parameters.Add(_accessTime);
            }

            public void Execute(string path, DateTime accessTime)
            {
                _path.Value = path;
                _accessTime.Value = accessTime.ToUniversalTime();
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        private class SelectFileCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteCommand _command;

            public SelectFileCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "SELECT id FROM files WHERE path = :path";
                _command.Parameters.Add(_path);
            }

            public long? Execute(string path)
            {
                _path.Value = path;
                return _command.ExecuteScalar() as long?;
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
                INSERT OR REPLACE INTO attributes (name, source, type, value, owner)
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
                _type.Value = (int) AttributeType.Image;
                _source.Value = (int) AttributeSource.Metadata;
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        private class CleanCommand : IDisposable
        {
            private readonly SQLiteParameter _lastAccessTimeThreshold = 
                new SQLiteParameter(":lastAccessTimeThreshold");

            private readonly SQLiteCommand _command;

            public CleanCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "DELETE FROM files WHERE lastAccessTime <= :lastAccessTimeThreshold";
                _command.Parameters.Add(_lastAccessTimeThreshold);
            }

            /// <summary>
            /// Remove all files (and their attributes) from the database whose last access time
            /// is less than <paramref name="lastAccessTimeThreshold"/>.
            /// </summary>
            /// <param name="lastAccessTimeThreshold"></param>
            public void Execute(DateTime lastAccessTimeThreshold)
            {
                _lastAccessTimeThreshold.Value = lastAccessTimeThreshold.ToUniversalTime();
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        #endregion
        
        private class SavepointCommand : IDisposable
        {
            private readonly SQLiteConnection _connection;
            private readonly string _name;
            private bool _isResolved = false;

            public SavepointCommand(SQLiteConnection connection, string name)
            {
                _connection = connection;
                _name = name;

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SAVEPOINT " + _name;
                    command.ExecuteNonQuery();
                }
            }
            
            public void Release()
            {
                if (_isResolved)
                    throw new InvalidOperationException("This savepoint has already been resolved.");

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "RELEASE SAVEPOINT " + _name;
                    command.ExecuteNonQuery();
                }

                _isResolved = true;
            }

            public void Rollback()
            {
                if (_isResolved)
                    throw new InvalidOperationException("This savepoint has already been resolved.");

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "RELEASE SAVEPOINT " + _name;
                    command.ExecuteNonQuery();
                }

                _isResolved = true;
            }

            public void Dispose()
            {
                if (!_isResolved)
                {
                    Rollback();
                }
            }
        }

        public void ApplyChanges()
        {
            // consume all requests added so far
            KeyValuePair<string, Request>[] requests;
            lock (_requests)
            {
                requests = _requests.ToArray();
                _requests.Clear();
            }

            // don't do anything if there haven't been any changes
            if (requests.Length <= 0)
            {
                return;
            }

            var exceptions = new List<Exception>();
            
            // apply all changes
            using (var connection = _connectionFactory.Create())
            using (var transaction = connection.BeginTransaction())
            using (var selectFileCommand = new SelectFileCommand(connection))
            using (var touchFileCommand = new TouchFileCommand(connection))
            using (var removeFileCommand = new DeleteFileCommand(connection))
            using (var insertFileCommand = new InsertFileCommand(connection))
            using (var updateThumbnailCommand = new UpdateThumbnailCommand(connection))
            using (var insertAttributeCommand = new InsertAttributeCommand(connection))
            {
                var requestCount = 0;
                foreach (var req in requests)
                {
                    var name = "sp" + requestCount++;
                    var savepoint = new SavepointCommand(connection, name);
                    var failed = false;
                    try
                    {
                        if (req.Value is StoreRequest storeReq)
                        {
                            // re-insert the file (this also deletes all attributes)
                            removeFileCommand.Execute(req.Key);
                            insertFileCommand.Execute(req.Key, storeReq.LastWriteTime);

                            // add all attributes
                            var fileId = connection.LastInsertRowId;
                            foreach (var attr in storeReq.Entity)
                            {
                                insertAttributeCommand.Execute(attr, fileId);
                            }
                        }
                        else if (req.Value is StoreThumbnailRequest thumbnailReq)
                        {
                            var id = selectFileCommand.Execute(req.Key);
                            if (id == null)
                                continue;
                            updateThumbnailCommand.Execute((long)id, thumbnailReq.Thumbnail);
                        }
                        else if (req.Value is TouchRequest touchReq)
                        {
                            touchFileCommand.Execute(req.Key, touchReq.AccessTime);
                        }
                        else if (req.Value is DeleteRequest)
                        {
                            removeFileCommand.Execute(req.Key);
                        }
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        savepoint.Rollback();
                        failed = true;
                    }

                    if (!failed)
                    {
                        savepoint.Release();
                    }
                }

                transaction.Commit();

                // clean outdated files
                try
                {
                    using (var cleanCommand = new CleanCommand(connection))
                    {
                        var threshold = DateTime.Now - _configuration.CacheLifespan;
                        cleanCommand.Execute(threshold);
                    }
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            // if there were errors, throw an exception
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        public void Dispose()
        {
            _loadCommand?.Dispose();
            _readConnection?.Dispose();
        }
    }
}
