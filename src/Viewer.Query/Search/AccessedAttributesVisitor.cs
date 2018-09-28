using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Query.Expressions;

namespace Viewer.Query.Search
{
    /// <summary>
    /// This class will find all attribute names which are accessed in given expression. Found
    /// names will be stored in a list so that repeated access is fast.
    /// </summary>
    internal class AccessedAttributesVisitor : ExpressionVisitor, IReadOnlyList<string>
    {
        private readonly List<string> _names = new List<string>();

        public AccessedAttributesVisitor(ValueExpression expr)
        {
            expr.Accept(this);
        }

        public override bool Visit(AttributeAccessExpression expr)
        {
            _names.Add(expr.Name);
            return true;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _names.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _names.Count;

        public string this[int index] => _names[index];
    }
}
