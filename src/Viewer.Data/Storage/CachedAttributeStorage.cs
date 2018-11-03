using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data.Properties;

namespace Viewer.Data.Storage
{
    [Export]
    [Export(typeof(IAttributeStorage))]
    public class CachedAttributeStorage : IAttributeStorage
    {
        private readonly IAttributeStorage _persistentStorage;
        private readonly IDeferredAttributeStorage _cacheStorage;
        private readonly AutoResetEvent _notifyWrite = new AutoResetEvent(false);
        private readonly Thread _writeThread;
        private int _writeCount = 0;
        
        /// <summary>
        /// Maximum time in milliseconds after which the write thread will write all changes to
        /// the cache storage.
        /// </summary>
        private const int WriteTimeout = 4000;

        /// <summary>
        /// Number of changes after which the write thread will write all changes to the cache stoage.
        /// </summary>
        private const int NotifyThreshold = 4096;
        
        [ImportingConstructor]
        public CachedAttributeStorage(
            [Import(typeof(FileSystemAttributeStorage))] IAttributeStorage persistentStorage,
            [Import(typeof(SqliteAttributeStorage))] IDeferredAttributeStorage cacheStorage)
        {
            _persistentStorage = persistentStorage;
            _cacheStorage = cacheStorage;
            
            _writeThread = new Thread(WriteThread)
            {
                Priority = ThreadPriority.Lowest
            };
            _writeThread.Start();
        }

        public IEntity Load(string path)
        {
            // try to load the entity from cache storage
            var entity = _cacheStorage.Load(path);
            if (entity == null)
            {
                // try to load the entity from the main storage
                entity = _persistentStorage.Load(path);
                if (entity != null)
                {
                    _cacheStorage.Store(entity);
                }
            }

            Notify();
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
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _persistentStorage.Store(entity);
            _cacheStorage.Store(entity);
            Notify();
        }

        public void StoreThumbnail(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _cacheStorage.StoreThumbnail(entity);
            Notify();
        }

        public void Delete(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _persistentStorage.Delete(entity);
            _cacheStorage.Delete(entity);
            Notify();
        }

        public void Move(IEntity entity, string newPath)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (newPath == null)
                throw new ArgumentNullException(nameof(newPath));
            
            _persistentStorage.Move(entity, newPath);
            _cacheStorage.Move(entity, newPath);
            Notify();
        }
        
        private void Notify()
        {
            var writeCount = Interlocked.Increment(ref _writeCount);
            if (writeCount % NotifyThreshold == 0)
            {
                _notifyWrite.Set();
            }
        }

        private volatile bool _shouldExit = false;

        private void WriteThread()
        {
            while (!_shouldExit)
            {
                _notifyWrite.WaitOne(WriteTimeout);
                _cacheStorage.ApplyChanges();
            }
        }

        public void Dispose()
        {
            _shouldExit = true;
            _notifyWrite.Set();
            _writeThread.Join();
            _notifyWrite?.Dispose();
        }
    }
}
