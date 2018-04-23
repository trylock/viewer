using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    /// <inheritdoc />
    /// <summary>
    /// EntityManager represents a result of an ordered query.
    /// It also keeps track of all changes to entities in the result and provides an API to manage them.
    /// </summary>
    public interface IEntityManager  : IEnumerable<IEntity>
    {
        /// <summary>
        /// Number of entities
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Get/set entity
        /// </summary>
        /// <param name="index">index of an entity</param>
        /// <returns>entity at given index</returns>
        IEntity this[int index] { get; set; }

        /// <summary>
        /// Add entity to the end
        /// </summary>
        /// <param name="entity">New entity</param>
        void Add(IEntity entity);
        
        /// <summary>
        /// Remove all entities for which given predicate returns true.
        /// </summary>
        /// <param name="removePredicate">Remove predicate</param>
        void RemoveAll(Predicate<IEntity> removePredicate);

        /// <summary>
        /// Remove all entities.
        /// </summary>
        void Clear();

        /// <summary>
        /// Move all modified entities in the query result to a snapshot and return it.
        /// </summary>
        /// <returns>Snapshot of modified entities.</returns>
        IReadOnlyList<IEntity> GetModified();
    }

    public class EntityManager : IEntityManager
    {
        public int Count => _entities.Count;
        
        private readonly List<IEntity> _entities = new List<IEntity>();
        private readonly IEntityRepository _modified;

        public EntityManager(IEntityRepository modified)
        {
            _modified = modified;
        }
        
        public IEnumerator<IEntity> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(IEntity item)
        {
            _entities.Add(item);
        }

        public void RemoveAll(Predicate<IEntity> removePredicate)
        {
            // unstage removed entities
            foreach (var entity in _entities)
            {
                if (removePredicate(entity))
                {
                    _modified.Remove(entity.Path);
                }
            }

            // remove entities
            _entities.RemoveAll(removePredicate);
        }

        public void Clear()
        {
            _entities.Clear();
            _modified.Clear();
        }

        public IEntity this[int index]
        {
            get => _entities[index];
            set
            {
                _entities[index] = value;
                _modified.Add(value);
            }
        }
        
        public IReadOnlyList<IEntity> GetModified()
        {
            return _modified.GetSnapshot();
        }
    }
}
