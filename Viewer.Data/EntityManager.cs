using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MetadataExtractor;
using Viewer.Data.Storage;

namespace Viewer.Data
{
    /// <summary>
    /// Class which implements this interface is responsible for maintaining a consistent state 
    /// of entities throughout the application.
    /// This type is thread safe.
    /// </summary>
    public interface IEntityManager
    {
        /// <summary>
        /// Get entity in memory or load it from an attribute storage.
        /// There will be at most 1 object for each entity at any given time.
        /// </summary>
        /// <param name="path">Path to an enttiy</param>
        /// <returns>Loaded entity</returns>
        IEntity GetEntity(string path);

        /// <summary>
        /// Set entity in the loader
        /// </summary>
        /// <param name="entity">Entity to set</param>
        void SetEntity(IEntity entity);

        /// <summary>
        /// Move an entity from <paramref name="oldPath"/> to <paramref name="newPath"/>
        /// </summary>
        /// <param name="oldPath"></param>
        /// <param name="newPath"></param>
        void MoveEntity(string oldPath, string newPath);

        /// <summary>
        /// Remove entity from the loader
        /// </summary>
        /// <param name="path">Path to an entity</param>
        void RemoveEntity(string path);

        /// <summary>
        /// Get a snapshot of unsaved modified entities.
        /// All entities in the snapshot are copies.
        /// </summary>
        /// <returns>Snapshot of modified entities</returns>
        IReadOnlyList<IEntity> GetModified();
    }

    [Export(typeof(IEntityManager))]
    public class EntityManager : IEntityManager
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Dictionary<string, WeakReference<IEntity>> _entities = new Dictionary<string, WeakReference<IEntity>>();
        private readonly Dictionary<string, IEntity> _modified = new Dictionary<string, IEntity>();
        private readonly IAttributeStorage _storage;

        [ImportingConstructor]
        public EntityManager(IAttributeStorage storage)
        {
            _storage = storage;
        }

        public IEntity GetEntity(string path)
        {
            // check whether the entity is loaded in main memory
            _lock.EnterReadLock();
            var isLoaded = false;
            WeakReference<IEntity> item = null;
            try
            {
                isLoaded = _entities.TryGetValue(path, out item);
            }
            finally
            {
                _lock.ExitReadLock();
            }

            // return loaded entity or load it
            if (isLoaded && item.TryGetTarget(out IEntity entity))
            {
                return entity;
            }

            return LoadEntity(path);
        }

        public void SetEntity(IEntity entity)
        {
            var path = entity.Path;
            var clone = entity.Clone();
            var cacheEntry = new WeakReference<IEntity>(entity);

            _lock.EnterWriteLock();
            try
            {
                _entities[path] = cacheEntry;
                _modified[path] = clone;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void MoveEntity(string oldPath, string newPath)
        {
            _storage.Move(oldPath, newPath);

            _lock.EnterUpgradeableReadLock();
            try
            {
                // check whether the entity is loaded in cache
                if (!_entities.TryGetValue(oldPath, out var value))
                {
                    return;
                }

                // the entity is in the cache => we have to move it
                _lock.EnterWriteLock();
                try
                {
                    _entities.Remove(oldPath);
                    if (!value.TryGetTarget(out var entity))
                    {
                        return;
                    }

                    // add the modified entity to the cache
                    entity.ChangePath(newPath);
                    _entities[newPath] = new WeakReference<IEntity>(entity);

                    // add the modified entity to the modified list
                    _modified.Remove(oldPath);
                    _modified.Add(newPath, entity.Clone());
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public void RemoveEntity(string path)
        {
            _storage.Remove(path);

            _lock.EnterWriteLock();
            try
            {
                _modified.Remove(path);
                _entities.Remove(path);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IReadOnlyList<IEntity> GetModified()
        {
            _lock.EnterWriteLock();
            try
            {
                var snapshot = _modified.Values.ToArray();
                _modified.Clear();
                return snapshot;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private IEntity LoadEntity(string path)
        {
            // load the entity from storage
            var entity = _storage.Load(path);
            if (entity == null)
                return null;

            // chace it
            var cacheEntry = new WeakReference<IEntity>(entity);
            _lock.EnterWriteLock();
            try
            {
                _entities[entity.Path] = cacheEntry;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return entity;
        }
    }
}
