using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Collections;
using Viewer.Data;
using Viewer.IO;
using Viewer.Query;

namespace Viewer.UI.Images
{
    public class QueryEvaluator : IDisposable
    {
        // dependencies
        private readonly ILazyThumbnailFactory _thumbnailFactory;
        private readonly IErrorListener _queryErrorListener;

        // state
        private readonly ConcurrentSortedSet<EntityView> _waitingQueue;

        /// <summary>
        /// Cancellation of the query evaluation
        /// </summary>
        public CancellationTokenSource Cancellation { get; }

        /// <summary>
        /// Comparer of the result set
        /// </summary>
        public IComparer<EntityView> Comparer => _waitingQueue.Comparer;

        /// <summary>
        /// Current query
        /// </summary>
        public IQuery Query { get; }

        /// <summary>
        /// Current load task
        /// </summary>
        public Task LoadTask { get; private set; }

        public QueryEvaluator(ILazyThumbnailFactory thumbnailFactory, IErrorListener queryErrorListener, IQuery query)
        {
            _thumbnailFactory = thumbnailFactory;
            _queryErrorListener = queryErrorListener;
            Cancellation = new CancellationTokenSource();
            Query = query;
            _waitingQueue = new ConcurrentSortedSet<EntityView>(new EntityViewComparer(Query.Comparer));
        }

        /// <summary>
        /// Evaluate the query on a differet thread.
        /// Found entities will be added to a waiting queue.
        /// Use <see cref="Consume"/> to get all entities loaded so far.
        /// </summary>
        /// <returns>Task finished when the evaluation ends</returns>
        public Task RunAsync()
        {
            LoadTask = Task.Factory.StartNew(
                Run,
                Cancellation.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            return LoadTask;
        }

        /// <summary>
        /// Load the query synchronously. See <see cref="RunAsync"/>
        /// </summary>
        public void Run()
        {
            try
            {
                foreach (var entity in Query.Evaluate(Cancellation.Token))
                {
                    Cancellation.Token.ThrowIfCancellationRequested();

                    _waitingQueue.Add(new EntityView(entity, _thumbnailFactory.Create(entity)));
                }
            }
            catch (QueryRuntimeException e)
            {
                _queryErrorListener.ReportError(0, 0, e.Message);
            }
        }

        /// <summary>
        /// Remove all loaded views so far and return them.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<EntityView> Consume()
        {
            return _waitingQueue.Consume();
        }

        public void Dispose()
        {
            Cancellation.Dispose();
        }
    }

    public interface IQueryEvaluatorFactory
    {
        /// <summary>
        /// Create query evaluator from query
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        QueryEvaluator Create(IQuery query);
    }

    [Export(typeof(IQueryEvaluatorFactory))]
    public class QueryEvaluatorFactory : IQueryEvaluatorFactory
    {
        private readonly ILazyThumbnailFactory _thumbnailFactory;
        private readonly IErrorListener _errorListener;

        [ImportingConstructor]
        public QueryEvaluatorFactory(ILazyThumbnailFactory thumbnailFactory, IErrorListener errorListener)
        {
            _thumbnailFactory = thumbnailFactory;
            _errorListener = errorListener;
        }

        public QueryEvaluator Create(IQuery query)
        {
            return new QueryEvaluator(_thumbnailFactory, _errorListener, query);
        }
    }
}
