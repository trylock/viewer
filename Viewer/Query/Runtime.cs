using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query
{
    /// <inheritdoc />
    /// <summary>
    /// Exception thrown when there is an error in a runtime function
    /// </summary>
    public class QueryRuntimeException : Exception
    {
        public QueryRuntimeException()
        {
        }

        public QueryRuntimeException(string message) : base(message)
        {
        }

        public QueryRuntimeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

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
        /// Call <paramref name="function"/> with <paramref name="arguments"/>.
        /// Arguments are automatically converted if necessary.
        /// </summary>
        /// <param name="function">Function to call</param>
        /// <param name="arguments">Actual arguments of the function call</param>
        /// <returns>Return value of the function call</returns>
        /// <exception cref="QueryRuntimeException">An error occurs in the function call</exception>
        BaseValue Call(IFunction function, params BaseValue[] arguments);

        /// <summary>
        /// Find function and call it. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        /// <exception cref="QueryRuntimeException">
        ///     An error occurs in the function call or
        ///     there is no function with the name and arguments.
        /// </exception>
        BaseValue FindAndCall(string name, params BaseValue[] arguments);
    }

    public class Runtime : IRuntime
    {
        private readonly Dictionary<string, List<IFunction>> _functions = new Dictionary<string, List<IFunction>>();
        private readonly IValueConverter _converter;

        [ImportingConstructor]
        public Runtime(IValueConverter converter, [ImportMany] IFunction[] functions)
        {
            _converter = converter;

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

        public BaseValue Call(IFunction function, params BaseValue[] arguments)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));
            if (function.Arguments.Count != arguments.Length)
                throw new ArgumentOutOfRangeException(nameof(arguments));

            // convert arguments
            var actualArguments = new BaseValue[arguments.Length];
            for (var i = 0; i < arguments.Length; ++i)
            {
                actualArguments[i] = ConvertTo(arguments[i], function.Arguments[i]);
            }

            // call the function
            return function.Call(new ArgumentList(actualArguments));
        }

        public BaseValue FindAndCall(string name, params BaseValue[] arguments)
        {
            var argumentList = arguments.Select(item => item.Type).ToArray();
            var function = FindFunction(name, argumentList);
            if (function != null)
            {
                return Call(function, arguments);
            }

            // unknown function
            const string argumentSeparator = ", ";
            var sb = new StringBuilder();
            foreach (var arg in arguments)
            {
                sb.Append(arg.Type);
                sb.Append(argumentSeparator);
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - argumentSeparator.Length, argumentSeparator.Length);
            }
            throw new QueryRuntimeException($"Unknown function {name}({sb})");

        }

        private static string NormalizeFunctionName(string name) => name.ToLowerInvariant();
    }
}
