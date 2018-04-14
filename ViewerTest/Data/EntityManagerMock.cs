using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace ViewerTest.Data
{
    class EntityManagerMock : IEntityManager
    {
        private List<IEntity> _entities;

        public EntityManagerMock(params IEntity[] entities)
        {
            _entities = new List<IEntity>(entities);
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
            _entities.RemoveAll(removePredicate);
        }

        public void Clear()
        {
            _entities.Clear();
        }

        public IReadOnlyList<IEntity> GetModified()
        {
            throw new NotImplementedException();
        }

        public int Count => _entities.Count;
        
        public IEntity this[int index]
        {
            get => _entities[index];
            set => _entities[index] = value;
        }
        
    }
}
