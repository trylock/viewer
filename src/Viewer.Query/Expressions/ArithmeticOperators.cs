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

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class SubtractionExpression : BinaryOperatorExpression
    {
        public SubtractionExpression(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "-", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class MultiplicationExpression : BinaryOperatorExpression
    {
        public MultiplicationExpression(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "*", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    internal class DivisionExpression : BinaryOperatorExpression
    {
        public DivisionExpression(int line, int column, ValueExpression left, ValueExpression right)
            : base(line, column, "/", left, right)
        {
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
