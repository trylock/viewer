using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query.Expressions
{
    internal class AdditionExpression : BinaryOperatorExpression
    {
        public AdditionExpression(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "+", left, right)
        {
        }
    }

    internal class SubtractionExpression : BinaryOperatorExpression
    {
        public SubtractionExpression(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "-", left, right)
        {
        }
    }

    internal class MultiplicationExpression : BinaryOperatorExpression
    {
        public MultiplicationExpression(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "*", left, right)
        {
        }
    }

    internal class DivisionExpression : BinaryOperatorExpression
    {
        public DivisionExpression(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "/", left, right)
        {
        }
    }
}
