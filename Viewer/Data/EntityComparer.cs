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
        private readonly List<ValueOrder> _order = new List<ValueOrder>();

        public void Add(ValueOrder order)
        {
            _order.Add(order);
        }

        public int Compare(IEntity x, IEntity y)
        {
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
}
