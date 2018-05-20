using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Images
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
            return _values.BinarySearch(item);
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
