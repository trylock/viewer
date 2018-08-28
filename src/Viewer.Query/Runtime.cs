using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query
{
    public interface IRuntime
    {
        /// <summary>
        /// Convert <paramref name="value"/> to <paramref name="resultType"/>.
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="resultType">Type of the result</param>
        /// <returns>Converted value</returns>
        BaseValue ConvertTo(BaseValue value, TypeId resultType);

        /// <summary>
        /// Find a function with <paramref name="arguments"/>.
        /// </summary>
        /// <param name="name">Function name</param>
        /// <param name="arguments">Function arguments</param>
        /// <returns>Fuction with given name and arguments or null</returns>
        IFunction FindFunction(string name, IReadOnlyList<TypeId> arguments);

        /// <summary>
        /// Call <paramref name="function"/> with <paramref name="context"/>.
        /// Arguments are automatically converted if necessary.
        /// </summary>
        /// <param name="function">Function to call</param>
        /// <param name="context">Execution context</param>
        /// <returns>Return value of the function call</returns>
        BaseValue Call(IFunction function, IExecutionContext context);

        /// <summary>
        /// Find function and call it. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        BaseValue FindAndCall(string name, IExecutionContext context);
    }

    [Export(typeof(IRuntime))]
    public class Runtime : IRuntime
    {
        private readonly Dictionary<string, List<IFunction>> _functions = new Dictionary<string, List<IFunction>>();
        private readonly IValueConverter _converter;
        private readonly IQueryErrorListener _queryErrorListener;

        [ImportingConstructor]
        public Runtime(
            IValueConverter converter, 
            IQueryErrorListener queryErrorListener,
            [ImportMany] IFunction[] functions)
        {
            _converter = converter;
            _queryErrorListener = queryErrorListener;

            foreach (var function in functions)
            {
                var normalizedFunctionName = NormalizeFunctionName(function.Name);
                if (!_functions.TryGetValue(normalizedFunctionName, out var functionList))
                {
                    functionList = new List<IFunction>{ function };
                    _functions.Add(normalizedFunctionName, functionList);
                }
                else
                {
                    functionList.Add(function);
                }
            }
        }

        public BaseValue ConvertTo(BaseValue value, TypeId resultType)
        {
            return _converter.ConvertTo(value, resultType);
        }
        
        public IFunction FindFunction(string name, IReadOnlyList<TypeId> arguments)
        {
            var normalizedName = NormalizeFunctionName(name);
            if (!_functions.TryGetValue(normalizedName, out var functions))
            {
                return null;
            }

            // find function with minimal conversion cost
            var minCost = int.MaxValue;
            IFunction minFunction = null;
            foreach (var function in functions)
            {
                if (function.Arguments.Count != arguments.Count)
                {
                    continue;
                }

                var cost = _converter.ComputeConversionCost(arguments, function.Arguments);
                if (cost < minCost)
                {
                    minCost = cost;
                    minFunction = function;
                }
            }

            return minFunction;
        }

        public BaseValue Call(IFunction function, IExecutionContext context)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));
            if (function.Arguments.Count != context.Count)
                throw new ArgumentOutOfRangeException(nameof(context));

            // convert arguments
            var actualArguments = new BaseValue[context.Count];
            for (var i = 0; i < context.Count; ++i)
            {
                actualArguments[i] = ConvertTo(context[i], function.Arguments[i]);
            }

            // call the function
            return function.Call(new ExecutionContext(
                actualArguments, 
                context.Entity, 
                context.Line, 
                context.Column));
        }

        public BaseValue FindAndCall(string name, IExecutionContext context)
        {
            var argumentList = context.Select(item => item.Type).ToArray();
            var function = FindFunction(name, argumentList);
            if (function != null)
            {
                return Call(function, context);
            }

            // unknown function, report an error
            const string argumentSeparator = ", ";
            var sb = new StringBuilder();
            foreach (var arg in context)
            {
                sb.Append(arg.Type);
                sb.Append(argumentSeparator);
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - argumentSeparator.Length, argumentSeparator.Length);
            }
            _queryErrorListener.OnRuntimeError(
                context.Line, 
                context.Column, 
                $"Unknown function {name}({sb})");
            return new IntValue(null);
        }

        private static string NormalizeFunctionName(string name) => name.ToLowerInvariant();
    }
}
