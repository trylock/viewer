using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.Data.Storage
{
    [Export(typeof(IAttributeStorage))]
    public class CachedAttributeStorage : IAttributeStorage, IDisposable
    {
        private readonly IAttributeStorage _persistentStorage;
        private readonly SqliteAttributeStorage _cacheStorage;
        private readonly AutoResetEvent _notifyWrite = new AutoResetEvent(false);
        private readonly TimeSpan _cacheLifespan = new TimeSpan(1, 0, 0, 0);
        private int _writeCount = 0;

        /// <summary>
        /// List of modified entities which will be written to the cache.
        /// If the value is null, only access time is modified.
        /// If the value is not null, the cache entry is replaced and its access time is modified.
        /// </summary>
        private readonly Dictionary<string, IEntity> _modified = new Dictionary<string, IEntity>();

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
            SqliteAttributeStorage cacheStorage)
        {
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
                if (_modified.TryGetValue(path, out var value))
                {
                    return value;
                }
            }

            // try to load the entity from cache storage
            var isCached = true;
            var entity = _cacheStorage.Load(path);
            if (entity == null)
            {
                // try to load the entity from the main storage
                isCached = false;
                entity = _persistentStorage.Load(path);
            }

            // update access time/cache entry
            Cache(path, isCached ? null : entity);

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
            Cache(entity.Path, entity);
        }

        public void Remove(string path)
        {
            _persistentStorage.Remove(path);
            _cacheStorage.Remove(path);
        }

        public void Move(string oldPath, string newPath)
        {
            _persistentStorage.Move(oldPath, newPath);
            _cacheStorage.Move(oldPath, newPath);
        }

        private void Cache(string path, IEntity value)
        {
            lock (_modified)
            {
                _modified[path] = value;
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

                KeyValuePair<string, IEntity>[] items;
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
                        if (item.Value == null)
                        {
                            _cacheStorage.Touch(item.Key);
                        }
                        else
                        {
                            _cacheStorage.Store(item.Value);
                        }
                    }
                    transaction.Commit();
                }

                // release least recently used photos from the cache
                _cacheStorage.Clean(_cacheLifespan);
            }
        }

        public void Dispose()
        {
            _notifyWrite?.Dispose();
        }
    }
}
