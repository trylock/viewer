using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query.Expressions
{
    internal class FunctionCallExpression : ValueExpression
    {
        /// <summary>
        /// Name of the function
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Function parameters
        /// </summary>
        public IReadOnlyList<ValueExpression> Parameters { get; }

        public override IEnumerable<ValueExpression> Children => Parameters;

        public FunctionCallExpression(int line, int column, string name, IEnumerable<ValueExpression> parameters) 
            : base(line, column)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameters = new List<ValueExpression>(parameters);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            sb.Append("(");
            for (var i = 0; i < Parameters.Count; ++i)
            {
                var value = Parameters[i];
                sb.Append(value);
                if (i + 1 < Parameters.Count)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(")");
            return sb.ToString();
        }

        public override Expression ToExpressionTree(ParameterExpression entityParameter, IRuntime runtime)
        {
            var arguments = new Expression[Parameters.Count];
            for (var i = 0; i < arguments.Length; ++i)
            {
                arguments[i] = Parameters[i].ToExpressionTree(entityParameter, runtime);
            }

            return FunctionUtils.RuntimeCall(Name, Line, Column, runtime, entityParameter, arguments);
        }
    }
}
