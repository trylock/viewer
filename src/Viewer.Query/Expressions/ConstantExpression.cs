using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.Expressions
{
    /// <inheritdoc />
    /// <summary>
    /// Constant expression in a query (for example: 3.14159, 42, "string")
    /// </summary>
    internal class ConstantExpression : ValueExpression
    {
        /// <summary>
        /// Value of the constant
        /// </summary>
        public BaseValue Value { get; }

        public ConstantExpression(int line, int column, BaseValue value) : base(line, column)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override Expression ToExpressionTree(
            ParameterExpression entityParameter,
            IRuntime runtime,
            IQueryErrorListener errorListener)
        {
            return Expression.Constant(Value);
        }
    }
}
