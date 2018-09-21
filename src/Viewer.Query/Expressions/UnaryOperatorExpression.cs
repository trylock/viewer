using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.Expressions
{
    internal abstract class UnaryOperatorExpression : ValueExpression
    {
        /// <summary>
        /// Name of the operator
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Parameter of the operator
        /// </summary>
        public ValueExpression Parameter { get; }

        public override IEnumerable<ValueExpression> Children
        {
            get
            {
                yield return Parameter;
            }
        }

        protected UnaryOperatorExpression(int line, int column, string name, ValueExpression parameter) 
            : base(line, column)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public override string ToString()
        {
            return Name + " " + Parameter;
        }

        public override Expression ToExpressionTree(
            ParameterExpression entityParameter, 
            IRuntime runtime)
        {
            var value = Parameter.ToExpressionTree(entityParameter, runtime);
            return FunctionUtils.RuntimeCall(Name, Line, Column, runtime, entityParameter, new[]
            {
                value
            });
        }
    }
}
