using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query.Expressions
{
    internal interface IExpressionVisitor<out T>
    {
        T Visit(AndExpression expr);
        T Visit(OrExpression expr);
        T Visit(NotExpression expr);

        T Visit(AdditionExpression expr);
        T Visit(SubtractionExpression expr);
        T Visit(MultiplicationExpression expr);
        T Visit(DivisionExpression expr);
        T Visit(UnaryMinusExpression expr);

        T Visit(LessThanOperator expr);
        T Visit(LessThanOrEqualOperator expr);
        T Visit(EqualOperator expr);
        T Visit(NotEqualOperator expr);
        T Visit(GreaterThanOperator expr);
        T Visit(GreaterThanOrEqualOperator expr);

        T Visit(FunctionCallExpression expr);
        T Visit(ConstantExpression expr);
        T Visit(AttributeAccessExpression expr);
    }

    /// <summary>
    /// Expression visitor without a return value (it has a boolean return value but the default
    /// implementaion does not use it). The default implementation of the interface will traverse
    /// the whole expression tree.
    /// </summary>
    internal abstract class ExpressionVisitor : IExpressionVisitor<bool>
    {
        public virtual bool Visit(AndExpression expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(OrExpression expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(NotExpression expr)
        {
            expr.Parameter.Accept(this);
            return true;
        }

        public virtual bool Visit(AdditionExpression expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(SubtractionExpression expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(MultiplicationExpression expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(DivisionExpression expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(UnaryMinusExpression expr)
        {
            expr.Parameter.Accept(this);
            return true;
        }

        public virtual bool Visit(LessThanOperator expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(LessThanOrEqualOperator expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(EqualOperator expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(NotEqualOperator expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(GreaterThanOperator expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(GreaterThanOrEqualOperator expr)
        {
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            return true;
        }

        public virtual bool Visit(FunctionCallExpression expr)
        {
            foreach (var child in expr.Parameters)
            {
                child.Accept(this);
            }

            return true;
        }

        public virtual bool Visit(ConstantExpression expr)
        {
            return true;
        }

        public virtual bool Visit(AttributeAccessExpression expr)
        {
            return true;
        }
    }
}
