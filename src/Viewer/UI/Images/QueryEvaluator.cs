using System;
using System.CodeDom;
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
    /// <para>
    /// This object is responsible for collection returned from the <see cref="Update"/> method.
    /// <see cref="Dispose"/>ing this object will dispose items in this collection. The caller
    /// must not use the collection after calling <see cref="Dispose"/> except for the
    /// <see cref="EntityView.Data"/> property of items in the collection.
    /// </para>
    ///
    /// <para>
    /// Query evaluation works in 3 stages:
    /// <list type="number">
    /// <item>
    /// <description>Find files from the query result set</description>
    /// </item>
    /// <item>
    /// <description>
    /// Group and sort values in the query result set. This is done in batches rather than for
    /// individual items since that will yield better performance.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Show photos to the user (this is done on the UI thread, see <see cref="Update"/>)
    /// </description>
    /// </item>
    /// </list>
    ///
    /// These stages are executed on separate threads so that they don't block each other. Data
    /// between (1) and (2) is shared using queues. Data between (2) and (3) is shared using
    /// additional back buffer which is updated in the 2nd stage and swaped with the front buffer
    /// in the 3rd stage to avoid race conditions.
    /// </para>
    /// </remarks>
    public class QueryEvaluator : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // dependencies
        private readonly IEntityManager _entities;
        private readonly IFileWatcher _fileWatcher;
        private readonly ILazyThumbnailFactory _thumbnailFactory;

        // state
        private readonly object _backBufferLock = new object();
        private bool _isBackBufferDirty = false;
        private List<Group> _backBuffer;
        private List<Group> _frontBuffer;
        private readonly ConcurrentQueue<Request> _requests = new ConcurrentQueue<Request>();
        private readonly AutoResetEvent _processBatch = new AutoResetEvent(false);
        private Thread _batchProcessingThread;

        /// <summary>
        /// Number of pending requests in <see cref="_requests"/>. This property is accessed and
        /// written to from multiple threads.
        /// </summary>
        private int _requestCount;

        /// <summary>
        /// Minimal time between a failed load operation and its retry
        /// </summary>
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximal retry operation count.
        /// </summary>
        private const int MaxRetryCount = 4;

        private abstract class Request
        {
            /// <summary>
            /// Return path of an existing entity (entity which has already been shown to the user)
            /// which this request modifies.
            /// </summary>
            /// <returns>Modified path or null</returns>
            public virtual string GetModifiedPath()
            {
                return null;
            }

            public virtual EntityView GetAddedView()
            {
                return null;
            }
        }
        
        private sealed class AddRequest : Request
        {
            public EntityView Value { get; }

            public AddRequest(EntityView value)
            {
                Value = value ?? throw new ArgumentNullException(nameof(value));
            }

            public override EntityView GetAddedView()
            {
                return Value;
            }
        }

        private sealed class ModifyRequest : Request
        {
            public IEntity Value { get; }

            public ModifyRequest(IEntity value)
            {
                Value = value ?? throw new ArgumentNullException(nameof(value));
            }

            public override string GetModifiedPath()
            {
                return Value.Path;
            }
        }

        private sealed class RemoveRequest : Request
        {
            public string Path { get; }

            public RemoveRequest(string path)
            {
                Path = path ?? throw new ArgumentNullException(nameof(path));
            }

            public override string GetModifiedPath()
            {
                return Path;
            }
        }

        private sealed class MoveRequest : Request
        {
            public string OldPath { get; }
            public string NewPath { get; }

            public MoveRequest(string oldPath, string newPath)
            {
                OldPath = oldPath ?? throw new ArgumentNullException(nameof(oldPath));
                NewPath = newPath ?? throw new ArgumentNullException(nameof(newPath));
            }

            public override string GetModifiedPath()
            {
                return OldPath;
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
            _backBuffer = new List<Group>();
            _frontBuffer = new List<Group>();

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
            EnqueueRequest(new RemoveRequest(e.Value.Path));
        }

        private void EntitiesOnMoved(object sender, EntityMovedEventArgs e)
        {
            // We have already changed entity's path, just have to make sure the list is still
            // sorted.
            EnqueueRequest(new ModifyRequest(e.Value));
        }

        private void EntitiesOnChanged(object sender, Data.EntityEventArgs e)
        {
            EnqueueRequest(new ModifyRequest(e.Value));
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
            EnqueueRequest(new RemoveRequest(path));
        }

        private void FileWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            // note: side effect of this check is that it ignores move operations done during the
            //       FileSystemAttributeStorage.Store call. 
            if (!IsEntityEvent(e))
                return; // skip this event
            var oldPath = PathUtils.NormalizePath(e.OldFullPath);
            var newPath = PathUtils.NormalizePath(e.FullPath);
            EnqueueRequest(new MoveRequest(oldPath, newPath));
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
                var view = new EntityView(entity, thumbnail);
                EnqueueRequest(new AddRequest(view));
            }
        }

        #endregion

        private void EnqueueRequest(Request req)
        {
            _requests.Enqueue(req);
            var value = Interlocked.Increment(ref _requestCount);
            if ((value % 100) == 0)
                _processBatch.Set();
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
                EnqueueRequest(new AddRequest(new EntityView(entity, thumbnail)));
            }
        }

        /// <summary>
        /// List of removed entities waiting to be disposed (by an <see cref="Update"/> call).
        /// We can't disposed them on a background thread because they are shared with the UI
        /// thread.
        /// </summary>
        /// <remarks>
        /// Access to this property is guarded by a lock on <see cref="_backBufferLock"/>.
        /// </remarks>
        private readonly List<EntityView> _removed = new List<EntityView>();
        private volatile bool _isEnd = false;

        private void ProcessRequests()
        {
            while (!_isEnd)
            {
                _processBatch.WaitOne(TimeSpan.FromMilliseconds(200));
                
                lock (_backBufferLock)
                {
                    ProcessChanges();
                }
            }
            _processBatch.Dispose();
        }

        private List<EntityView> GetOrAddList(
            BaseValue key,
            Dictionary<BaseValue, List<EntityView>> index)
        {
            if (!index.TryGetValue(key, out var list))
            {
                list = new List<EntityView>();
                index.Add(key, list);
            }

            return list;
        }

        public void ProcessChanges()
        {
            var index = new Dictionary<string, Request>(StringComparer.CurrentCultureIgnoreCase);
            var added = new Dictionary<BaseValue, List<EntityView>>();
            while (_requests.TryDequeue(out var req))
            {
                // group added views
                var item = req.GetAddedView();
                if (item != null)
                {
                    var key = Query.GetGroup(item.Data);
                    var list = GetOrAddList(key, added);
                    list.Add(item);
                }

                // index modification requests
                var path = req.GetModifiedPath();
                if (path != null)
                {
                    index[path] = req;
                }
            }

            // When the UI thread acquires the lock, it should update the front buffer only if
            // there has been changes. Otherwise, it would block the UI for too long on each update.
            _isBackBufferDirty = index.Count > 0 || added.Count > 0;
            
            // apply changes to the data in the back buffer
            if (index.Count > 0)
            {
                var groupHead = 0;
                for (var i = 0; i < _backBuffer.Count; ++i)
                {
                    Trace.Assert(groupHead <= i, $"{nameof(groupHead)} <= {nameof(i)}");
                    var group = _backBuffer[i];

                    // update items in the group
                    var head = 0;
                    for (var j = 0; j < group.Items.Count; ++j)
                    {
                        Trace.Assert(head <= j, $"{nameof(head)} <= {nameof(j)}");
                        var item = group.Items[j];
                        if (!index.TryGetValue(item.FullPath, out var req))
                        {
                            // this item has not changed
                            group.Items[head] = item;
                            ++head;
                        }
                        else if (req is RemoveRequest)
                        {
                            _removed.Add(item);
                        }
                        else if (req is MoveRequest moveReq)
                        {
                            item.Data.ChangePath(moveReq.NewPath);
                            var key = Query.GetGroup(item.Data);
                            var list = GetOrAddList(key, added);
                            list.Add(item);
                        }
                        else if (req is ModifyRequest modReq)
                        {
                            item = new EntityView(modReq.Value, item.Thumbnail);
                            var key = Query.GetGroup(item.Data);
                            var list = GetOrAddList(key, added);
                            list.Add(item);
                        }
                    }

                    group.Items.RemoveRange(head, group.Items.Count - head);
                    
                    // remove the group if it is empty
                    if (group.Items.Count > 0)
                    {
                        _backBuffer[groupHead] = group;
                        ++groupHead;
                    }
                }

                _backBuffer.RemoveRange(groupHead, _backBuffer.Count - groupHead);
            }

            // add new groups
            if (added.Count > 0)
            {
                // merge items of existing groups
                for (var i = 0; i < _backBuffer.Count; ++i)
                {
                    var group = _backBuffer[i];
                    if (!added.TryGetValue(group.Key, out var list))
                    {
                        continue;
                    }

                    group.Items.Sort(Comparer);
                    group.Items = group.Items.Merge(list, Comparer);
                    added.Remove(group.Key);
                }

                // add brand new groups
                var addedGroups = added
                    .Select(pair => new Group(pair.Key)
                    {
                        Items = pair.Value
                    })
                    .ToList();
                addedGroups.Sort();
                foreach (var group in addedGroups)
                {
                    group.Items.Sort(Comparer);
                }

                _backBuffer = _backBuffer.Merge(addedGroups, Comparer<Group>.Default);
            }
        }
        
        /// <summary>
        /// Update current collection. It takes all changes made so far and applies them to
        /// the local collection of <see cref="EntityView"/>s. 
        /// </summary>
        /// <returns>Modified collection</returns>
        public List<Group> Update()
        {
            var entered = Monitor.TryEnter(_backBufferLock, TimeSpan.FromMilliseconds(20));
            if (!entered)
            {
                return _frontBuffer;
            }

            try
            {
                if (!_isBackBufferDirty)
                {
                    return _frontBuffer;
                }

                _isBackBufferDirty = false;

                // dispose removed items
                foreach (var item in _removed)
                {
                    item.Dispose();
                }
                _removed.Clear();

                _frontBuffer.Clear();
                foreach (var item in _backBuffer)
                {
                    _frontBuffer.Add(new Group(item));
                }

                return _frontBuffer;
            }
            finally
            {
                Monitor.Exit(_backBufferLock);
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
            _processBatch.Set();

            // cancel current loading operation
            Cancellation.Cancel();
            LoadTask.ContinueWith(parent =>
            {
                Cancellation.Dispose();
                foreach (var group in _backBuffer)
                {
                    group.Dispose();
                }
                _backBuffer = null;
                _frontBuffer = null;
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
