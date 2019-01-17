using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.Expressions
{
    /// <summary>
    /// Reduce constant (sub)expressions to a single constant. This visitor will build a new
    /// expression tree. The returend expression will not modify the column and line information
    /// so that runtime error reporting will be correct.
    /// </summary>
    internal class ConstantReductionVisitor : IExpressionVisitor<ValueExpression>
    {
        private readonly IRuntime _runtime;

        public ConstantReductionVisitor(IRuntime runtime)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public ValueExpression Visit(AndExpression expr)
        {
            var left = expr.Left.Accept(this);
            var right = expr.Right.Accept(this);
            if (left is ConstantExpression leftConstant && leftConstant.Value.IsNull)
            {
                return leftConstant; // the expression is false
            }
            else if (right is ConstantExpression rightConstant && rightConstant.Value.IsNull)
            {
                return rightConstant; // the expression is false
            }
            else if (left is ConstantExpression nonNullConstant && right is ConstantExpression)
            {
                // at this point, both operands are non-null constants, thus the expression is true
                return nonNullConstant;
            }

            return new AndExpression(expr.Line, expr.Column, left, right);
        }

        public ValueExpression Visit(OrExpression expr)
        {
            var left = expr.Left.Accept(this);
            var right = expr.Right.Accept(this);
            if (left is ConstantExpression leftConstant && !leftConstant.Value.IsNull)
            {
                return leftConstant; // the expression is true
            }
            else if (right is ConstantExpression rightConstant && !rightConstant.Value.IsNull)
            {
                return rightConstant; // the expression is true
            }
            else if (left is ConstantExpression nullConstant && right is ConstantExpression)
            {
                // at this point, both operands are null, thus the expression is false
                return nullConstant;
            }

            return new OrExpression(expr.Line, expr.Column, left, right);
        }

        public ValueExpression Visit(NotExpression expr)
        {
            var parameter = expr.Parameter.Accept(this);
            if (parameter is ConstantExpression constant)
            {
                return new ConstantExpression(
                    expr.Line, 
                    expr.Column,
                    new IntValue(constant.Value.IsNull ? (int?) 1 : null));
            }

            return new NotExpression(expr.Line, expr.Column, parameter);
        }

        public ValueExpression Visit(AdditionExpression expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new AdditionExpression(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        public ValueExpression Visit(SubtractionExpression expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new SubtractionExpression(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        public ValueExpression Visit(MultiplicationExpression expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new MultiplicationExpression(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        public ValueExpression Visit(DivisionExpression expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new DivisionExpression(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        public ValueExpression Visit(UnaryMinusExpression expr)
        {
            var result = ProcessUnaryOperator(expr);
            if (result == null)
            {
                return expr;
            }

            return result;
        }

        public ValueExpression Visit(LessThanOperator expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new LessThanOperator(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        public ValueExpression Visit(LessThanOrEqualOperator expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new LessThanOrEqualOperator(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        public ValueExpression Visit(EqualOperator expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new EqualOperator(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        public ValueExpression Visit(NotEqualOperator expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new NotEqualOperator(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        public ValueExpression Visit(GreaterThanOperator expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new GreaterThanOperator(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        public ValueExpression Visit(GreaterThanOrEqualOperator expr)
        {
            var result = ProcessBinaryOperator(expr);
            if (result.Constant == null)
            {
                return new GreaterThanOrEqualOperator(expr.Line, expr.Column, result.Left, result.Right);
            }

            return result.Constant;
        }

        private static readonly IEntity Entity = new FileEntity("__ConstantReduction__");
        
        public ValueExpression Visit(FunctionCallExpression expr)
        {
            var result = ReduceFunction(expr.Name, expr);
            if (result.Constant == null)
            {
                // not all parameters are constant
                return new FunctionCallExpression(expr.Line, expr.Column, expr.Name, result.Children);
            }

            return result.Constant;
        }

        private (
            ConstantExpression Constant,
            ValueExpression Left,
            ValueExpression Right) ProcessBinaryOperator(BinaryOperatorExpression expr)
        {
            var result = ReduceFunction(expr.Name, expr);
            Trace.Assert(result.Children.Count == 2);
            return (result.Constant, result.Children[0], result.Children[1]);
        }

        private ConstantExpression ProcessUnaryOperator(UnaryOperatorExpression expr)
        {
            var result = ReduceFunction(expr.Name, expr);
            return result.Constant;
        }

        /// <summary>
        /// This method takes children expressions of <paramref name="expr"/> as parameters to
        /// a function named <paramref name="name"/>. If all children of <paramref name="expr"/>
        /// are reducible to constant expressions, this function will reduce the whole function
        /// call to a constant expressions. Otherwise, it will only recude all reducible
        /// children subexpressions.
        /// </summary>
        /// <param name="name">Name of the function</param>
        /// <param name="expr">
        /// Expression whose children subexpressions will be regarded as function parameters.
        /// </param>
        /// <returns>
        /// <para>
        /// If all subexpressions of <paramref name="expr"/> are reducible to constants, the
        /// first returned value is the whole reduced function call. Otherwise, the first part
        /// is null.
        /// </para>
        /// 
        /// <para>Reduced subexpression</para>
        /// </returns>
        private (
            ConstantExpression Constant, 
            List<ValueExpression> Children) ReduceFunction(string name, ValueExpression expr)
        {
            // reduce subexpressions
            var children = new List<ValueExpression>();
            foreach (var parameter in expr.Children)
            {
                var reduced = parameter.Accept(this);
                children.Add(reduced);
            }

            if (children.All(subexpr => subexpr is ConstantExpression))
            {
                var values = children
                    .OfType<ConstantExpression>()
                    .Select(item => item.Value)
                    .ToList();
                var context = new ExecutionContext(values, _runtime, Entity, expr.Line, expr.Column);
                var result = _runtime.FindAndCall(name, context);
                var constant = new ConstantExpression(expr.Line, expr.Column, result);
                return (constant, children);
            }

            return (null, children);
        }

        public ValueExpression Visit(ConstantExpression expr)
        {
            return expr;
        }

        public ValueExpression Visit(AttributeAccessExpression expr)
        {
            return expr;
        }
    }
}
