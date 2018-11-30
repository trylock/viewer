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
using Viewer.Query.Execution;
using Viewer.UI.Images.Layout;

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
        private SortedDictionary<BaseValue, Group> _viewsBack;
        private SortedDictionary<BaseValue, Group> _viewsFront;
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
        public IExecutableQuery Query { get; }

        /// <summary>
        /// Current load task
        /// </summary>
        public Task LoadTask { get; private set; } = Task.CompletedTask;

        private Thread _batchProcessingThread;
        private readonly AutoResetEvent _isNotified = new AutoResetEvent(false);
        private int _changeCount;

        public QueryEvaluator(
            IFileWatcherFactory fileWatcherFactory, 
            ILazyThumbnailFactory thumbnailFactory, 
            IEntityManager entities,
            IExecutableQuery query)
        {
            _entities = entities;
            _fileWatcher = fileWatcherFactory.Create();
            _thumbnailFactory = thumbnailFactory;
            Query = query;

            // initialize internal structures
            Cancellation = new CancellationTokenSource();
            Comparer = new EntityViewComparer(Query.Comparer);
            _viewsBack = new SortedDictionary<BaseValue, Group>();
            _viewsFront = new SortedDictionary<BaseValue, Group>();
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
            AddRequest(new Request(RequestType.Remove, e.Value.Path));
        }

        private void EntitiesOnMoved(object sender, EntityMovedEventArgs e)
        {
            // We have already changed entity's path, just have to make sure the list is still
            // sorted.
            AddRequest(new Request(RequestType.Modify, e.Value.Path));
        }

        private void EntitiesOnChanged(object sender, Data.EntityEventArgs e)
        {
            AddRequest(new Request(RequestType.Modify, e.Value.Path));
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
            AddRequest(new Request(RequestType.Remove, path));
        }

        private void FileWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            // note: side effect of this check is that it ignores move operations done during the
            //       FileSystemAttributeStorage.Store call. 
            if (!IsEntityEvent(e))
                return; // skip this event
            var oldPath = PathUtils.NormalizePath(e.OldFullPath);
            var newPath = PathUtils.NormalizePath(e.FullPath);
            AddRequest(new Request(oldPath, newPath));
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
                var thumbnail = _thumbnailFactory.Create(entity, Cancellation.Token);
                AddView(new EntityView(entity, thumbnail));
            }
        }

        #endregion

        private void AddRequest(Request req)
        {
            _requests.Enqueue(req);
            var value = Interlocked.Increment(ref _changeCount);
            if ((value % 100) == 0)
                _isNotified.Set();
        }

        private void AddView(EntityView item)
        {
            _added.Add(item);
            var value = Interlocked.Increment(ref _changeCount);
            if ((value % 100) == 0)
                _isNotified.Set();
        }

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
            _batchProcessingThread = new Thread(ProcessRequests);
            _batchProcessingThread.Start();

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
            var options = new ExecutionOptions
            {
                CancellationToken = Cancellation.Token,
                Progress = progress
            };

            foreach (var entity in Query.Execute(options))
            {
                var thumbnail = _thumbnailFactory.Create(entity, Cancellation.Token);
                AddView(new EntityView(entity, thumbnail));
            }
        }

        /// <summary>
        /// List of removed entities waiting to be disposed (by an <see cref="Update"/> call).
        /// We can't disposed them on a background thread because it is being used by the UI
        /// thread.
        /// </summary>
        /// <remarks>
        /// Access to this property is guarded by a lock on <see cref="_viewsBack"/>.
        /// </remarks>
        private readonly List<EntityView> _removed = new List<EntityView>();
        private volatile bool _isEnd = false;

        private void ProcessRequests()
        {
            while (!_isEnd)
            {
                _isNotified.WaitOne(TimeSpan.FromMilliseconds(2000));
                
                lock (_viewsBack)
                {
                    ProcessChanges();
                }
            }
            _isNotified.Dispose();
        }

        public void ProcessChanges()
        {
            // index all requests using a local hash table
            var index = new Dictionary<string, Request>(
                StringComparer.CurrentCultureIgnoreCase);
            var added = new Dictionary<BaseValue, List<EntityView>>();
            while (_requests.TryDequeue(out var req))
            {
                index[req.Path] = req;
            }

            // group added items
            var addedSnapshot = _added.Consume();
            foreach (var item in addedSnapshot)
            {
                var key = Query.GetGroup(item.Data);
                if (!added.TryGetValue(key, out var list))
                {
                    list = new List<EntityView>();
                    added.Add(key, list);
                }
                list.Add(item);
            }

            // apply all changes to the local copy
            var removedGroups = new List<BaseValue>();
            foreach (var pair in _viewsBack)
            {
                // find items added to this group
                var group = pair.Value;
                if (!added.TryGetValue(group.Key, out var addedToThisGroup))
                {
                    addedToThisGroup = new List<EntityView>();
                }

                // process all changes
                ProcessGroup(group, index, addedToThisGroup);

                // check if we should remove this group 
                if (group.Items.Count <= 0)
                {
                    removedGroups.Add(group.Key);
                }
            }

            // remove empty groups
            foreach (var key in removedGroups)
            {
                _viewsBack.Remove(key);
            }

            // add new groups
            foreach (var pair in added)
            {
                if (_viewsBack.ContainsKey(pair.Key))
                {
                    continue;
                }

                _viewsBack.Add(pair.Key, new Group(pair.Key)
                {
                    Items = pair.Value
                });
            }
        }

        private void ProcessGroup(
            Group group, 
            Dictionary<string, Request> changes, 
            List<EntityView> added)
        {
            // process all items in this group
            var modified = new List<EntityView>();
            var head = 0;
            for (var i = 0; i < group.Items.Count; ++i)
            {
                Trace.Assert(head <= i);

                var item = group.Items[i];
                if (!changes.TryGetValue(item.FullPath, out var req))
                {
                    group.Items[head] = item;
                    ++head;
                }
                else if (req.Type == RequestType.Remove)
                {
                    // we will remove and dispose all items at once after the loop
                    lock (_removed)
                    {
                        _removed.Add(item);
                    }
                }
                else if (req.Type == RequestType.Modify)
                {
                    // we will remember all modified items, sort them and merge them back
                    // later.
                    modified.Add(item);
                }
                else // if (type == RequestType.Move)
                {
                    item.Data.ChangePath(req.NewPath);
                    modified.Add(item);
                }
            }
            group.Items.RemoveRange(head, group.Items.Count - head);

            // re-add modified entities
            if (modified.Count > 0)
            {
                modified.Sort(Comparer);
                added = added.Merge(modified, Comparer);
            }

            if (added.Count > 0)
            {
                group.Items = group.Items.Merge(added, Comparer);
            }
        }

        /// <summary>
        /// Update current collection. It takes all changes made so far and applies them to
        /// the local collection of <see cref="EntityView"/>s. 
        /// </summary>
        /// <returns>Modified collection</returns>
        public SortedDictionary<BaseValue, Group> Update()
        {
            var entered = Monitor.TryEnter(_viewsBack, TimeSpan.FromMilliseconds(20));
            if (!entered)
            {
                return _viewsFront;
            }

            try
            {
                // dispose removed items
                foreach (var item in _removed)
                {
                    item.Dispose();
                }
                _removed.Clear();

                _viewsFront.Clear();
                foreach (var item in _viewsBack)
                {
                    _viewsFront.Add(item.Key, item.Value);
                }

                return _viewsFront;
            }
            finally
            {
                Monitor.Exit(_viewsBack);
            }
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

            _isEnd = true;
            _isNotified.Set();

            // cancel current loading operation
            Cancellation.Cancel();
            LoadTask.ContinueWith(parent =>
            {
                Cancellation.Dispose();
                foreach (var pair in _viewsBack)
                {
                    pair.Value.Dispose();
                }
                _viewsBack = null;
                _viewsFront = null;
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
        QueryEvaluator Create(IExecutableQuery query);
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

        public QueryEvaluator Create(IExecutableQuery query)
        {
            return new QueryEvaluator(_fileWatcherFactory, _thumbnailFactory, _entities, query);
        }
    }
}
