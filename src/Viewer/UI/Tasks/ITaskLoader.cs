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
        /// Message shown with the progress bar.
        /// </summary>
        string Message { get; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Default implementation of loading progress which carries only the task message.
    /// </summary>
    public class LoadingProgress : ILoadingProgress
    {
        public string Message { get; }

        public LoadingProgress(string name)
        {
            Message = name;
        }
    }

    public interface IProgressController : IProgress<ILoadingProgress>
    {
        /// <summary>
        /// Total number of tasks.
        /// </summary>
        int TotalTaskCount { get; set; }

        /// <summary>
        /// Close the progress window. It has to be called from the UI thread. Call this method
        /// when the task is finished. You can dispose <see cref="CancellationTokenSource"/> after
        /// this.
        /// </summary>
        void Close();
    }

    public interface ITaskLoader
    {
        /// <summary>
        /// Create a task loader which shows loading progress to the user.
        /// </summary>
        /// <param name="name">Name of the task</param>
        /// <param name="cancellation">
        /// Cancellation token source. The loader takes ownership of the cancellation token source.
        /// Dispose it by calling <see cref="IProgressController.Close"/>
        /// </param>
        /// <returns>
        /// Progress whose Report method updates the loading progress. The report method of the
        /// returned progress is thread safe.
        /// </returns>
        IProgressController CreateLoader(string name, CancellationTokenSource cancellation);
    }
}
