using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    /// <summary>
    /// EntityRepository is a thread safe collection of modified entities.
    /// </summary>
    public interface IEntityRepository
    {
        /// <summary>
        /// Add an entity to the repository.
        /// </summary>
        /// <param name="entity">New entity</param>
        void Add(IEntity entity);

        /// <summary>
        /// Try to add an entity to the repository. 
        /// If there is an entity with the same path already, <paramref name="entity"/> won't be added.
        /// </summary>
        /// <param name="entity">New entity</param>
        /// <returns>true iff <paramref name="entity"/> has been added</returns>
        bool TryAdd(IEntity entity);

        /// <summary>
        /// Remove entity with <paramref name="path"/> from the repository
        /// </summary>
        /// <param name="path">Path to an entity</param>
        void Remove(string path);

        /// <summary>
        /// Move entity from <paramref name="oldPath"/> to <paramref name="newPath"/>
        /// </summary>
        /// <param name="oldPath"></param>
        /// <param name="newPath"></param>
        void Move(string oldPath, string newPath);

        /// <summary>
        /// Remove all entities.
        /// </summary>
        void Clear();

        /// <summary>
        /// Move all entities currently in the repository to a snapshot and return it.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<IEntity> GetSnapshot();
    }
    
    [Export(typeof(IEntityRepository))]
    public class EntityRepository : IEntityRepository
    {
        private Dictionary<string, IEntity> _modified = new Dictionary<string, IEntity>();
        
        public virtual void Add(IEntity entity)
        {
            lock (_modified)
            {
                _modified[entity.Path] = entity;
            }
        }

        public bool TryAdd(IEntity entity)
        {
            lock (_modified)
            {
                if (_modified.ContainsKey(entity.Path))
                {
                    return false;
                }
                _modified.Add(entity.Path, entity);
                return true;
            }
        }

        public virtual void Remove(string path)
        {
            lock (_modified)
            {
                _modified.Remove(path);
            }
        }

        public virtual void Move(string oldPath, string newPath)
        {
            lock (_modified)
            {
                if (!_modified.TryGetValue(oldPath, out IEntity value))
                {
                    return;
                }

                _modified.Remove(oldPath);
                _modified[newPath] = value.ChangePath(newPath);
            }
        }

        public void Clear()
        {
            lock (_modified)
            {
                _modified.Clear();
            }
        }

        public IReadOnlyList<IEntity> GetSnapshot()
        {
            lock (_modified)
            {
                var values = _modified.Values.ToList();
                _modified.Clear();
                return values;
            }
        }
    }
}
