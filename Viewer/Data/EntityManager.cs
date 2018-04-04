using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    /// <summary>
    /// Entity manager manages entities when they are loaded in memory.
    /// </summary>
    public interface IEntityManager : IDisposable, IEnumerable<Entity>
    {
        /// <summary>
        /// Make sure all changed entities loaded in the manager are saved on a disk.
        /// </summary>
        void Persist();

        /// <summary>
        /// Free all entities from memory.
        /// All unsaved changes will be lost.
        /// </summary>
        void Clear();

        /// <summary>
        /// Try to get entity at given path.
        /// If it is not loaded, load it into the manager.
        /// </summary>
        /// <param name="path">Path of the entity</param>
        /// <returns>Loaded entity or null if it was not found</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is not a valid path</exception>
        Entity GetEntity(string path);

        /// <summary>
        /// Add a new entity to the manager.
        /// If an entity with the same path already exists, it will be replaced.
        /// IsDirty flag will be set.
        /// </summary>
        /// <param name="entity">New entity</param>
        void AddEntity(Entity entity);

        /// <summary>
        /// Permanently delete an entity and all its attributes.
        /// No exception is thrown if no entity was not found at given path.
        /// Entity does not need to be loaded in memory.
        /// If it is loaded in memory, it will be removed.
        /// </summary>
        /// <param name="path">Path to an entity</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is not a valid path</exception>
        void DeleteEntity(string path);

        /// <summary>
        /// Move an entity from <paramref name="oldPath"/> to <paramref name="newPath"/>.
        /// The moved entity does not have to be loaded in memory. 
        /// If it is loaded in memory, its Path property will be modified.
        /// </summary>
        /// <param name="oldPath">Old path of an entity</param>
        /// <param name="newPath">New path to entity</param>
        void MoveEntity(string oldPath, string newPath);
    }

    public class EntityManager : IEntityManager
    {
        private Dictionary<string, Entity> _entities = new Dictionary<string, Entity>();

        private IAttributeStorage _storage;

        public EntityManager(IAttributeStorage storage)
        {
            _storage = storage;
        }

        public void Dispose()
        {
            foreach (var pair in _entities)
            {
                pair.Value.Dispose();
            }
            _entities.Clear();
        }

        public IEnumerator<Entity> GetEnumerator()
        {
            return _entities.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Persist()
        {
            // perform all pending writes
            _storage.Flush();

            // write all changed attributes
            foreach (var pair in _entities)
            {
                var entity = pair.Value;
                if (entity.IsDirty)
                {
                    _storage.Store(entity);
                }
            }
        }

        public void Clear()
        {
            Dispose();
        }

        public Entity GetEntity(string path)
        {
            if (_entities.TryGetValue(path, out Entity entity))
            {
                return entity;
            }

            entity = _storage.Load(path);
            if (entity == null)
            {
                return null;
            }

            _entities.Add(entity.Path, entity);
            return entity;
        }

        public void AddEntity(Entity entity)
        {
            entity.SetDirty();
            if (_entities.ContainsKey(entity.Path))
            {
                _entities.Remove(entity.Path);
            }
            _entities.Add(entity.Path, entity);
        }

        public void DeleteEntity(string path)
        {
            if (_entities.TryGetValue(path, out Entity entity))
            {
                entity.Dispose();
                _entities.Remove(path);
            }
            
            _storage.Remove(path);
        }

        public void MoveEntity(string oldPath, string newPath)
        {
            if (_entities.TryGetValue(oldPath, out Entity entity))
            {
                _entities.Remove(oldPath);
                entity.Path = newPath;
                _entities.Add(entity.Path, entity);
            }

            _storage.Move(oldPath, newPath);
        }
    }
}
