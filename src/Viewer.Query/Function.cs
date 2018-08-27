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
    public interface IArgumentList : IReadOnlyList<BaseValue>
    {
        /// <summary>
        /// Convert ith argumnet to given type
        /// </summary>
        /// <typeparam name="T">Type of the argument</typeparam>
        /// <param name="index">index of an argument</param>
        /// <returns>Converted argument</returns>
        T Get<T>(int index) where T : BaseValue;
    }

    public class ArgumentList : IArgumentList
    {
        private readonly IReadOnlyList<BaseValue> _values;

        public int Count => _values.Count;

        public ArgumentList(IReadOnlyList<BaseValue> values)
        {
            _values = values;
        }

        public IEnumerator<BaseValue> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public BaseValue this[int index] => _values[index];

        public T Get<T>(int index) where T : BaseValue
        {
            return _values[index] as T;
        }
    }

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
        BaseValue Call(IArgumentList arguments);
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

        public BaseValue Call(IArgumentList arguments) => _original.Call(arguments);
    }
}
