using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly IAttributeStorage _storage;
        private readonly IEntityRepository _modified;

        [ImportingConstructor]
        public EntityManager(IAttributeStorage storage, IEntityRepository modified)
        {
            _storage = storage;
            _modified = modified;
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
            _entities[entity.Path] = new WeakReference<IEntity>(entity);
            _modified.Add(entity.Clone());
        }

        public void MoveEntity(string oldPath, string newPath)
        {
            if (_entities.TryGetValue(oldPath, out var value))
            {
                _entities.Remove(oldPath);
                if (value.TryGetTarget(out var entity))
                {
                    _entities[newPath] = new WeakReference<IEntity>(entity);
                }
                _modified.Move(oldPath, newPath);
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
            return _modified.GetSnapshot();
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
