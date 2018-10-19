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

        /// <summary>
        /// Same as <see cref="ToHashSet{T}(System.Collections.Generic.IEnumerable{T})"/> but
        /// it uses <see cref="EqualityComparer{T}.Default"/> comparer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> list)
        {
            return ToHashSet(list, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Convert values from <paramref name="list"/> to a hash set.
        /// </summary>
        /// <typeparam name="T">Type of an item</typeparam>
        /// <param name="list">List of items</param>
        /// <param name="comparer">Comparer of items</param>
        /// <returns>
        /// Hash set which contains exactly elements from <paramref name="list"/>
        /// </returns>
        public static HashSet<T> ToHashSet<T>(
            this IEnumerable<T> list,
            IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(list, comparer);
        }

        /// <summary>
        /// If <paramref name="items"/> contains a single item equal to <paramref name="item"/>,
        /// it will return an empty set. Otherwise, <paramref name="items"/> will be returned.
        /// </summary>
        /// <typeparam name="T">Type of an item</typeparam>
        /// <param name="items">Collection of items</param>
        /// <param name="item">
        /// If <paramref name="items"/> contains exactly this item (i.e., no other item).
        /// </param>
        /// <param name="comparer">
        /// Equality comparer used to compare the first item from <paramref name="items"/>
        /// with <paramref name="item"/>
        /// </param>
        /// <returns>
        /// <paramref name="items"/> exepct if it only contains <paramref name="item"/>. In that
        /// case, an empty enumerable will be returned.
        /// </returns>
        public static IEnumerable<T> SkipSingletonWith<T>(
            this IEnumerable<T> items,
            T item,
            IEqualityComparer<T> comparer)
        {
            using (var enumerator = items.GetEnumerator())
            {
                // if it is empty, we are done
                var hasNext = enumerator.MoveNext();
                if (!hasNext)
                {
                    yield break;
                }

                // move past the first item
                var firstItem = enumerator.Current;
                hasNext = enumerator.MoveNext();

                // if there is only one item, check whether we should exit
                if (!hasNext && comparer.Equals(enumerator.Current, item))
                {
                    yield break;
                }

                // return the first item and item after that
                yield return firstItem;
                if (hasNext)
                {
                    yield return enumerator.Current;
                }

                // return the rest of the items
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }
    }
}
