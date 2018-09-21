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

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class LessThanOrEqualOperator : BinaryOperatorExpression
    {
        public LessThanOrEqualOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "<=", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class EqualOperator : BinaryOperatorExpression
    {
        public EqualOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "=", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class NotEqualOperator : BinaryOperatorExpression
    {
        public NotEqualOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "!=", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class GreaterThanOperator : BinaryOperatorExpression
    {
        public GreaterThanOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, ">", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class GreaterThanOrEqualOperator : BinaryOperatorExpression
    {
        public GreaterThanOrEqualOperator(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, ">=", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
