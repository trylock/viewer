using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Core.Collections
{
    /// <summary>
    /// Extension of a IList , IReadOnlyList types
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Find the lower bound of <paramref name="value"/> in <paramref name="list"/>.
        /// </summary>
        /// <remarks>
        /// Lower bound is the first value which is greater than <paramref name="value"/> according
        /// to <paramref name="comparer"/>. 
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <param name="comparer"></param>
        /// <returns>
        /// The first index which is greater than <paramref name="value"/> or length of the
        /// <paramref name="list"/> if all values are at most <paramref name="value"/>.
        /// </returns>
        public static int LowerBound<T>(this IList<T> list, T value, IComparer<T> comparer)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            var begin = 0;
            var end = list.Count;
            while (begin < end)
            {
                var index = (begin + end) / 2;
                if (comparer.Compare(list[index], value) < 0)
                {
                    begin = index + 1;
                }
                else
                {
                    end = index;
                }
            }

            return end;
        }

        /// <summary>
        /// Same as <see cref="LowerBound{T}(IList{T}, T, IComparer{T})"/> but it uses the
        /// default comparer for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int LowerBound<T>(this IList<T> list, T value)
        {
            return list.LowerBound(value, Comparer<T>.Default);
        }
    }
}
