using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Core.Collections
{
    /// <summary>
    /// Extensions of <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Merge 2 sorted lists <paramref name="firstList"/> and <paramref name="secondList"/>
        /// using <paramref name="comparer"/> to compare elements. This does not sort the
        /// collections, the caller guarantees they are sorted by <paramref name="comparer"/>.
        /// </summary>
        /// <typeparam name="T">Type of an element in the collection</typeparam>
        /// <param name="firstList">First sorted list</param>
        /// <param name="secondList">Second sorted list</param>
        /// <param name="comparer">
        ///     Comparer by which elements are sorted in <paramref name="firstList"/> and
        ///     <paramref name="secondList"/>. The returned list is also sorted by this comparer.
        /// </param>
        /// <returns>
        ///     A new list of elements from both <paramref name="firstList"/> and
        ///     <paramref name="secondList"/> sorted by <paramref name="comparer"/>.
        /// </returns>
        public static List<T> Merge<T>(
            this IEnumerable<T> firstList, 
            IEnumerable<T> secondList,
            IComparer<T> comparer)
        {
            var result = new List<T>();

            using (var first = firstList.GetEnumerator())
            using (var second = secondList.GetEnumerator())
            {
                var firstHasNext = first.MoveNext();
                var secondHasNext = second.MoveNext();
                while (firstHasNext || secondHasNext)
                {
                    if (!secondHasNext)
                    {
                        result.Add(first.Current);
                        firstHasNext = first.MoveNext();
                    }
                    else if (!firstHasNext)
                    {
                        result.Add(second.Current);
                        secondHasNext = second.MoveNext();
                    }
                    else
                    {
                        var cmp = comparer.Compare(first.Current, second.Current);
                        if (cmp <= 0)
                        {
                            result.Add(first.Current);
                            firstHasNext = first.MoveNext();
                        }
                        else // if (cmp > 0)
                        {
                            result.Add(second.Current);
                            secondHasNext = second.MoveNext();
                        }
                    }
                }
            }

            return result;
        }
    }
}
