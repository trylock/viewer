using System;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Images;
using Viewer.IO;
using Viewer.Properties;
using Viewer.Query;
using Viewer.UI.Explorer;
using Viewer.UI.Presentation;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ImagesPresenter : Presenter<IImagesView>
    {
        private readonly IFileSystem _fileSystem;
        private readonly IFileSystemErrorView _dialogView;
        private readonly ISelection _selection;
        private readonly IEntityManager _entityManager;
        private readonly IClipboardService _clipboard;
        private readonly IImageLoader _imageLoader;
        private readonly IApplicationState _state;
        private readonly IQueryFactory _queryFactory;

        protected override ExportLifetimeContext<IImagesView> ViewLifetime { get; }

        /// <summary>
        /// Minimal size of a thumbnail
        /// (i.e. size of a thumbnail for View.ThumbnailScale == 1.0)
        /// </summary>
        private Size _minItemSize = new Size(133, 100);

        /// <summary>
        /// Current thumbnail area size
        /// </summary>
        private Size _thumbnailAreaSize = new Size(133, 100);

        /// <summary>
        /// Current state of the rectangle selection
        /// </summary>
        private readonly RectangleSelection<IFileView> _rectangleSelection = new RectangleSelection<IFileView>(new FileViewPathComparer());

        /// <summary>
        /// Thumbnail size calculator
        /// </summary>
        private readonly IThumbnailSizeCalculator _thumbnailSizeCalculator;

        /// <summary>
        /// true iff constrol is pressed
        /// </summary>
        private bool _isControl;

        /// <summary>
        /// true iff shift is pressed
        /// </summary>
        private bool _isShift;

        /// <summary>
        /// Last query load task
        /// </summary>
        private Task _loadTask = Task.CompletedTask;

        /// <summary>
        /// Currently loaded query
        /// </summary>
        private IQuery _query;
        
        /// <summary>
        /// Queue of entities loaded from the query which are not shown yet.
        /// </summary>
        private ConcurrentSortedSet<IFileView> _waitingQueue;

        /// <summary>
        /// Minimal time in milliseconds between 2 poll events.
        /// </summary>
        private const int PollingRate = 100;
        
        [ImportingConstructor]
        public ImagesPresenter(
            ExportFactory<IImagesView> viewFactory,
            IFileSystem fileSystem,
            IFileSystemErrorView dialogView,
            ISelection selection, 
            IEntityManager entityManager,
            IClipboardService clipboard,
            IImageLoader imageLoader,
            IApplicationState state,
            IQueryFactory queryFactory)
        {
            ViewLifetime = viewFactory.CreateExport();
            _fileSystem = fileSystem;
            _dialogView = dialogView;
            _selection = selection;
            _entityManager = entityManager;
            _clipboard = clipboard;
            _imageLoader = imageLoader;
            _state = state;
            _queryFactory = queryFactory;
            _thumbnailSizeCalculator = new FrequentRatioThumbnailSizeCalculator(_imageLoader, 100);

            View.ItemSize = _minItemSize;
            SubscribeTo(View, "View");
        }
        
        public override void Dispose()
        {
            foreach (var item in View.Items)
            {
                item.Dispose();
            }
            _selection.Clear();
            View.Items.Clear();
            base.Dispose();
        }

        /// <summary>
        /// Execute given query and show all entities in the result.
        /// </summary>
        /// <param name="query">Query to show</param>
        public async void LoadQueryAsync(IQuery query)
        {
            // cancel previous load operation and wait for it to end
            await CancelLoadAsync();

            // start loading a new query
            _query = query;
            _waitingQueue = new ConcurrentSortedSet<IFileView>(new FileViewComparer(_query.Comparer));

            View.Items = new SortedList<IFileView>(_waitingQueue.Comparer);
            View.BeginLoading();
            View.BeginPolling(PollingRate);

            try
            {
                _loadTask = Task.Run(() => LoadQueryBlocking(query), _query.Cancellation.Token);
                await _loadTask;
                View.UpdateItems();
            }   
            catch (OperationCanceledException)
            {
            }
            finally
            {
                View.EndLoading();
            }
        }

        private async Task CancelLoadAsync()
        {
            // cancel previous load operation
            _query?.Cancellation.Cancel();

            // wait for it to end
            try
            {
                await _loadTask;
            }
            catch (OperationCanceledException)
            {
            }

            // reset state
            _thumbnailSizeCalculator.Reset();
            _rectangleSelection.Clear();
            _selection.Clear();

            // reset view
            if (View.Items != null)
            {
                foreach (var item in View.Items)
                {
                    item.Dispose();
                }
                View.Items.Clear();
            }
        }

        /// <summary>
        /// Load entities from the query and put them to a waiting queue.
        /// </summary>
        /// <param name="query">Query to load</param>
        private void LoadQueryBlocking(IQuery query)
        {
            var directories = new HashSet<string>();

            foreach (var entity in query)
            {
                query.Cancellation.Token.ThrowIfCancellationRequested();
                
                // add the file to the result
                var item = new FileView(entity, GetPhotoThumbnail(entity));
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
                    _waitingQueue.Add(new DirectoryView(dir, GetDirectoryThumbnail(dir)));
                }
            }
        }
        
        /// <summary>
        /// Create lazily initialized thumbnail for a photo
        /// </summary>
        /// <param name="item">Entity</param>
        /// <returns>Thumbnail</returns>
        private ILazyThumbnail GetPhotoThumbnail(IEntity item)
        {
            return new PhotoThumbnail(_imageLoader, item, _thumbnailAreaSize);
        }

        /// <summary>
        /// Create lazily initialized thumbnail for a directory
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <returns>Thumbnail</returns>
        private ILazyThumbnail GetDirectoryThumbnail(string path)
        {
            return new DirectoryThumbnail(path);
        }

        /// <summary>
        /// Compute current thumbnail size based on the current minimal thumbnail size
        /// and View.ThumbnailScale
        /// </summary>
        /// <returns></returns>
        private Size ComputeThumbnailSize()
        {
            return new Size(
                (int)(_minItemSize.Width * View.ThumbnailScale),
                (int)(_minItemSize.Height * View.ThumbnailScale)
            );
        }

        private void ChangeSelection(IEnumerable<int> newSelection)
        {
            var selectedItems = newSelection.Select(index => View.Items[index]);
            var changed = _rectangleSelection.Set(selectedItems);
            if (!changed)
            {
                return;
            }

            UpdateSelectedItems();
        }

        private void UpdateSelectedItems()
        {
            // set global selection
            _selection.Replace(GetEntitiesInSelection());

            // update the view
            foreach (var item in View.Items)
            {
                if (_rectangleSelection.Contains(item))
                {
                    item.State = FileViewState.Selected;
                }
                else
                {
                    item.State = FileViewState.None;
                }
            }

            View.UpdateItems();
        }

        private IEnumerable<IEntity> GetEntitiesInSelection()
        {
            return _rectangleSelection.OfType<FileView>().Select(item => item.Data);
        }

        #region User input

        private void View_Poll(object sender, EventArgs e)
        {
            // get a snapshot of the waiting queue
            var items = _waitingQueue.Consume();
            if (items.Count > 0)
            {
                // update item size
                foreach (var item in items)
                {
                    if (item is FileView fileItem)
                    {
                        _minItemSize = _thumbnailSizeCalculator.AddEntity(fileItem.Data);
                    }
                }

                // show all entities in the snapshot
                View.Items = View.Items.Merge(items);
                View.ItemSize = ComputeThumbnailSize();
            }

            View.UpdateItems();
        }

        private void View_SelectionBegin(object sender, MouseEventArgs e)
        {
            var strategy = SelectionStrategy.Replace;
            if (_isShift)
            {
                strategy = SelectionStrategy.Union;
            }
            else if (_isControl)
            {
                strategy = SelectionStrategy.SymetricDifference;
            }
            _rectangleSelection.Begin(e.Location, strategy);
            UpdateSelectedItems();
        }

        private void View_SelectionDrag(object sender, MouseEventArgs e)
        {
            var bounds = _rectangleSelection.GetBounds(e.Location);
            var newSelection = View.GetItemsIn(bounds);
            ChangeSelection(newSelection);

            View.ShowSelection(bounds);
        }

        private void View_SelectionEnd(object sender, MouseEventArgs e)
        {
            var bounds = _rectangleSelection.GetBounds(e.Location);
            var newSelection = View.GetItemsIn(bounds);
            ChangeSelection(newSelection);

            _rectangleSelection.End();
            View.HideSelection();
        }

        private void View_SelectItem(object sender, EntityEventArgs e)
        {
            var item = View.Items[e.Index];
            if (!_rectangleSelection.Contains(item))
            {
                ChangeSelection(new[] { e.Index });
            }
        }

        private void View_BeginDragItems(object sender, EventArgs e)
        {
            var dragFiles = _rectangleSelection.Select(elem => elem.FullPath).ToArray();
            var data = new DataObject(DataFormats.FileDrop, dragFiles);
            View.BeginDragDrop(data, DragDropEffects.Copy);
        }
        
        private void View_HandleKeyDown(object sender, KeyEventArgs e)
        {
            _isControl = e.Control;
            _isShift = e.Shift;

            if (e.Control && e.KeyCode == Keys.A)
            {
                ChangeSelection(Enumerable.Range(0, View.Items.Count));
            }
        }
        
        private void View_HandleKeyUp(object sender, KeyEventArgs e)
        {
            _isControl = e.Control;
            _isShift = e.Shift;
        }

        private void View_BeginEditItemName(object sender, EntityEventArgs e)
        {
            View.ShowItemEditForm(e.Index);
        }

        private void View_CancelEditItemName(object sender, EventArgs e)
        {
            View.HideItemEditForm();
        }

        private void View_RenameItem(object sender, RenameEventArgs e)
        {
            // check the new file name 
            if (!PathUtils.IsValidFileName(e.NewName))
            {
                _dialogView.InvalidFileName(e.NewName, PathUtils.GetInvalidFileCharacters());
                return;
            }

            // construct the new file path
            var item = View.Items[e.Index];
            var basePath = PathUtils.GetDirectoryPath(item.FullPath);
            var newPath = Path.Combine(basePath, e.NewName + Path.GetExtension(item.FullPath));

            // rename the file
            try
            {
                if (item is FileView view)
                {
                    var entity = view.Data;
                    _entityManager.MoveEntity(entity.Path, newPath);
                }
                else
                {
                    _fileSystem.MoveDirectory(item.FullPath, newPath);
                }
                item.FullPath = newPath;
                View.UpdateItem(e.Index);
            }
            catch (PathTooLongException)
            {
                _dialogView.PathTooLong(newPath);
            }
            catch (DirectoryNotFoundException ex)
            {
                _dialogView.DirectoryNotFound(ex.Message);
            }
            catch (IOException)
            {
                _dialogView.FailedToMove(item.FullPath, newPath);
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(newPath);
            }
            finally
            {
                View.HideItemEditForm();
            }
        }

        private void View_ViewGotFocus(object sender, EventArgs e)
        {
            _selection.Replace(GetEntitiesInSelection());
        }

        private IEnumerable<string> GetPathsInSelection()
        {
            return _selection.Select(entity => entity.Path);
        }

        private void View_CopyItems(object sender, EventArgs e)
        {
            _clipboard.SetFiles(GetPathsInSelection());
            _clipboard.SetPreferredEffect(DragDropEffects.Copy);
        }

        private void View_DeleteItems(object sender, EventArgs e)
        {
            if (!_rectangleSelection.Any())
            {
                return;
            }

            // confirm delete
            var filesToDelete = GetPathsInSelection().ToArray();
            if (!_dialogView.ConfirmDelete(filesToDelete))
            {
                return;
            }

            // delete files 
            var deletedPaths = new HashSet<string>();
            foreach (var item in _selection)
            {
                var path = item.Path;
                try
                {
                    _entityManager.RemoveEntity(path);
                    deletedPaths.Add(path);
                }
                catch (UnauthorizedAccessException)
                {
                    _dialogView.UnauthorizedAccess(path);
                }
                catch (DirectoryNotFoundException ex)
                {
                    _dialogView.DirectoryNotFound(ex.Message);
                }
                catch (PathTooLongException)
                {
                    _dialogView.PathTooLong(path);
                }
                catch (IOException)
                {
                    _dialogView.FileInUse(path);
                }
            }

            // remove deleted items from the query and the view
            View.Items.RemoveAll(view => deletedPaths.Contains(view.FullPath));

            // clear selection
            ChangeSelection(Enumerable.Empty<int>());
            
            View.UpdateItems();
        }

        private void View_OpenItem(object sender, EntityEventArgs e)
        {
            if (e.Index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(e));
            }

            if (!_rectangleSelection.Any())
            {
                return;
            }
            
            // if the selection contains files only
            if (_rectangleSelection.All(item => item is FileView))
            {
                // count number of directories before selected item
                var directoryCount = 0;
                for (var i = 0; i < e.Index; ++i)
                {
                    if (View.Items[i].GetType() != typeof(FileView))
                    {
                        ++directoryCount;
                    }
                }

                // find index of the selected item after removing all directories
                var entities = View.Items.OfType<FileView>().Select(item => item.Data);
                var entityIndex = e.Index - directoryCount;
                _state.OpenEntity(entities, entityIndex);
            }
            else
            {
                var entity = View.Items[e.Index];
                var query = _queryFactory.CreateQuery(entity.FullPath);
                _state.ExecuteQuery(query);
            }
        }
        
        private async void View_CloseView(object sender, EventArgs eventArgs)
        {
            await CancelLoadAsync();
            Dispose();
        }

        private void View_ThumbnailSizeChanged(object sender, EventArgs e)
        {
            _thumbnailAreaSize = View.ItemSize = ComputeThumbnailSize();
            View.UpdateItems();
        }

        private void View_ThumbnailSizeCommit(object sender, EventArgs e)
        {
            // update the actual size of thumbnails in the view
            foreach (var item in View.Items)
            {
                item.Thumbnail.Resize(ComputeThumbnailSize());
            }

            // start loading the new thumbnails for visible items
            View.UpdateItems();
        }

        #endregion
        
    }
}
