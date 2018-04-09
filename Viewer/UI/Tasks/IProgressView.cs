using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.UI.Tasks
{
    public delegate void WorkDelegate(IProgressView view);

    public interface IProgressView
    {
        /// <summary>
        /// Event called when user requests to cancel the operation.
        /// </summary>
        event EventHandler CancelProgress;

        /// <summary>
        /// Get cancellation token of this view
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Open a new progress view.
        /// </summary>
        /// <param name="name">Name of the task</param>
        /// <param name="maximum">Total number of units of work to do.</param>
        /// <param name="doWork">Function which starts an intensive computation</param>
        void Show(string name, int maximum, WorkDelegate doWork);

        /// <summary>
        /// Close this progress view
        /// </summary>
        /// <remarks>Can be called from multiple threads</remarks>
        /// <param name="canceled">true iff the operation was canceled</param>
        void CloseView(bool canceled);

        /// <summary>
        /// Begin a new operation.
        /// It is assumed that the previous operation has been done 
        /// (i.e. a single unit of work has been done).
        /// </summary>
        /// <remarks>Can be called from multiple threads</remarks>
        /// <param name="name">Name of the task</param>
        void StartWork(string name);

        /// <summary>
        /// Can be called from multiple threads
        /// </summary>
        /// <remarks>Can be called from multiple threads</remarks>
        void FinishWork();

        /// <summary>
        /// Create a progress object which will update the view
        /// </summary>
        /// <typeparam name="T">Type of the progress value</typeparam>
        /// <param name="finishedPredicate">Function which returns true iff we have finished a unit of work</param>
        /// <param name="taskNameGetter">Function will set current task name</param>
        /// <returns>Progress object</returns>
        IProgress<T> CreateProgress<T>(Func<T, bool> finishedPredicate, Func<T, string> taskNameGetter);
    }

    public interface IProgressViewFactory
    {
        IProgressView Create();
    }
}
