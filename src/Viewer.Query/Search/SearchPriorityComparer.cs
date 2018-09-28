using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.IO;
using Viewer.Query.Expressions;

namespace Viewer.Query.Search
{
    /// <summary>
    /// This comparer compares directories based on given priority function. This class is passed
    /// to the <see cref="FileFinder.GetDirectories()"/> method.
    /// </summary>
    internal class SearchPriorityComparer : IComparer<string>
    {
        private readonly ValueExpression _expression;
        private readonly IPriorityFunction _priority;

        public SearchPriorityComparer(ValueExpression expression, IPriorityFunction priority)
        {
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _priority = priority ?? throw new ArgumentNullException(nameof(priority));
        }

        private double GetPriority(string path)
        {
            var priority = _priority.Compute(_expression, path);
            return priority;
        }

        private int GetLevel(string path)
        {
            var count = 0;
            foreach (var c in path)
            {
                if (PathUtils.PathSeparators.Contains(c))
                {
                    ++count;
                }
            }

            return count;
        }

        /// <summary>
        /// Compare 2 paths <paramref name="x"/> and <paramref name="y"/> based on their
        /// priorities returned by <see cref="GetPriority"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(string x, string y)
        {
            if (x == y)
            {
                return 0;
            }
            var priorityX = GetPriority(x);
            var priorityY = GetPriority(y);
            if (priorityX == priorityY)
            {
                var levelX = GetLevel(x);
                var levelY = GetLevel(y);
                return levelX - levelY;
            }

            return priorityX > priorityY ? -1 : 1;
        }
    }
}
