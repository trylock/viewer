using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.Expressions
{
    internal abstract class BinaryOperatorExpression : ValueExpression
    {
        /// <summary>
        /// Name of the operator (e.g. +, -, *, /, and, or etc.)
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Left operand of this operator
        /// </summary>
        public ValueExpression Left { get; }

        /// <summary>
        /// Right operand of this operator
        /// </summary>
        public ValueExpression Right { get; }

        public override IEnumerable<ValueExpression> Children
        {
            get
            {
                yield return Left;
                yield return Right;
            }
        }

        protected BinaryOperatorExpression(
            int line, 
            int column, 
            string name, 
            ValueExpression left, 
            ValueExpression right) : base(line, column)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
        }

        public override string ToString()
        {
            return "(" + Left + " " + Name + " " + Right + ")";
        }

        public override Expression ToExpressionTree(
            ParameterExpression entityParameter,
            IRuntime runtime)
        {
            var left = Left.ToExpressionTree(entityParameter, runtime);
            var right = Right.ToExpressionTree(entityParameter, runtime);
            return FunctionUtils.RuntimeCall(Name, Line, Column, runtime, entityParameter, left, right);
        }
    }
}
