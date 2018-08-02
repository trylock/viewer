using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Collections
{
    public class SortedList<T> : IEnumerable<T>
    {
        private readonly List<T> _values = new List<T>();

        public IComparer<T> Comparer { get; }

        public int Count => _values.Count;

        public bool IsReadOnly => false;
        
        public SortedList(IComparer<T> comparer)
        {
            Comparer = comparer;
        }

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

        /// <summary>
        /// Add a new item to the list.
        /// The list will remain sorted.
        /// Time complexity of this operation is linear with the size of this collection.
        /// </summary>
        /// <param name="item"></param>
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
        /// Neither collection will be modified.
        /// Time complexity of this operation is linear with the combined size of the 2 collections.
        /// This method does not check whether the second collection is sorted.
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

        public void Clear()
        {
            _values.Clear();
        }

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
