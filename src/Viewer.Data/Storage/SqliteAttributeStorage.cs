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
        private readonly IAttributeSerializer _fileMetadataSerializer;
        
        [ImportingConstructor]
        public SqliteAttributeStorage(
            SQLiteConnectionFactory connectionFactory, 
            IStorageConfiguration configuration,
            [Import(typeof(FileMetadataAttributeSerializer))] IAttributeSerializer fileMetadataSerializer)
        {
            _fileMetadataSerializer = fileMetadataSerializer;
            _connectionFactory = connectionFactory;
            _requests = new Dictionary<string, Request>(StringComparer.CurrentCultureIgnoreCase);
            _configuration = configuration;
        }

        /// <summary>
        /// Every <see cref="Reader"/> acquires this lock as a reader whenever it is created. The
        /// <see cref="ApplyChanges"/> method tries to acquire it as a writer whenever it tries
        /// to delete outdated files. The purpose of it is to prevent the <see cref="ApplyChanges"/>
        /// method from evicting files which will be read right after that.
        /// </summary>
        private readonly ReaderWriterLockSlim _readerLock = new ReaderWriterLockSlim();

        private void OnReaderCreated()
        {
            _readerLock.EnterReadLock();
        }

        private void OnReaderDisposed()
        {
            _readerLock.ExitReadLock();
        }

        /// <summary>
        /// SQLite connection and commands are kept in a thread-local variable. This way, it is
        /// safe to call the Load method from multiple threads.
        /// </summary>
        private class Reader : IReadableAttributeStorage
        {
            private readonly SqliteAttributeStorage _storage;
            private readonly ThreadLocal<LoadEntityQuery> _query;

            public Reader(SqliteAttributeStorage storage)
            {
                _storage = storage;
                _storage.OnReaderCreated();
                _query = new ThreadLocal<LoadEntityQuery>(CreateQuery, true);
            }

            private LoadEntityQuery CreateQuery()
            {
                return new LoadEntityQuery(_storage._connectionFactory.Create());
            }

            public IEntity Load(string path)
            {
                return _storage.LoadImpl(path, 
                    () => _query.Value, dispose: false);
            }

            public void Dispose()
            {
                _storage.OnReaderDisposed();
                foreach (var value in _query.Values)
                {
                    value.Dispose();
                }
                _query.Dispose();
            }
        }

        public IReadableAttributeStorage CreateReader()
        {
            return new Reader(this);
        }

        /// <summary>
        /// Load an entity at <paramref name="path"/> using query returned from
        /// <paramref name="queryGetter"/>. The query is disposed iff <paramref name="dispose"/>
        /// is true.
        /// </summary>
        /// <param name="path">Path to load</param>
        /// <param name="queryGetter">Function which returns load query</param>
        /// <param name="dispose">
        /// ture iff we should dispose the query returned from <paramref name="queryGetter"/>
        /// </param>
        /// <returns>Loaded entity of null</returns>
        private IEntity LoadImpl(string path, Func<LoadEntityQuery> queryGetter, bool dispose)
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
                fileAttributes.AddRange(_fileMetadataSerializer.Deserialize(fi, null));
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            // otherwise, load the entity from the database
            var query = queryGetter();
            try
            {
                // load valid attributes
                int count = 0;
                foreach (var attr in query.Fetch(entity.Path, lastWriteTime))
                {
                    entity.SetAttribute(attr);
                    ++count;
                }

                // update file attributes
                var result = count > 0 ? entity : null;
                if (result != null)
                {
                    foreach (var attr in fileAttributes)
                    {
                        result.SetAttribute(attr);
                    }
                }

                return result;
            }
            finally
            {
                if (dispose)
                {
                    query.Dispose();
                }
            }
        }

        public IEntity Load(string path)
        {
            return LoadImpl(path, 
                () => new LoadEntityQuery(_connectionFactory.Create()), 
                dispose: true);
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

            var thumbnail = entity.GetAttribute(ExifJpegSerializer.Thumbnail);
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
            using (var files = new Files(connection))
            {
                files.Move(entity.Path, newPath);
                transaction.Commit();
            }
        }
        
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
            using (var files = new Files(connection))
            using (var attributes = new Attributes(connection))
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
                            files.Delete(req.Key);
                            files.Insert(req.Key, storeReq.LastWriteTime);

                            // add all attributes
                            var fileId = connection.LastInsertRowId;
                            foreach (var attr in storeReq.Entity)
                            {
                                attributes.Insert(attr, fileId);
                            }
                        }
                        else if (req.Value is StoreThumbnailRequest thumbnailReq)
                        {
                            var id = files.FindId(req.Key);
                            if (id < 0)
                                continue;
                            attributes.SetThumbnail(thumbnailReq.Thumbnail, id);
                        }
                        else if (req.Value is TouchRequest touchReq)
                        {
                            files.Touch(req.Key, touchReq.AccessTime);
                        }
                        else if (req.Value is DeleteRequest)
                        {
                            files.Delete(req.Key);
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
                if (_readerLock.TryEnterWriteLock(100))
                {
                    try
                    {
                        files.DeleteOutdated(DateTime.Now - _configuration.CacheLifespan);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                    finally
                    {
                        _readerLock.ExitWriteLock();
                    }
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
        }
    }
}
