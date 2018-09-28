using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query.Search
{
    /// <summary>
    /// Collection of subsets of items of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">Type of items in subsets</typeparam>
    internal class SubsetCollection<T> : IReadOnlyList<IReadOnlyList<T>>
    {
        private readonly List<List<T>> _subsets = new List<List<T>>();

        /// <summary>
        /// Number of subsets in the collection
        /// </summary>
        public int Count => _subsets.Count;

        /// <summary>
        /// Get index of a collection which contains exactly elements of <paramref name="subset"/>.
        /// If there is no such collection, it will be added.
        /// </summary>
        /// <param name="subset">Searched subset</param>
        /// <returns>Index of the subset</returns>
        public int Add(IEnumerable<T> subset)
        {
            var list = subset.ToList();
            list.Sort();

            var index = _subsets.FindIndex(item => item.SequenceEqual(list));
            if (index < 0)
            {
                index = _subsets.Count;
                _subsets.Add(list);
            }

            return index;
        }

        /// <summary>
        /// Find all indices of subsets which contains an item for which
        /// <paramref name="itemPredicate"/> returns true.
        /// </summary>
        /// <param name="itemPredicate">Element predicate</param>
        /// <returns>
        /// Indices of subsets with an item for which <paramref name="itemPredicate"/> returned true
        /// </returns>
        public IEnumerable<int> FindIndices(Predicate<T> itemPredicate)
        {
            for (var i = 0; i < _subsets.Count; ++i)
            {
                var subset = _subsets[i];
                if (subset.Find(itemPredicate) != null)
                {
                    yield return i;
                }
            }
        }

        public IEnumerator<IReadOnlyList<T>> GetEnumerator()
        {
            return _subsets.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IReadOnlyList<T> this[int index] => _subsets[index];
    }
}
