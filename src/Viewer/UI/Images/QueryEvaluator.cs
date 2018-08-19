using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Core.Collections;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.IO;
using Viewer.Query;

namespace Viewer.UI.Images
{
    /// <inheritdoc />
    /// <summary>
    /// Query evaluator evaluates query and tries to keep it updated. It will watch changes in
    /// entity changes in the file system and update the result accordingly.
    /// </summary>
    /// <remarks>
    /// This object is responsible for collection returned from the <see cref="Update"/> method.
    /// <see cref="Dispose"/>ing this object will dispose items in this collection.
    /// </remarks>
    public class QueryEvaluator : IDisposable
    {
        // dependencies
        private readonly IEntityManager _entities;
        private readonly IFileWatcher _fileWatcher;
        private readonly ILazyThumbnailFactory _thumbnailFactory;
        private readonly IErrorListener _queryErrorListener;

        // state
        private readonly ConcurrentSortedSet<EntityView> _addRequests;
        private readonly ConcurrentQueue<RenamedEventArgs> _moveRequests;
        private readonly ConcurrentQueue<FileSystemEventArgs> _deleteRequests;
        private SortedList<EntityView> _views;

        /// <summary>
        /// Cancellation of the query evaluation
        /// </summary>
        public CancellationTokenSource Cancellation { get; }

        /// <summary>
        /// Current query execution progress.
        /// </summary>
        public QueryProgress Progress { get; } = new QueryProgress();

        /// <summary>
        /// Current query
        /// </summary>
        public IQuery Query { get; }

        /// <summary>
        /// Current load task
        /// </summary>
        public Task LoadTask { get; private set; } = Task.CompletedTask;

        public QueryEvaluator(
            IFileWatcherFactory fileWatcherFactory, 
            ILazyThumbnailFactory thumbnailFactory, 
            IErrorListener queryErrorListener, 
            IEntityManager entities, 
            IQuery query)
        {
            _entities = entities;
            _fileWatcher = fileWatcherFactory.Create();
            _fileWatcher.Renamed += FileWatcherOnRenamed;
            _fileWatcher.Deleted += FileWatcherOnDeleted;
            _fileWatcher.Created += FileWatcherOnCreated;
            _thumbnailFactory = thumbnailFactory;
            _queryErrorListener = queryErrorListener;
            Cancellation = new CancellationTokenSource();
            Query = query;
            
            var entityViewComparer = new EntityViewComparer(Query.Comparer);
            _moveRequests = new ConcurrentQueue<RenamedEventArgs>();
            _deleteRequests = new ConcurrentQueue<FileSystemEventArgs>();
            _addRequests = new ConcurrentSortedSet<EntityView>(entityViewComparer);
            _views = new SortedList<EntityView>(entityViewComparer);
        }

        private static bool IsEntityEvent(FileSystemEventArgs e)
        {
            var newExtension = Path.GetExtension(e.FullPath)?.ToLowerInvariant();
            return newExtension == ".jpeg" || 
                   newExtension == ".jpg" || 
                   newExtension == ""; // directory
        }

        private void FileWatcherOnDeleted(object sender, FileSystemEventArgs e)
        {
            if (!IsEntityEvent(e))
                return; // skip this event
            _deleteRequests.Enqueue(e);
        }

        private void FileWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            // note: side effect of this check is that it ignores move operations done during the
            //       FileSystemAttributeStorage.Store call. 
            if (!IsEntityEvent(e))
                return; // skip this event
            _moveRequests.Enqueue(e);
        }

        private void FileWatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            if (!IsEntityEvent(e))
                return; // skip this event

            IEntity entity = null;
            try
            {
                entity = _entities.GetEntity(e.FullPath);
            } // silently ignore load exceptions
            catch (InvalidDataFormatException)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (PathTooLongException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (IOException)
            {
            }
            catch (ArgumentException) // invalid path
            {
            }

            if (entity != null && Query.Match(entity))
            {
                _addRequests.Add(new EntityView(entity, _thumbnailFactory.Create(entity, Cancellation.Token)));
            }
        }

        /// <summary>
        /// Evaluate the query on a differet thread.
        /// Found entities will be added to a waiting queue.
        /// Use <see cref="Update"/> to get all entities loaded so far.
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
            var directories = new HashSet<string>();
            
            try
            {
                foreach (var entity in Query.Evaluate(Progress, Cancellation.Token))
                {
                    Cancellation.Token.ThrowIfCancellationRequested();

                    // if the entity is in an undiscovered directory,
                    // start watching changes in this directory
                    var parentDirectory = PathUtils.GetDirectoryPath(entity.Path);
                    if (!directories.Contains(parentDirectory))
                    {
                        try
                        {
                            _fileWatcher.Watch(parentDirectory);
                        }
                        catch (ArgumentException)
                        {
                            // The path is invalid or the directory does not exist anymore.
                            // Ignore this error, just don't watch the directory.
                        }

                        directories.Add(parentDirectory);
                    }

                    // add a new entity
                    var thumbnail = _thumbnailFactory.Create(entity, Cancellation.Token);
                    _addRequests.Add(new EntityView(entity, thumbnail));
                }
            }
            catch (QueryRuntimeException e)
            {
                _queryErrorListener.ReportError(0, 0, e.Message);
            }
        }

        /// <summary>
        /// Update current collection. It takes all changes made so far and applies them to
        /// the local collection of <see cref="EntityView"/>s
        /// </summary>
        /// <returns>Modified collection</returns>
        public SortedList<EntityView> Update()
        {
            // process all add requests
            var added = _addRequests.Consume();
            if (added.Count > 0)
            {
                _views = _views.Merge(added);
            }

            // process all rename requests
            while (_moveRequests.TryDequeue(out var req))
            {
                var oldPath = PathUtils.NormalizePath(req.OldFullPath);
                for (var i = 0; i < _views.Count; ++i)
                {
                    if (!string.Equals(_views[i].FullPath, oldPath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }

                    var item = _views[i];
                    item.FullPath = req.FullPath;
                    _views.RemoveAt(i);
                    _views.Add(item);
                    break;
                }
            }

            // process all delete requests
            var deleted = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            while (_deleteRequests.TryDequeue(out var req))
            {
                var path = PathUtils.NormalizePath(req.FullPath);
                deleted.Add(path);
            }

            _views.RemoveAll(item => deleted.Contains(item.FullPath));

            return _views;
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispose this evaluator and all system resources used by this evaluator.
        /// If a load task is in progress, it will be cancelled and disposed asynchronously.
        /// </summary>
        public void Dispose()
        {
            // stop watching file changes
            _fileWatcher.Dispose();

            // cancel current loading operation
            Cancellation.Cancel();
            LoadTask.ContinueWith(parent =>
            {
                Cancellation.Dispose();
                foreach (var view in _views)
                {
                    view.Dispose();
                }
                _views = null;
            });
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
        private readonly IFileWatcherFactory _fileWatcherFactory;
        private readonly ILazyThumbnailFactory _thumbnailFactory;
        private readonly IErrorListener _errorListener;
        private readonly IEntityManager _entities;

        [ImportingConstructor]
        public QueryEvaluatorFactory(IFileWatcherFactory fileWatcherFactory, ILazyThumbnailFactory thumbnailFactory, IErrorListener errorListener, IEntityManager entities)
        {
            _fileWatcherFactory = fileWatcherFactory;
            _thumbnailFactory = thumbnailFactory;
            _errorListener = errorListener;
            _entities = entities;
        }

        public QueryEvaluator Create(IQuery query)
        {
            return new QueryEvaluator(_fileWatcherFactory, _thumbnailFactory, _errorListener, _entities, query);
        }
    }
}
