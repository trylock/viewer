using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.IO;

namespace Viewer.Query.Search
{
    /// <summary>
    /// This directory comparer forces the <see cref="FileFinder"/> to search directories in a BFS
    /// order. It determines the level of a directory by the number of directory separators.
    /// Therefore, the compared paths have to have a unified format.
    /// </summary>
    internal class BreadthFirstSearchComparer : IComparer<string>
    {
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

        public int Compare(string x, string y)
        {
            var xLevel = GetLevel(x);
            var yLevel = GetLevel(y);
            return Comparer<int>.Default.Compare(xLevel, yLevel);
        }
    }
}
