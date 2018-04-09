using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data.Storage;

namespace Viewer.Data
{
    /// <summary>
    /// Entity manager manages entities when they are loaded in memory.
    /// </summary>
    public interface IEntityManager : IEnumerable<IEntity>
    {
        /// <summary>
        /// Try to get entity at given path.
        /// If it is not loaded, load it into the manager.
        /// </summary>
        /// <param name="path">Path of the entity</param>
        /// <returns>Loaded entity or null if it was not found</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is not a valid path</exception>
        IEntity GetEntity(string path);
        
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

        /// <summary>
        /// Remove all entities from memory.
        /// All unsaved changes will be lost.
        /// </summary>
        void Clear();

        /// <summary>
        /// Mark entity as changed. 
        /// </summary>
        /// <param name="entity">Entity</param>
        void Stage(IEntity entity);

        /// <summary>
        /// Try to stage entity. 
        /// If there is an entity with given path, it won't be replaced.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>true iff the entity was staged</returns>
        bool TryStage(IEntity entity);

        /// <summary>
        /// Remove entity from the staged area.
        /// </summary>
        /// <param name="path">Entity to remove</param>
        void Unstage(string path);

        /// <summary>
        /// Move all staged entities to given snapshot.
        /// </summary>
        /// <returns></returns>
        IEntityStageSnapshot ConsumeStaged();
    }
    
    public class CommitProgress
    {
        public IEntity Entity { get; }

        /// <summary>
        /// true iff the save operation finished (successfully or otherwise)
        /// </summary>
        public bool IsFinished { get; }

        public CommitProgress(IEntity entity, bool isFinished)
        {
            Entity = entity;
            IsFinished = isFinished;
        }
    }

    public interface IEntityStageSnapshot : IEnumerable<IEntity>
    {
        /// <summary>
        /// Get number of entities in the snapshot
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Commit the snapshot (save the entities to their files). 
        /// If an exception occurs during a save operation (this includes the progress report function) 
        /// and the entity was not changed, it will be put back into the stage.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">The token will be queried before we try to save an entity</param>
        /// <param name="progress">Report commit progress. Can be null.</param>
        void Commit(CancellationToken cancellationToken, IProgress<CommitProgress> progress);
    }

    public class EntityManager : IEntityManager
    {
        private Dictionary<string, IEntity> _entities = new Dictionary<string, IEntity>();
        private Dictionary<string, IEntity> _staged = new Dictionary<string, IEntity>();

        private IAttributeStorage _storage;

        public EntityManager(IAttributeStorage storage)
        {
            _storage = storage;
        }
        
        public IEnumerator<IEntity> GetEnumerator()
        {
            return _entities.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public void Clear()
        {
            _entities.Clear();
            lock (_staged)
            {
                _staged.Clear();
            }
        }

        public IEntity GetEntity(string path)
        {
            if (_entities.TryGetValue(path, out IEntity entity))
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
        
        public void DeleteEntity(string path)
        {
            Unstage(path);
            _entities.Remove(path);
            _storage.Remove(path);
        }

        public void MoveEntity(string oldPath, string newPath)
        {
            if (_entities.TryGetValue(oldPath, out IEntity entity))
            {
                entity = entity.ChangePath(newPath);
                _entities.Remove(oldPath);
                _entities.Add(entity.Path, entity);
            }

            _storage.Move(oldPath, newPath);
        }

        private void SaveEntity(IEntity entity)
        {
            _storage.Store(entity);
        }

        public void Stage(IEntity entity)
        {
            // update the entity in manager
            _entities[entity.Path] = entity;

            // add it to the staged area
            lock (_staged)
            {
                _staged[entity.Path] = entity;
            }
        }

        public bool TryStage(IEntity entity)
        {
            lock (_staged)
            {
                if (_staged.ContainsKey(entity.Path))
                {
                    return false;
                }

                _entities[entity.Path] = entity;
                _staged[entity.Path] = entity;
                return true;
            }
        }

        public void Unstage(string path)
        {
            lock (_staged)
            {
                _staged.Remove(path);
            }
        }

        private class StagedSnapshot : IEntityStageSnapshot
        {
            public int Count => _snapshot.Count;

            private EntityManager _manager;
            private IList<IEntity> _snapshot;

            public StagedSnapshot(EntityManager manager, IList<IEntity> snapshot)
            {
                _manager = manager;
                _snapshot = snapshot;
            }

            public void Commit(CancellationToken cancellationToken, IProgress<CommitProgress> progress)
            {
                // save all changed entities
                var errors = new List<Exception>();
                foreach (var entity in _snapshot)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        progress?.Report(new CommitProgress(entity, false));
                        _manager.SaveEntity(entity);
                    }
                    catch (Exception e)
                    {
                        // put the entity back to the stage if it wasn't changed during the commit
                        _manager.TryStage(entity);
                        errors.Add(e);
                    }
                    finally
                    {
                        progress?.Report(new CommitProgress(entity, true));
                    }
                }

                if (errors.Count > 0)
                {
                    throw new AggregateException(errors);
                }
            }
            
            public IEnumerator<IEntity> GetEnumerator()
            {
                return _snapshot.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEntityStageSnapshot ConsumeStaged()
        {
            lock (_staged)
            {
                var snapshot = _staged.Values.ToArray();
                _staged.Clear();
                return new StagedSnapshot(this, snapshot);
            }
        }
    }
}
