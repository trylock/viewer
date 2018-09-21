using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.Expressions
{
    /// <summary>
    /// Expression whose result is a value (see <see cref="BaseValue"/>)
    /// </summary>
    internal abstract class ValueExpression
    {
        /// <summary>
        /// Line in the query on which this value expression starts
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Column in <see cref="Line"/> on which this expression starts
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Get all subexpressions of this expression
        /// </summary>
        public virtual IEnumerable<ValueExpression> Children => Enumerable.Empty<ValueExpression>();

        protected ValueExpression(int line, int column)
        {
            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));
            if (column < 0)
                throw new ArgumentOutOfRangeException(nameof(column));

            Line = line;
            Column = column;
        }
        
        /// <summary>
        /// Compile this expression to a function which takes an entity and returns the computed
        /// value.
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="errorListener"></param>
        /// <returns></returns>
        public virtual Func<IEntity, BaseValue> CompileFunction(
            IRuntime runtime, 
            IQueryErrorListener errorListener)
        {
            var entityParameter = Expression.Parameter(typeof(IEntity), "entity");
            var expression = ToExpressionTree(entityParameter, runtime, errorListener);
            var functionExpression = Expression.Lambda<Func<IEntity, BaseValue>>(expression, entityParameter);
            var function = functionExpression.Compile();
            return function;
        }

        /// <summary>
        /// Compile this expression to a predicate function which takes an entity and returns true
        /// iff the expression evaluates to a value which is not null.
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="errorListener"></param>
        /// <returns></returns>
        public virtual Func<IEntity, bool> CompilePredicate(IRuntime runtime, IQueryErrorListener errorListener)
        {
            var function = CompileFunction(runtime, errorListener);
            return entity => !function(entity).IsNull;
        }

        public abstract override string ToString();

        public abstract Expression ToExpressionTree(
            ParameterExpression entityParameter, 
            IRuntime runtime,
            IQueryErrorListener errorListener);
    }
}
