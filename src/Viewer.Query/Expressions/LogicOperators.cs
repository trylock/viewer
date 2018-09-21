using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query.Expressions
{
    internal class NotExpression : UnaryOperatorExpression
    {
        public NotExpression(int line, int column, ValueExpression parameter)
            : base(line, column, "not", parameter)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class OrExpression : BinaryOperatorExpression
    {
        public OrExpression(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "or", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class AndExpression : BinaryOperatorExpression
    {
        public AndExpression(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "and", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
