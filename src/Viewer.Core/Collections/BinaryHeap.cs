using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Core.Collections
{
    /// <summary>
    /// Binary min-heap. Order of items is defined by <see cref="Comparer"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of an item in this heap</typeparam>
    public class BinaryHeap<TValue> : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        private readonly List<TValue> _heap = new List<TValue>();

        /// <summary>
        /// Comparer used to sort values in the heap
        /// </summary>
        public IComparer<TValue> Comparer { get; }

        /// <summary>
        /// Number of items in the heap.
        /// </summary>
        public int Count => _heap.Count;

        /// <summary>
        /// Create a new empty binary heap with default comparer (<see cref="Comparer{TValue}.Default"/>)
        /// </summary>
        public BinaryHeap() : this(Comparer<TValue>.Default)
        {
        }

        /// <summary>
        /// Create a new empty binary heap with <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">Comparer by which items in the heap will be sorted.</param>
        public BinaryHeap(IComparer<TValue> comparer)
        {
            Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        /// <summary>
        /// Add <paramref name="item"/> to the queue at its position in order defined by
        /// <see cref="Comparer"/>.
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(TValue item)
        {
            _heap.Add(item);
            FixUp(_heap.Count - 1);
        }

        /// <summary>
        /// Remove minimal item from the heap and return it. Minimal item is determined using
        /// <see cref="Comparer"/>.
        /// </summary>
        /// <returns>Removed minimal item from the queue</returns>
        public TValue Dequeue()
        {
            if (_heap.Count <= 0)
                throw new InvalidOperationException("The heap is empty.");

            var root = _heap[0];

            // move to last item to the root and remove the last item
            _heap[0] = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);

            FixDown(0);
            return root;
        }

        /// <summary>
        /// Fix the heap invariante on the way up from node at index <paramref name="index"/>
        /// </summary>
        /// <param name="index">Index of a node to fix</param>
        private void FixUp(int index)
        {
            while (index > 0)
            {
                var parentIndex = (index - 1) / 2;
                if (Comparer.Compare(_heap[parentIndex], _heap[index]) <= 0)
                {
                    break; // the parent is smaller than its child => we are done
                }

                // swap this item with its parent
                Swap(parentIndex, index);

                // move to the parent
                index = parentIndex;
            }
        }

        /// <summary>
        /// Fix the heap invariant on the way down from node at index <paramref name="index"/>
        /// </summary>
        /// <param name="index">Index of a node to fix</param>
        private void FixDown(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var childIndex = index * 2 + 1;
            while (childIndex < _heap.Count)
            {
                if (childIndex + 1 < _heap.Count &&
                    Comparer.Compare(_heap[childIndex + 1], _heap[childIndex]) < 0)
                {
                    // the right child is the smallest one from the 2
                    ++childIndex;
                }

                if (Comparer.Compare(_heap[index], _heap[childIndex]) < 0)
                {
                    // the parent is smaller than its smallest child => the invariant is restored
                    break;
                }

                // swap parent with the selected child node
                Swap(index, childIndex);

                // move to the selected child node
                index = childIndex;
                childIndex = index * 2 + 1;
            }
        }

        /// <summary>
        /// Swap positions of 2 nodes in the heap.
        /// </summary>
        /// <param name="firstIndex">Index of a node in the heap</param>
        /// <param name="secondIndex">Index of a node in the heap</param>
        private void Swap(int firstIndex, int secondIndex)
        {
            var temp = _heap[firstIndex];
            _heap[firstIndex] = _heap[secondIndex];
            _heap[secondIndex] = temp;
        }

        #region ICollection 

        public bool IsReadOnly => false;

        public void Add(TValue item)
        {
            Enqueue(item);
        }

        public void Clear()
        {
            _heap.Clear();
        }

        public bool Contains(TValue item)
        {
            return _heap.Contains(item);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            _heap.CopyTo(array, arrayIndex);
        }

        public bool Remove(TValue item)
        {
            return _heap.Remove(item);
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return _heap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
