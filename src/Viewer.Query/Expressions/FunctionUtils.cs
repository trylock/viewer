using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.Expressions
{
    internal static class FunctionUtils
    {
        public static Expression RuntimeCall(
            string functionName, 
            int line, 
            int column, 
            IRuntime runtime,
            ParameterExpression entityParameter,
            Expression argumentsArray)
        {
            if (argumentsArray.Type != typeof(BaseValue[]))
                throw new ArgumentOutOfRangeException(nameof(argumentsArray));

            var constructor = typeof(ExecutionContext).GetConstructors()[0];
            var context = Expression.New(
                constructor,
                new Expression[] {
                    argumentsArray,
                    Expression.Constant(runtime),
                    entityParameter,
                    Expression.Constant(line),
                    Expression.Constant(column),
                });

            var runtimeCall = runtime.GetType().GetMethod("FindAndCall");

            return Expression.Call(
                Expression.Constant(runtime),
                runtimeCall,
                Expression.Constant(functionName),
                context);
        }
        
        public static Expression RuntimeCall(
            string functionName, 
            int line, 
            int column, 
            IRuntime runtime,
            ParameterExpression entityParameter,
            params Expression[] arguments)
        {
            var argumentsArray = Expression.NewArrayInit(typeof(BaseValue), arguments);
            return RuntimeCall(functionName, line, column, runtime, entityParameter, argumentsArray);
        }
    }
}
