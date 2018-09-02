using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MetadataExtractor;
using Viewer.Data.Storage;

namespace Viewer.Data
{
    public class EntityEventArgs : EventArgs
    {
        /// <summary>
        /// New value of an entity.
        /// </summary>
        public IEntity Value { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create new entity event arguments.
        /// </summary>
        /// <param name="value">Modified entity</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
        public EntityEventArgs(IEntity value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    public class EntityMovedEventArgs : EntityEventArgs
    {
        /// <summary>
        /// Old path of the entity.
        /// </summary>
        public string OldPath { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create new entity changed event arguments.
        /// </summary>
        /// <param name="oldPath">Old path of an entity</param>
        /// <param name="newValue">New value of the entity</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="oldPath"/> or <paramref name="newValue"/> is null
        /// </exception>
        public EntityMovedEventArgs(string oldPath, IEntity newValue) : base(newValue)
        {
            OldPath = oldPath ?? throw new ArgumentNullException(nameof(oldPath));
        }
    }

    /// <summary>
    /// Class which implements this interface is responsible for maintaining a consistent state 
    /// of entities throughout the application.
    /// This type is thread safe.
    /// </summary>
    public interface IEntityManager
    {
        /// <summary>
        /// Event occurs whenever an entity changes due to calling <see cref="SetEntity"/>.
        /// This means that only attributes have changed, not its path. See <see cref="Moved"/>.
        /// </summary>
        event EventHandler<EntityEventArgs> Changed;

        /// <summary>
        /// Event occurs whenever an entity is deleted due to calling <see cref="RemoveEntity"/>.
        /// </summary>
        event EventHandler<EntityEventArgs> Deleted;

        /// <summary>
        /// Event occurs whenever an entity is moved due to calling <see cref="MoveEntity"/>.
        /// </summary>
        event EventHandler<EntityMovedEventArgs> Moved;

        /// <summary>
        /// Load it from an attribute storage. See <see cref="IAttributeStorage.Load"/> for the list of
        /// possible exceptions this method can throw.
        /// </summary>
        /// <param name="path">Path to an entity</param>
        /// <returns>Loaded entity</returns>
        IEntity GetEntity(string path);

        /// <summary>
        /// Add the entity to the modified list. 
        /// If an entity with the same path is in the list already, it will be replaced iff
        /// <paramref name="replace"/> is true.
        /// </summary>
        /// <param name="entity">Entity to set</param>
        /// <param name="replace">Replace entity in the modified list if true.</param>
        void SetEntity(IEntity entity, bool replace);

        /// <summary>
        /// Move <paramref name="entity"/> to <paramref name="newPath"/>
        /// in the storage and in the modified list.
        /// This will set entity path to <paramref name="newPath"/> if the move was successful.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newPath"></param>
        void MoveEntity(IEntity entity, string newPath);

        /// <summary>
        /// Remove entity in the storage and in the modified list.
        /// </summary>
        /// <param name="entity">Entity to remove</param>
        void RemoveEntity(IEntity entity);

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
        private readonly Dictionary<string, IEntity> _modified = new Dictionary<string, IEntity>();
        private readonly IAttributeStorage _storage;

        [ImportingConstructor]
        public EntityManager(IAttributeStorage storage)
        {
            _storage = storage;
        }

        public event EventHandler<EntityEventArgs> Changed;
        public event EventHandler<EntityEventArgs> Deleted;
        public event EventHandler<EntityMovedEventArgs> Moved;

        public IEntity GetEntity(string path)
        {
            return _storage.Load(path);
        }

        public void SetEntity(IEntity entity, bool replace)
        {
            var isAdded = false;
            var path = entity.Path;
            var clone = entity.Clone();
            if (replace)
            {
                isAdded = true;
                lock (_modified)
                {
                    _modified[path] = clone;
                }
            }
            else
            {
                lock (_modified)
                {
                    if (!_modified.ContainsKey(path))
                    {
                        _modified.Add(path, clone);
                        isAdded = true;
                    }
                }
            }

            // invoke entity changed event
            if (isAdded)
            {
                Changed?.Invoke(this, new EntityEventArgs(entity));
            }
        }

        public void MoveEntity(IEntity entity, string newPath)
        {
            var oldPath = entity.Path;
            _storage.Move(entity, newPath);

            lock (_modified)
            {
                if (_modified.TryGetValue(oldPath, out var modifiedEntity))
                {
                    _modified.Remove(oldPath);
                    _modified[newPath] = modifiedEntity.ChangePath(newPath);
                }
            }

            var movedEntity = entity.ChangePath(newPath);

            Moved?.Invoke(this, new EntityMovedEventArgs(oldPath, movedEntity));
        }

        public void RemoveEntity(IEntity entity)
        {
            _storage.Delete(entity);

            lock (_modified)
            {
                _modified.Remove(entity.Path);
            }

            Deleted?.Invoke(this, new EntityEventArgs(entity));
        }

        public IReadOnlyList<IEntity> GetModified()
        {
            IReadOnlyList<IEntity> snapshot;
            lock (_modified)
            {
                snapshot = _modified.Values.ToArray();
                _modified.Clear();
            }

            return snapshot;
        }
    }
}
