using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.Suggestions
{
    public class QuerySuggestionComparer : IComparer<IQuerySuggestion>
    {
        public static QuerySuggestionComparer Default { get; } = new QuerySuggestionComparer();

        private static readonly Dictionary<string, int> CategoryPriority = new Dictionary<string, int>
        {
            { TypeId.String.ToString(), 0 },
            { TypeId.Integer.ToString(), 0 },
            { TypeId.Real.ToString(), 0 },
            { TypeId.DateTime.ToString(), 0 },
            { AttributeNameSuggestionProvider.CategoryName, 1 },
            { "Keyword", 2 },
        };

        private int GetCategoryPriority(string category)
        {
            if (CategoryPriority.TryGetValue(category, out var value))
            {
                return value;
            }

            return int.MaxValue;
        }

        public int Compare(IQuerySuggestion x, IQuerySuggestion y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }
            else if (ReferenceEquals(x, null))
            {
                return 1;
            }
            else if (ReferenceEquals(y, null))
            {
                return -1;
            }

            var xPriority = GetCategoryPriority(x.Category);
            var yPriority = GetCategoryPriority(y.Category);
            var priorityDiff = xPriority - yPriority;
            if (priorityDiff == 0)
            {
                return StringComparer.CurrentCulture.Compare(x.Name, y.Name);
            }

            return priorityDiff;
        }
    }
}
