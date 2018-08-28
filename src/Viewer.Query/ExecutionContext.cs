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
    }

    public class ExecutionContext : IExecutionContext
    {
        private readonly IReadOnlyList<BaseValue> _values;

        public int Count => _values.Count;
        public IEntity Entity { get; }
        public int Line { get; }
        public int Column { get; }

        public ExecutionContext(IReadOnlyList<BaseValue> values, IEntity entity, int line, int column)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
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
    }
}
