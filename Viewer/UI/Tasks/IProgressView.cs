using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.UI.Tasks
{
    public delegate void WorkDelegate<T>(IProgressView<T> view);

    public interface IProgressView<T>
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
        /// Get view's progress controller
        /// </summary>
        IProgress<T> Progress { get; }

        /// <summary>
        /// Open a new progress view.
        /// </summary>
        /// <param name="doWork">Function which starts an intensive computation</param>
        void Show(WorkDelegate<T> doWork);

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
        /// Set view title
        /// </summary>
        /// <param name="title">New title</param>
        /// <returns>this view</returns>
        IProgressView<T> WithTitle(string title);
        
        /// <summary>
        /// Set number of units of work
        /// </summary>
        /// <param name="workUnitCount"></param>
        /// <returns>this view</returns>
        IProgressView<T> WithWork(int workUnitCount);
    }

    public interface IProgressViewFactory
    {
        IProgressView<T> Create<T>(Func<T, bool> finishPredicate, Func<T, string> taskNameGetter);
    }
}
