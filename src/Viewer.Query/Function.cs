using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query
{
    /// <summary>
    /// Represents a function callable from viewer query expression.
    /// </summary>
    public interface IFunction
    {
        /// <summary>
        /// Name of the function
        /// </summary>
        string Name { get; }

        /// <summary>
        /// List of argument types of the function
        /// </summary>
        IReadOnlyList<TypeId> Arguments { get; }

        /// <summary>
        /// Call the function with given arguments
        /// </summary>
        /// <param name="arguments">
        /// Arguments of the function call. There will always be a correct number of arguments with
        /// correct types but some values can be null (e.g. due to a bad conversion).
        /// </param>
        /// <returns>Return value of the function</returns>
        BaseValue Call(IExecutionContext arguments);
    }

    /// <inheritdoc />
    /// <summary>
    /// Derive this class to easily create function alias (i.e., the same function with different
    /// name). The derived class just has to provide a new name for the function. Everything else
    /// will be implemented automatically.
    /// </summary>
    /// <typeparam name="T">Type of the function whose alias this is.</typeparam>
    public abstract class FunctionAlias<T> : IFunction where T : IFunction, new()
    {
        private readonly T _original = new T();

        /// <inheritdoc />
        /// <summary>
        /// Name of the alias function
        /// </summary>
        public abstract string Name { get; }

        public IReadOnlyList<TypeId> Arguments => _original.Arguments;

        public BaseValue Call(IExecutionContext arguments) => _original.Call(arguments);
    }
}
