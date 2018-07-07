using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using Viewer.Data.Storage;

namespace Viewer.Data
{
    /// <summary>
    /// Class which implements this interface is responsible for maintaining a consistent state 
    /// of entities throughout the application. 
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
            if (!_entities.TryGetValue(path, out var item) ||
                !item.TryGetTarget(out IEntity entity))
            {
                return LoadEntity(path);
            }

            return entity;
        }

        public void SetEntity(IEntity entity)
        {
            var path = entity.Path;
            _entities[path] = new WeakReference<IEntity>(entity);
            _modified[path] = entity.Clone();
        }

        public void MoveEntity(string oldPath, string newPath)
        {
            _storage.Move(oldPath, newPath);

            if (_entities.TryGetValue(oldPath, out var value))
            {
                _entities.Remove(oldPath);
                if (value.TryGetTarget(out var entity))
                {
                    // add the modified entity to the cache
                    entity.ChangePath(newPath);
                    _entities[newPath] = new WeakReference<IEntity>(entity);
                    
                    // add the modified entity to the modified list
                    _modified.Remove(oldPath);
                    _modified.Add(newPath, entity.Clone());
                }
            }
        }

        public void RemoveEntity(string path)
        {
            _storage.Remove(path);
            _modified.Remove(path);
            _entities.Remove(path);
        }

        public IReadOnlyList<IEntity> GetModified()
        {
            var snapshot = _modified.Values.ToArray();
            _modified.Clear();
            return snapshot;
        }

        private IEntity LoadEntity(string path)
        {
            var entity = _storage.Load(path);
            if (entity == null)
                return null;
            _entities[entity.Path] = new WeakReference<IEntity>(entity);
            return entity;
        }
    }
}
