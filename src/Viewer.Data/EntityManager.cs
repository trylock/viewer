using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MetadataExtractor;
using Viewer.Core.Collections;
using Viewer.Data.Storage;
using Viewer.IO;

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
    /// Proxy of an entity in the saving list. It can revert changes, return the entity to the
    /// modified list or save the entity to its file.
    /// </summary>
    public interface IModifiedEntity : IEntity
    {
        /// <summary>
        /// Revert this entity to its initial state. 
        /// </summary>
        void Revert();

        /// <summary>
        /// Return this entity to the modified list of <see cref="IEntityManager"/>
        /// </summary>
        void Return();

        /// <summary>
        /// Save the entity to its file.
        /// </summary>
        /// <seealso cref="IAttributeStorage.Store"/>
        void Save();
    }

    /// <summary>
    /// Class which implements this interface is responsible for maintaining a consistent state 
    /// of entities throughout the application.
    /// This type is thread safe.
    /// </summary>
    /// <remarks>
    /// Entities are loaded using reader returned from the <see cref="CreateReader"/> method. The
    /// implementation keeps 2 lists: the modified list and the saving list. Lifetime of an entity
    /// is as follows:
    /// <list type="number">
    /// <item>
    /// <description>
    /// Before modifying an entity, its initial state is captured in the modified list by calling
    /// the <see cref="SetEntity"/> method.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// After each modification, the entity state in the modified list is updated by calling the
    /// <see cref="SetEntity"/> method.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// All entities are added to the saving list by calling the <see cref="GetModified"/> method.
    /// Entities from the saving list can be removed in one of the following ways:
    ///     <list type="bullet">
    ///         <item>
    ///         <description>
    ///         Call the <see cref="IModifiedEntity.Return"/> method to return the item to the
    ///         modified list iff an item with the same path has not been added to the list yet
    ///         </description>
    ///         </item>
    ///         <item>
    ///         <description>
    ///         Call the <see cref="IModifiedEntity.Revert"/> method to revert the entity to its
    ///         initial state in the whole application (initial state is the state of the entity
    ///         captured by the first call to the <see cref="SetEntity"/> method)
    ///         </description>
    ///         </item>
    ///         <item>
    ///         <description>
    ///         Call the <see cref="IModifiedEntity.Save"/> method to save the entity to its file.
    ///         </description>
    ///         </item>
    ///     </list>
    /// </description>
    /// </item>
    /// </list>
    /// The <see cref="CreateReader"/> method uses these 2 lists to load an entity. This assures
    /// that the most recent version of an entity will always be loaded even if it has not been
    /// saved in a file yet.
    /// </remarks>
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
        /// Load a single entity. If you are only going to load a single entity, use this method
        /// instead of <see cref="CreateReader"/>. In all other cases, use <see cref="CreateReader"/>
        /// </summary>
        /// <param name="path">Path to a file to load</param>
        /// <returns>Loaded entity</returns>
        /// <seealso cref="IAttributeStorage.Load"/>
        IEntity GetEntity(string path);

        /// <summary>
        /// Create an entity reader. If you're gonna do many load operations, this method can
        /// perform better depending on the used attribute storage implementation.
        /// </summary>
        /// <returns></returns>
        IReadableAttributeStorage CreateReader();

        /// <summary>
        /// Add the entity to the modified list. If an entity with the same path is in the list
        /// already, it will be replaced iff <paramref name="replace"/> is true.
        /// </summary>
        /// <remarks>
        /// This method triggeres the <see cref="Changed"/> event if <paramref name="replace"/>
        /// is true or <paramref name="replace"/> is false and there is no entity with the same
        /// path as <paramref name="entity"/> in the modified list (i.e., if this method adds
        /// <paramref name="entity"/> to the modified list)
        /// </remarks>
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
        IReadOnlyList<IModifiedEntity> GetModified();
    }
    
    [Export(typeof(IEntityManager))]
    public class EntityManager : IEntityManager
    {
        private enum EntityState
        {
            /// <summary>
            /// The entity has not been marked as modified
            /// </summary>
            None,

            /// <summary>
            /// The entity has been marked as modified but it has not been saved in its file yet
            /// </summary>
            Modified,

            /// <summary>
            /// The entity is waiting to be saved 
            /// </summary>
            Saving
        }

        private class EntityProxy : IModifiedEntity
        {
            private readonly EntityManager _entityManager;
            private readonly IEntity _initialState;
            private IEntity _currentState;

            /// <summary>
            /// State of this proxy determines in which list it lies (the modified list or the saving list)
            /// Access to this property should be protected with a lock on the _entityManager._modified
            /// collection.
            /// </summary>
            public EntityState State { get; set; } = EntityState.Modified;

            /// <summary>
            /// Reference to the entity
            /// </summary>
            public IEntity Entity { get; }
            
            public EntityProxy(EntityManager entityManager, IEntity entity)
            {
                _entityManager = entityManager;
                Entity = entity;
                _initialState = Entity.Clone();
                _currentState = _initialState;
            }
            
            /// <summary>
            /// Revert <see cref="Entity"/> to its first state (state at the time of creation of this object)
            /// </summary>
            public void Revert()
            {
                // revert entity to its initial state
                Entity.Set(_initialState);
                _currentState = _initialState;

                // remove this proxy from the modified list if it is there
                var modified = _entityManager._modified;
                lock (modified)
                {
                    State = EntityState.None;
                    if (modified.TryGetValue(Path, out var proxy) && proxy == this)
                    {
                        modified.Remove(Path);
                    }
                }
                
                _entityManager.Changed?.Invoke(_entityManager, new EntityEventArgs(Entity));
            }

            /// <summary>
            /// Return this entity to the modified list iff an entity with the same path has not
            /// been added to that list yet.
            /// </summary>
            public void Return()
            {
                var modified = _entityManager._modified;
                lock (modified)
                {
                    // there is either a more recent version of this proxy in the _modified collection
                    // or this proxy is in the collection thus chaning its state is sufficient
                    State = EntityState.Modified;
                }
            }

            /// <summary>
            /// Save the entity to its file and remove it from the saving list.
            /// </summary>
            public void Save()
            {
                _entityManager._storage.Store(this);
                var modified = _entityManager._modified;
                lock (modified)
                {
                    if (modified.TryGetValue(Path, out var proxy) && proxy == this)
                    {
                        modified.Remove(Path);
                    }
                }
            }

            /// <summary>
            /// Capture current state of <paramref name="entity"/> as the current state.
            /// </summary>
            /// <param name="entity"></param>
            public void Capture(IEntity entity)
            {
                _currentState = entity.Clone();
            }

            #region Entity proxy

            public string Path => _currentState.Path;

            public Attribute GetAttribute(string name)
            {
                return _currentState.GetAttribute(name);
            }

            public T GetValue<T>(string name) where T : BaseValue
            {
                return _currentState.GetValue<T>(name);
            }

            public IEntity SetAttribute(Attribute attr)
            {
                return _currentState.SetAttribute(attr);
            }

            public IEntity RemoveAttribute(string name)
            {
                return _currentState.RemoveAttribute(name);
            }

            public IEntity ChangePath(string newPath)
            {
                Entity.ChangePath(newPath);
                _initialState.ChangePath(newPath);
                _currentState.ChangePath(newPath);

                return this;
            }

            public IEntity Clone()
            {
                return _currentState.Clone();
            }

            public IEntity Set(IEntity entity)
            {
                return _currentState.Set(entity);
            }

            public IEnumerator<Attribute> GetEnumerator()
            {
                return _currentState.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        /// <summary>
        /// This collection represents the modified list and the saving list. Items are
        /// distinguished using the <see cref="EntityProxy.State"/> property. There are basically
        /// 4 options:
        /// 1) An entity is only in the modified list => it will be in this collection
        /// 2) An entity is only in the saving list => the most recent one will be in this collection
        /// 3) An entity is in modified list and in the saving list => the one in the modified list
        ///    will be in this collection (since that entity is newer than any item in the saving list)
        /// 4) An entity is neither in the modified list, nor in the saving list => no item for
        ///    this entity is in this collection
        /// </summary>
        private readonly Dictionary<string, EntityProxy> _modified =
            new Dictionary<string, EntityProxy>(StringComparer.CurrentCultureIgnoreCase);

        private readonly IAttributeStorage _storage;

        [ImportingConstructor]
        public EntityManager(IAttributeStorage storage)
        {
            _storage = storage;
        }

        public event EventHandler<EntityEventArgs> Changed;
        public event EventHandler<EntityEventArgs> Deleted;
        public event EventHandler<EntityMovedEventArgs> Moved;

        private class Reader : IReadableAttributeStorage
        {
            private readonly EntityManager _entityManager;
            private readonly IReadableAttributeStorage _reader;

            public Reader(EntityManager entityManager, IReadableAttributeStorage reader)
            {
                _entityManager = entityManager;
                _reader = reader;
            }

            public IEntity Load(string path)
            {
                return _entityManager.GetEntityImpl(_reader, path);
            }

            public void Dispose()
            {
                _reader?.Dispose();
            }
        }

        private IEntity GetEntityImpl(IReadableAttributeStorage storage, string path)
        {
            var normalizedPath = PathUtils.NormalizePath(path);
            lock (_modified)
            {
                if (_modified.TryGetValue(normalizedPath, out var proxy))
                {
                    return proxy.Entity;
                }
            }

            return storage.Load(path);
        }

        public IEntity GetEntity(string path)
        {
            return GetEntityImpl(_storage, path);
        }

        public IReadableAttributeStorage CreateReader()
        {
            return new Reader(this, _storage.CreateReader());
        }

        public void SetEntity(IEntity entity, bool replace)
        {
            var isAdded = false;
            var path = entity.Path;
            lock (_modified)
            {
                if (!_modified.TryGetValue(path, out var proxy) ||
                    proxy.State != EntityState.Modified)
                {
                    _modified[path] = new EntityProxy(this, entity);
                    isAdded = true;
                }
                else if (replace) // if it is allowed to replace the entity in the modified list
                {
                    proxy.Capture(entity);
                    isAdded = true;
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
                    _modified[newPath] = (EntityProxy) modifiedEntity.ChangePath(newPath);
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

        public IReadOnlyList<IModifiedEntity> GetModified()
        {
            var snapshot = new List<EntityProxy>();
            lock (_modified)
            {
                foreach (var item in _modified)
                {
                    var proxy = item.Value;
                    if (proxy.State == EntityState.Modified)
                    {
                        proxy.State = EntityState.Saving;
                        snapshot.Add(proxy);
                    }
                }
            }

            return snapshot;
        }
    }
}
