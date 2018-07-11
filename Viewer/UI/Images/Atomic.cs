using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.UI.Images
{
    /// <summary>
    /// Provide atomic read/write operations for value types.
    /// Note: there is a non-trivial overhead as it boxes the wrapped value.
    /// This type is thread safe.
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    public class Atomic<T> 
    {
        private volatile object _value;

        /// <summary>
        /// Read/write of the property has acquire/release semantics.
        /// </summary>
        public T Value
        {
            get => (T) _value;
            set => _value = value;
        }

        /// <summary>
        /// Initializes the value with default(T)
        /// </summary>
        public Atomic()
        {
            _value = default(T);
        }

        /// <summary>
        /// Initializes the vlaue with <paramref name="value"/>
        /// </summary>
        /// <param name="value">Initial value of the variable</param>
        public Atomic(T value)
        {
            _value = value;
        }

        public static implicit operator T(Atomic<T> atomic)
        {
            return atomic.Value;
        }
    }
}
