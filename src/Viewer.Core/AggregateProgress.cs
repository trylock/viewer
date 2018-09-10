using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Core
{
    /// <inheritdoc />
    /// <summary>
    /// Aggregate progress aggregates multiple instances of <see cref="T:System.IProgress`1" />.
    /// Use factories in <see cref="AggregateProgress"/> to create this object.
    /// </summary>
    /// <typeparam name="T">Type of the progress report parameter</typeparam>
    public class AggregateProgress<T> : IProgress<T>
    {
        private readonly List<IProgress<T>> _progress;

        /// <summary>
        /// Create a new empty progress. <see cref="Report"/> function is basically a no-op.
        /// </summary>
        internal AggregateProgress()
        {
            _progress = new List<IProgress<T>>();
        }

        /// <summary>
        /// Create a new aggregate progress which aggregates <paramref name="progress"/> objects.
        /// </summary>
        /// <param name="progress">Progress objects to aggregate.</param>
        internal AggregateProgress(IEnumerable<IProgress<T>> progress)
        {
            _progress = progress?.ToList() ?? throw new ArgumentNullException(nameof(progress));
        }

        /// <inheritdoc />
        /// <summary>
        /// Report a progress to all progress objects
        /// </summary>
        /// <param name="value">Value to report</param>
        public void Report(T value)
        {
            foreach (var progress in _progress)
            {
                progress.Report(value);
            }
        }
    }

    /// <summary>
    /// <see cref="AggregateProgress{T}"/> factories.
    /// </summary>
    public static class AggregateProgress
    {
        /// <summary>
        /// Create an empty aggregate progress (its Report method is basically a no-op)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AggregateProgress<T> CreateEmpty<T>()
        {
            return new AggregateProgress<T>();
        }

        /// <summary>
        /// Create an aggregate progress from a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static AggregateProgress<T> Create<T>(IEnumerable<IProgress<T>> progress)
        {
            return new AggregateProgress<T>(progress);
        }

        /// <summary>
        /// Create a new aggregate progress. This factory method can infer template types. 
        /// </summary>
        /// <typeparam name="T">Type of the reported value</typeparam>
        /// <param name="progress">Progress objects</param>
        /// <returns>Aggregate progress object</returns>
        public static AggregateProgress<T> Create<T>(params IProgress<T>[] progress)
        {
            return new AggregateProgress<T>(progress);
        }
    }
}
