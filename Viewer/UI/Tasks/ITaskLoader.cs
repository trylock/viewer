using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.UI.Tasks
{
    /// <summary>
    /// Argument of the IProgress.Report method has to implement this interface
    /// in order for the IProgress to work with background loader.
    /// </summary>
    public interface ILoadingProgress
    {
        /// <summary>
        /// Name of the next task or null if this was the last task
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// Default implementation of loading progress which carries only the task name.
    /// </summary>
    public class LoadingProgress : ILoadingProgress
    {
        public string Name { get; }

        public LoadingProgress(string name)
        {
            Name = name;
        }
    }

    public interface ITaskLoader
    {
        /// <summary>
        /// Create a task loader which shows loading progress to the user.
        /// </summary>
        /// <param name="name">Name of the task</param>
        /// <param name="totalTaskCount">Total number of tasks to finish (one call to the Report method finishes one task)</param>
        /// <param name="cancellation">Cancellation token source</param>
        /// <returns>
        ///     Progress whose Report method updates the loading progress.
        ///     The report method of the returned progress is thread safe.
        /// </returns>
        IProgress<ILoadingProgress> CreateLoader(string name, int totalTaskCount, CancellationTokenSource cancellation);
    }
}
