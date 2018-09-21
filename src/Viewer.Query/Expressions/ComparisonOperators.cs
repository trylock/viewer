using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query.Expressions
{
    internal class LessThanOperator : BinaryOperatorExpression
    {
        public LessThanOperator(int line, int column, ValueExpression left, ValueExpression right) 
            : base(line, column, "<", left, right)
        {
        }
    }

    internal class LessThanOrEqualOperator : BinaryOperatorExpression
    {
        public LessThanOrEqualOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "<=", left, right)
        {
        }
    }

    internal class EqualOperator : BinaryOperatorExpression
    {
        public EqualOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "=", left, right)
        {
        }
    }

    internal class NotEqualOperator : BinaryOperatorExpression
    {
        public NotEqualOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "!=", left, right)
        {
        }
    }

    internal class GreaterThanOperator : BinaryOperatorExpression
    {
        public GreaterThanOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, ">", left, right)
        {
        }
    }

    internal class GreaterThanOrEqualOperator : BinaryOperatorExpression
    {
        public GreaterThanOrEqualOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, ">=", left, right)
        {
        }
    }
}
