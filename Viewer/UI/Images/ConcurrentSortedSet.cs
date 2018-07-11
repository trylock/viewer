using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.UI.Images
{
    /// <summary>
    /// Thread safe collection for sharing sorted collection among producer and consumer threads.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentSortedSet<T>
    {
        public IComparer<T> Comparer => _set.KeyComparer;

        private ImmutableSortedSet<T> _set;

        public ConcurrentSortedSet(IComparer<T> comparer)
        {
            _set = ImmutableSortedSet<T>.Empty.WithComparer(comparer);
        }

        public void Add(T item)
        {
            ImmutableSortedSet<T> newSet, oldSet;
            do
            {
                oldSet = _set;
                newSet = oldSet.Add(item);
            } while (Interlocked.CompareExchange(ref _set, newSet, oldSet) != oldSet);
        }

        public IReadOnlyList<T> Consume()
        {
            var empty = ImmutableSortedSet<T>.Empty.WithComparer(Comparer);
            return Interlocked.Exchange(ref _set, empty);
        }
    }
}
