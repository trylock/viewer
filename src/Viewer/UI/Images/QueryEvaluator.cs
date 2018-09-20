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
using NLog;
using Viewer.Core;
using Viewer.Core.Collections;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.IO;
using Viewer.Query;

namespace Viewer.UI.Images
{
    /// <inheritdoc />
    /// <summary>
    /// This query progress starts watching changes in all query folders.
    /// </summary>
    internal class FolderQueryProgress : IProgress<QueryProgressReport>
    {
        private readonly IFileWatcher _watcher;

        public FolderQueryProgress(IFileWatcher watcher)
        {
            _watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
        }

        public void Report(QueryProgressReport value)
        {
            if (value.Type == ReportType.Folder)
            {
                var path = PathUtils.NormalizePath(value.FilePath);
                try
                {
                    _watcher.Watch(path);
                }
                catch (FileNotFoundException)
                {
                    // ignore this folder
                }
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Query evaluator evaluates query and tries to keep it updated. It will watch entity changes
    /// in the file system and update the result accordingly.
    /// </summary>
    /// <remarks>
    /// This object is responsible for collection returned from the <see cref="Update"/> method.
    /// <see cref="Dispose"/>ing this object will dispose items in this collection. The caller
    /// must not use the collection after calling <see cref="Dispose"/> except for the
    /// <see cref="EntityView.Data"/> property of items in the collection.
    /// </remarks>
    public class QueryEvaluator : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // dependencies
        private readonly IEntityManager _entities;
        private readonly IFileWatcher _fileWatcher;
        private readonly ILazyThumbnailFactory _thumbnailFactory;

        // state
        private List<EntityView> _views;
        private readonly ConcurrentSortedSet<EntityView> _added;
        private readonly ConcurrentQueue<Request> _requests = new ConcurrentQueue<Request>();

        /// <summary>
        /// Minimal time between a failed load operation and its retry
        /// </summary>
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximal retry operation count.
        /// </summary>
        private const int MaxRetryCount = 4;

        private enum RequestType
        {
            Modify,
            Remove,
            Move
        }

        private struct Request
        {
            public RequestType Type { get; }
            public string Path { get; }
            public string NewPath { get; }

            public Request(RequestType type, string path)
            {
                Type = type;
                Path = path;
                NewPath = null;
            }

            public Request(string oldPath, string newPath)
            {
                Type = RequestType.Move;
                Path = oldPath;
                NewPath = newPath;
            }
        }

        /// <summary>
        /// Comparer used to sort items in the query result set.
        /// </summary>
        public IComparer<EntityView> Comparer { get; }

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
            IEntityManager entities, 
            IQuery query)
        {
            _entities = entities;
            _fileWatcher = fileWatcherFactory.Create();
            _thumbnailFactory = thumbnailFactory;
            Query = query;

            // initialize internal structures
            Cancellation = new CancellationTokenSource();
            Comparer = new EntityViewComparer(Query.Comparer);
            _views = new List<EntityView>();
            _added = new ConcurrentSortedSet<EntityView>(Comparer);

            // register event handlers
            _fileWatcher.Renamed += FileWatcherOnRenamed;
            _fileWatcher.Deleted += FileWatcherOnDeleted;
            _fileWatcher.Created += FileWatcherOnCreated;
            _entities.Deleted += EntitiesOnDeleted;
            _entities.Changed += EntitiesOnChanged;
            _entities.Moved += EntitiesOnMoved;
        }
        
        #region Event Handlers
        
        private void EntitiesOnDeleted(object sender, Data.EntityEventArgs e)
        {
            _requests.Enqueue(new Request(RequestType.Remove, e.Value.Path));
        }

        private void EntitiesOnMoved(object sender, EntityMovedEventArgs e)
        {
            // We have already changed entity's path, just have to make sure the list is still sorted.
            _requests.Enqueue(new Request(RequestType.Modify, e.Value.Path));
        }

        private void EntitiesOnChanged(object sender, Data.EntityEventArgs e)
        {
            _requests.Enqueue(new Request(RequestType.Modify, e.Value.Path));
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
            var path = PathUtils.NormalizePath(e.FullPath);
            _requests.Enqueue(new Request(RequestType.Remove, path));
        }

        private void FileWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            // note: side effect of this check is that it ignores move operations done during the
            //       FileSystemAttributeStorage.Store call. 
            if (!IsEntityEvent(e))
                return; // skip this event
            var oldPath = PathUtils.NormalizePath(e.OldFullPath);
            var newPath = PathUtils.NormalizePath(e.FullPath);
            _requests.Enqueue(new Request(oldPath, newPath));
        }

        private async void FileWatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            if (!IsEntityEvent(e))
                return; // skip this event

            try
            {
                await Retry
                    .Async(() => CheckAndAdd(e.FullPath))
                    .WithAttempts(MaxRetryCount)
                    .WithDelay(RetryDelay)
                    .WhenExactly<IOException>();
            }
            catch (IOException ex)
            {
                Logger.Debug(ex, "All attempts have failed.");
            }
        }

        private void CheckAndAdd(string path)
        {
            IEntity entity = null;
            try
            {
                entity = _entities.GetEntity(path);
            } // silently ignore load exceptions
            catch (InvalidDataFormatException ex)
            {
                Logger.Debug(ex);
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
            catch (ArgumentException ex) // invalid path
            {
                Logger.Debug(ex);
            }

            if (entity != null && Query.Match(entity))
            {
                _added.Add(new EntityView(entity, _thumbnailFactory.Create(entity, Cancellation.Token)));
            }
        }

        #endregion

        /// <summary>
        /// Get all searched folders so far. This can be called even if the query evaluation is
        /// in progress but in that case, only a subset of searched folders will be returned.
        /// </summary>
        /// <returns>All searched folders so far</returns>
        public IEnumerable<string> GetSearchedDirectories()
        {
            return _fileWatcher.GetWatchedDirectories();
        }

        /// <summary>
        /// Evaluate the query on a differet thread. Found entities will be added to a waiting queue.
        /// Use <see cref="Update"/> to get all entities loaded so far.
        /// </summary>
        /// <returns>Task finished when the whole evaluation ends</returns>
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
        /// Load the query synchronously. <see cref="Cancellation"/> is used for cancellation token,
        /// <see cref="Progress"/> is used for progress. See <see cref="RunAsync"/>.
        /// </summary>
        public void Run()
        {
            var progress = AggregateProgress.Create(Progress, new FolderQueryProgress(_fileWatcher));

            foreach (var entity in Query.Execute(progress, Cancellation.Token))
            {
                Cancellation.Token.ThrowIfCancellationRequested();
                
                _added.Add(new EntityView(entity, _thumbnailFactory.Create(entity, Cancellation.Token)));
            }
        }
        
        /// <summary>
        /// Update current collection. It takes all changes made so far and applies them to
        /// the local collection of <see cref="EntityView"/>s. 
        /// </summary>
        /// <returns>Modified collection</returns>
        public List<EntityView> Update()
        {
            // index changes
            var index = new Dictionary<string, Request>(StringComparer.CurrentCultureIgnoreCase);
            while (_requests.TryDequeue(out var req))
            {
                index[req.Path] = req;
            }

            var modified = new List<EntityView>();
            var head = 0;
            for (var i = 0; i < _views.Count; ++i)
            {
                Trace.Assert(head <= i);

                var item = _views[i];
                if (!index.TryGetValue(item.FullPath, out var req))
                {
                    _views[head] = item;
                    ++head;
                }
                else if (req.Type == RequestType.Remove)
                {
                    item.Dispose();
                }
                else if (req.Type == RequestType.Modify)
                {
                    modified.Add(item);
                }
                else // if (type == RequestType.Move)
                {
                    item.Data.ChangePath(req.NewPath);
                    modified.Add(item);
                }
            }
            _views.RemoveRange(head, _views.Count - head);

            // re-add modified entities
            var added = _added.Consume();
            if (modified.Count > 0)
            {
                modified.Sort(Comparer);
                added = added.Merge(modified, Comparer);
            }

            // add new entities
            if (added.Count > 0)
            {
                _views = _views.Merge(added, Comparer);
            }

            return _views;
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Dispose this evaluator and all system resources used by this evaluator.
        /// If a load task is in progress, it will be cancelled and disposed asynchronously.
        /// </summary>
        public void Dispose()
        {
            // unsubscribe from events
            _entities.Moved -= EntitiesOnMoved;
            _entities.Changed -= EntitiesOnChanged;
            _entities.Deleted -= EntitiesOnDeleted;

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
        private readonly IEntityManager _entities;

        [ImportingConstructor]
        public QueryEvaluatorFactory(IFileWatcherFactory fileWatcherFactory, ILazyThumbnailFactory thumbnailFactory, IEntityManager entities)
        {
            _fileWatcherFactory = fileWatcherFactory;
            _thumbnailFactory = thumbnailFactory;
            _entities = entities;
        }

        public QueryEvaluator Create(IQuery query)
        {
            return new QueryEvaluator(_fileWatcherFactory, _thumbnailFactory, _entities, query);
        }
    }
}
