using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Collections;
using Viewer.IO;
using Viewer.Query;

namespace Viewer.UI.Images
{
    public class QueryEvaluator
    {
        // dependencies
        private readonly ILazyThumbnailFactory _thumbnailFactory;
        private readonly IErrorListener _queryErrorListener;

        // state
        private readonly ConcurrentSortedSet<IFileView> _waitingQueue;
        
        /// <summary>
        /// Cancellation of the query evaluation
        /// </summary>
        public CancellationTokenSource Cancellation => Query.Cancellation;

        /// <summary>
        /// Comparer of the result set
        /// </summary>
        public IComparer<IFileView> Comparer => _waitingQueue.Comparer;

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
            Query = query;
            _waitingQueue = new ConcurrentSortedSet<IFileView>(new FileViewComparer(Query.Comparer));
        }

        /// <summary>
        /// Evaluate the query on a differet thread.
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
        /// Load the query synchronously 
        /// </summary>
        public void Run()
        {
            var directories = new HashSet<string>();

            try
            {
                foreach (var entity in Query)
                {
                    Cancellation.Token.ThrowIfCancellationRequested();

                    // add the file to the result
                    var item = new FileView(entity, _thumbnailFactory.Create(entity));
                    _waitingQueue.Add(item);

                    // add all subdirectories to the result
                    var dirPath = PathUtils.GetDirectoryPath(entity.Path);
                    if (directories.Contains(dirPath))
                    {
                        continue;
                    }

                    directories.Add(dirPath);
                    foreach (var dir in Directory.EnumerateDirectories(dirPath))
                    {
                        Cancellation.Token.ThrowIfCancellationRequested();
                        _waitingQueue.Add(new DirectoryView(dir, new DirectoryThumbnail(dir)));
                    }
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
        public IReadOnlyList<IFileView> Consume()
        {
            return _waitingQueue.Consume();
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
