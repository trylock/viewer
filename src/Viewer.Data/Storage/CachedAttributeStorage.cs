using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data.Properties;

namespace Viewer.Data.Storage
{
    [Export(typeof(IAttributeStorage))]
    public class CachedAttributeStorage : IAttributeStorage, IDisposable
    {
        private enum StoreFlags
        {
            Touch,
            Thumbnail,
            Everything
        }

        private class StoreRequest
        {
            public IEntity Entity { get; }
            public StoreFlags Flags { get; }

            public StoreRequest(IEntity entity, StoreFlags flags)
            {
                Entity = entity ?? throw new ArgumentNullException(nameof(entity));
                Flags = flags;
            }
        }

        private readonly IStorageConfiguration _configuration;
        private readonly IAttributeStorage _persistentStorage;
        private readonly SqliteAttributeStorage _cacheStorage;
        private readonly AutoResetEvent _notifyWrite = new AutoResetEvent(false);
        private int _writeCount = 0;

        /// <summary>
        /// List of modified entities which will be written to the cache.
        /// </summary>
        private readonly Dictionary<string, StoreRequest> _modified = new Dictionary<string, StoreRequest>();

        /// <summary>
        /// Maximum time in milliseconds after which the write thread will write all changes to
        /// the cache storage.
        /// </summary>
        private const int WriteTimeout = 5000;

        /// <summary>
        /// Number of changes after which the write thread will write all changes to the cache stoage.
        /// </summary>
        private const int NotifyThreshold = 1000;

        [ImportingConstructor]
        public CachedAttributeStorage(
            [Import(typeof(FileSystemAttributeStorage))] IAttributeStorage persistentStorage, 
            SqliteAttributeStorage cacheStorage,
            IStorageConfiguration configuration)
        {
            _configuration = configuration;
            _persistentStorage = persistentStorage;
            _cacheStorage = cacheStorage;

            var cachingThread = new Thread(WriteThread)
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            cachingThread.Start();
        }

        public IEntity Load(string path)
        {
            // try to find the entity in the waiting list
            lock (_modified)
            {
                if (_modified.TryGetValue(path, out var req))
                {
                    return req.Entity;
                }
            }

            // try to load the entity from cache storage
            var storeFlags = StoreFlags.Touch;
            var entity = _cacheStorage.Load(path);
            if (entity == null)
            {
                // try to load the entity from the main storage
                storeFlags = StoreFlags.Everything;
                entity = _persistentStorage.Load(path);
            }

            // update access time/cache entry
            Cache(path, new StoreRequest(entity, storeFlags));

            return entity;
        }

        /// <inheritdoc />
        /// <summary>
        /// Store <paramref name="entity"/> to its file.
        /// The cache update is non-blocking and it is done asynchronously on a background thread.
        /// </summary>
        /// <param name="entity"></param>
        public void Store(IEntity entity)
        {
            _persistentStorage.Store(entity);
            Cache(entity.Path, new StoreRequest(entity, StoreFlags.Everything));
        }

        public void StoreThumbnail(IEntity entity)
        {
            Cache(entity.Path, new StoreRequest(entity, StoreFlags.Thumbnail));
        }

        public void Remove(IEntity entity)
        {
            _persistentStorage.Remove(entity);
            _cacheStorage.Remove(entity);
        }

        public void Move(IEntity entity, string newPath)
        {
            _persistentStorage.Move(entity, newPath);
            _cacheStorage.Move(entity, newPath);
        }

        private void Cache(string path, StoreRequest req)
        {
            lock (_modified)
            {
                _modified[path] = req;
            }

            var writeCount = Interlocked.Increment(ref _writeCount);
            if (writeCount % NotifyThreshold == 0)
            {
                _notifyWrite.Set();
            }
        }
        
        private void WriteThread()
        {
            for (;;)
            {
                _notifyWrite.WaitOne(WriteTimeout);

                KeyValuePair<string, StoreRequest>[] items;
                lock (_modified)
                {
                    items = _modified.ToArray();
                    _modified.Clear();
                }

                // update access times and attributes of cache entries
                using (var transaction = _cacheStorage.Connection.BeginTransaction())
                {
                    foreach (var item in items)
                    {
                        var req = item.Value;
                        switch (req.Flags)
                        {
                            case StoreFlags.Touch:
                                _cacheStorage.Touch(req.Entity);
                                break;
                            case StoreFlags.Thumbnail:
                                _cacheStorage.StoreThumbnail(req.Entity);
                                break;
                            case StoreFlags.Everything:
                                _cacheStorage.Store(req.Entity);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(req.Flags));
                        }
                    }

                    transaction.Commit();
                }

                // release least recently used photos from the cache
                _cacheStorage.Clean(_configuration.CacheLifespan, _configuration.CacheMaxFileCount);
            }
        }

        public void Dispose()
        {
            _notifyWrite?.Dispose();
        }
    }
}
