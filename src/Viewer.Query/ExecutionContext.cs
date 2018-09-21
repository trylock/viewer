using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query
{
    /// <inheritdoc />
    /// <summary>
    /// Function execution context contains all information a function needs for execution.
    /// Primarily, it is a list of arguments. For error reporting, it contains current entity
    /// and position in the query.
    /// </summary>
    public interface IExecutionContext : IReadOnlyList<BaseValue>
    {
        /// <summary>
        /// Current entity 
        /// </summary>
        IEntity Entity { get; }

        /// <summary>
        /// Line at which this function has been executed in the query.
        /// </summary>
        int Line { get; }

        /// <summary>
        /// Column in the <see cref="Line"/> at which this function has been executed in the query.
        /// </summary>
        int Column { get; }

        /// <summary>
        /// Convert ith argumnet to given type
        /// </summary>
        /// <typeparam name="T">Type of the argument</typeparam>
        /// <param name="index">index of an argument</param>
        /// <returns>Converted argument</returns>
        T Get<T>(int index) where T : BaseValue;

        /// <summary>
        /// Report an error. Function which encounters an error state returns value returned by
        /// this function. 
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>Error value returned by this function</returns>
        BaseValue Error(string message);
    }

    public class ExecutionContext : IExecutionContext
    {
        private readonly IReadOnlyList<BaseValue> _values;
        private readonly IRuntime _runtime;

        public int Count => _values.Count;
        public IEntity Entity { get; }
        public int Line { get; }
        public int Column { get; }

        public ExecutionContext(
            IReadOnlyList<BaseValue> values, 
            IRuntime runtime, 
            IEntity entity, 
            int line, 
            int column)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            Entity = entity;
            Line = line;
            Column = column;
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

        public BaseValue Error(string message)
        {
            _runtime.ReportError(Line, Column, message);
            return new IntValue(null);
        }
    }
}
