using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public class ValueOrder
    {
        /// <summary>
        /// Value getter. Given an entity, compute the value based on which we will sort the values.
        /// </summary>
        public Func<IEntity, BaseValue> Getter { get; set; }

        /// <summary>
        /// Sort direction
        /// </summary>
        public int Direction { get; set; } = 1;
    }

    public class EntityComparer : IComparer<IEntity>
    {
        private readonly List<ValueOrder> _order;

        public static EntityComparer Default { get; } = new EntityComparer();

        public EntityComparer()
        {
            _order = new List<ValueOrder>();
        }

        public EntityComparer(List<ValueOrder> order)
        {
            _order = order;
        }

        public int Compare(IEntity x, IEntity y)
        {
            if (x is DirectoryEntity && y is DirectoryEntity)
            {
                return Comparer<string>.Default.Compare(x.Path, y.Path);
            }
            else if (x is DirectoryEntity)
            {
                return -1;
            }
            else if (y is DirectoryEntity)
            {
                return 1;
            }

            foreach (var order in _order)
            {
                var valueA = order.Getter(x);
                var valueB = order.Getter(y);
                var result = ValueComparer.Default.Compare(valueA, valueB) * order.Direction;
                if (result != 0)
                    return result;
            }

            return Comparer<string>.Default.Compare(x?.Path, y?.Path);
        }
    }

    public class EntityPathEqualityComparer : IEqualityComparer<IEntity>
    {
        public static EntityPathEqualityComparer Default { get; } = new EntityPathEqualityComparer();

        public bool Equals(IEntity x, IEntity y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;
            return x.Path == y.Path;
        }

        public int GetHashCode(IEntity obj)
        {
            return obj.Path.GetHashCode();
        }
    }
}
