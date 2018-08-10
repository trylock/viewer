using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.Core.Collections
{
    /// <summary>
    /// Thread safe sorted collection of items.
    /// Producer threads can <see cref="Add"/> items to the collection and it will remain sorted by <see cref="Comparer"/>.
    /// A consumer thread can take all items in the collection by calling <see cref="Consume"/>.
    /// <see cref="Consume"/> will remove all items from the collections are return them to the caller in an order defined by <see cref="Comparer"/>.
    /// </summary>
    /// <typeparam name="T">Type of an element in the collection</typeparam>
    public class ConcurrentSortedSet<T>
    {
        /// <summary>
        /// Comparer which defines order of items in this collection.
        /// </summary>
        public IComparer<T> Comparer => _set.KeyComparer;

        private ImmutableSortedSet<T> _set;

        /// <summary>
        /// Create a new empty sorted collection with <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">Comparer used to sort this collection</param>
        public ConcurrentSortedSet(IComparer<T> comparer)
        {
            _set = ImmutableSortedSet<T>.Empty.WithComparer(comparer);
        }

        /// <summary>
        /// Add a new item to the collection. The collection will remain sorted by <see cref="Comparer"/>.
        /// If <paramref name="item"/> has been in the collection already, it will *not* be added again.
        /// </summary>
        /// <param name="item">Item to add to his collection</param>
        public void Add(T item)
        {
            ImmutableSortedSet<T> newSet, oldSet;
            do
            {
                oldSet = _set;
                newSet = oldSet.Add(item);
            } while (Interlocked.CompareExchange(ref _set, newSet, oldSet) != oldSet);
        }

        /// <summary>
        /// Take all items currently in the collection, remove them from the collection and return them.
        /// Returend items will be sorted by using <see cref="Comparer"/>.
        /// </summary>
        /// <returns>Sorted snapshot of this collection.</returns>
        public IReadOnlyList<T> Consume()
        {
            var empty = ImmutableSortedSet<T>.Empty.WithComparer(Comparer);
            return Interlocked.Exchange(ref _set, empty);
        }
    }
}
