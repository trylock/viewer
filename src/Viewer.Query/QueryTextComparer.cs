using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query
{
    /// <summary>
    /// Compare queries based on their textual representation
    /// </summary>
    public class QueryTextComparer : IComparer<IExecutableQuery>, IEqualityComparer<IExecutableQuery>
    {
        public static QueryTextComparer Default { get; } = new QueryTextComparer();

        public int Compare(IExecutableQuery x, IExecutableQuery y)
        {
            return string.Compare(x?.Text, y?.Text, StringComparison.CurrentCulture);
        }

        public bool Equals(IExecutableQuery x, IExecutableQuery y)
        {
            return string.Equals(x?.Text, y?.Text, StringComparison.CurrentCulture);
        }

        public int GetHashCode(IExecutableQuery obj)
        {
            return obj.Text.GetHashCode();
        }
    }
}
