using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Core.Collections
{
    /// <inheritdoc cref="ICollection{T}" />
    /// <inheritdoc cref="IReadOnlyList{T}" />
    /// <summary>
    /// Sorted list is a list of items sorted using <see cref="Comparer" />.
    /// Random access to elements is a constant time operation.
    /// Collection modification is an <c>O(n)</c> operation where <c>n</c> is <see cref="Count"/>.
    /// </summary>
    /// <typeparam name="T">Type of an element in the collection</typeparam>
    public class SortedList<T> : ICollection<T>, IReadOnlyList<T>
    {
        private readonly List<T> _values = new List<T>();

        /// <summary>
        /// Comparer used to sort items in the collection
        /// </summary>
        public IComparer<T> Comparer { get; }
        
        public int Count => _values.Count;

        public bool IsReadOnly => false;
        
        /// <summary>
        /// Create an empty sorted list with <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">Comparer of this list</param>
        public SortedList(IComparer<T> comparer)
        {
            Comparer = comparer;
        }

        /// <inheritdoc />
        /// <summary>
        /// Create an empty sorted list with <see cref="P:System.Collections.Generic.Comparer`1.Default" /> as a comparer.
        /// </summary>
        public SortedList() : this(Comparer<T>.Default)
        {
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        /// <summary>
        /// Add <paramref name="item"/> to the list. The list will remain sorted.
        /// Time complexity of this operation is <c>O(n)</c> where <c>n</c> is <see cref="Count"/>.
        /// </summary>
        /// <param name="item">Item added to the list</param>
        public void Add(T item)
        {
            _values.Add(item);

            var index = _values.Count - 2;
            while (index >= 0)
            {
                var comparison = Comparer.Compare(_values[index], _values[index + 1]);
                if (comparison > 0)
                {
                    var tmp = _values[index];
                    _values[index] = _values[index + 1];
                    _values[index + 1] = tmp;
                    --index;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Merge 2 sorted collections to a new SortedList.
        /// Neither collection will be modified. A new collection will be allocated, instead.
        /// Time complexity of this operation is <c>O(n)</c> where <c>n</c> is <see cref="Count"/> + number of items in <paramref name="other"/>.
        /// This method does not check whether <paramref name="other"/> is sorted.
        /// </summary>
        /// <param name="other">Other sorted collection which will be merged with this list.</param>
        /// <returns>A new sorted list which contains all elements from both lists.</returns>
        public SortedList<T> Merge(IEnumerable<T> other)
        {
            var result = new SortedList<T>(Comparer);
            using (var left = GetEnumerator())
            using (var right = other.GetEnumerator())
            {
                var leftHasNext = left.MoveNext();
                var rightHasNext = right.MoveNext();
                while (leftHasNext || rightHasNext)
                {
                    if (!leftHasNext)
                    {
                        result._values.Add(right.Current);
                        rightHasNext = right.MoveNext();
                    }
                    else if (!rightHasNext)
                    {
                        result._values.Add(left.Current);
                        leftHasNext = left.MoveNext();
                    }
                    else
                    {
                        var cmp = Comparer.Compare(left.Current, right.Current);
                        if (cmp <= 0)
                        {
                            result._values.Add(left.Current);
                            leftHasNext = left.MoveNext();
                        }
                        else
                        {
                            result._values.Add(right.Current);
                            rightHasNext = right.MoveNext();
                        }
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        /// <summary>
        /// Remove all items in the collection.
        /// Time complexity of this operation <c>O(n)</c> where <c>n</c> is <see cref="P:Viewer.Core.Collections.SortedList`1.Count" />
        /// </summary>
        public void Clear()
        {
            _values.Clear();
        }

        /// <inheritdoc />
        /// <summary>
        /// Check whether <paramref name="item" /> is in the collection.
        /// Time complexity of this operation is <c>O(log(n))</c> where <c>n</c> is <see cref="P:Viewer.Core.Collections.SortedList`1.Count" />
        /// </summary>
        /// <param name="item">Searched item</param>
        /// <returns>true iff <paramref name="item" /> is in this list</returns>
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }
        
        public void CopyTo(T[] array, int arrayIndex)
        {
            _values.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _values.Remove(item);
        }

        /// <summary>
        /// Find index of <paramref name="item"/> in the list.
        /// Time complexity of this operation is <c>O(log(n))</c> where <c>n</c> is <see cref="Count"/>.
        /// </summary>
        /// <param name="item">Searched item</param>
        /// <returns>Index of <paramref name="item"/> or -1 if <paramref name="item"/> is not in the collection</returns>
        public int IndexOf(T item)
        {
            return _values.BinarySearch(item, Comparer);
        }

        public void RemoveAt(int index)
        {
            _values.RemoveAt(index);
        }

        public void RemoveAll(Predicate<T> predicate)
        {
            _values.RemoveAll(predicate);
        }

        public T this[int index]
        {
            get => _values[index];
            set => _values[index] = value;
        }
    }
}
