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
        private readonly IFileSystemErrorView _dialogView;
        private readonly ISelection _selection;
        private readonly IEntityManager _entityManager;
        private readonly IClipboardService _clipboard;
        private readonly IImageLoader _imageLoader;
        private readonly IApplicationState _state;
        private readonly IThumbnailGenerator _thumbnailGenerator;

        protected override ExportLifetimeContext<IImagesView> ViewLifetime { get; }

        /// <summary>
        /// Minimal size of a thumbnail
        /// (i.e. size of a thumbnail for View.ThumbnailScale == 1.0)
        /// </summary>
        private Size _minItemSize = new Size(133, 100);
        
        /// <summary>
        /// Current state of the rectangle selection
        /// </summary>
        private readonly RectangleSelection<EntityView> _rectangleSelection = new RectangleSelection<EntityView>(new EntityViewPathComparer());

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
        private ImmutableSortedSet<EntityView> _waitingQueue = ImmutableSortedSet<EntityView>.Empty;

        /// <summary>
        /// Minimal time in milliseconds between 2 poll events.
        /// </summary>
        private const int PollingRate = 100;

        [ImportingConstructor]
        public ImagesPresenter(
            ExportFactory<IImagesView> viewFactory,
            IFileSystemErrorView dialogView,
            ISelection selection, 
            IEntityManager entityManager,
            IClipboardService clipboard,
            IImageLoader imageLoader,
            IApplicationState state,
            IThumbnailGenerator thumbnailGenerator)
        {
            ViewLifetime = viewFactory.CreateExport();
            _dialogView = dialogView;
            _selection = selection;
            _entityManager = entityManager;
            _clipboard = clipboard;
            _imageLoader = imageLoader;
            _state = state;
            _thumbnailGenerator = thumbnailGenerator;
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
            _waitingQueue = ImmutableSortedSet<EntityView>.Empty.WithComparer(new EntityViewComparer(_query.Comparer));

            View.Items = new SortedList<EntityView>(new EntityViewComparer(_query.Comparer));
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
        /// Load entities from the query and put them to a qaiting queue.
        /// </summary>
        /// <param name="query">Query to load</param>
        private void LoadQueryBlocking(IQuery query)
        {
            foreach (var entity in query)
            {
                query.Cancellation.Token.ThrowIfCancellationRequested();

                var item = new EntityView(entity, GetThumbnail(entity));
                ImmutableSortedSet<EntityView> newQueue, oldQueue;
                do
                {
                    oldQueue = _waitingQueue;
                    newQueue = oldQueue.Add(item);
                } while (Interlocked.CompareExchange(ref _waitingQueue, newQueue, oldQueue) != oldQueue);
            }
        }
        
        /// <summary>
        /// Create lazily initialized thumbnail for an entity
        /// </summary>
        /// <param name="item">Entity</param>
        /// <returns>Thumbnail</returns>
        private Lazy<Image> GetThumbnail(IEntity item)
        {
            return new Lazy<Image>(() => _imageLoader.LoadThumbnail(item, View.ItemSize));
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

            // set global selection
            _selection.Replace(_rectangleSelection.Select(item => item.Data));

            UpdateSelectedItems();
        }

        private void UpdateSelectedItems()
        {
            foreach (var item in View.Items)
            {
                if (_rectangleSelection.Contains(item))
                {
                    item.State = EntityViewState.Selected;
                }
                else
                {
                    item.State = EntityViewState.None;
                }
            }

            View.UpdateItems();
        }

        #region User input

        private void View_Poll(object sender, EventArgs e)
        {
            // get a snapshot of the waiting queue
            var empty = ImmutableSortedSet<EntityView>.Empty.WithComparer(new EntityViewComparer(_query.Comparer));
            var items = Interlocked.Exchange(ref _waitingQueue, empty);
            if (items.Count <= 0)
            {
                return;
            }

            // update item size
            foreach (var item in items)
            {
                _minItemSize = _thumbnailSizeCalculator.AddEntity(item.Data);
            }

            // show all entities in the snapshot
            View.Items = View.Items.Merge(items);
            View.ItemSize = ComputeThumbnailSize();
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
            else if (e.KeyCode == Keys.Escape)
            {
                View.HideItemEditForm();
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
            var item = View.Items[e.Index].Data;
            var basePath = PathUtils.GetDirectoryPath(item.Path);
            var newPath = Path.Combine(basePath, e.NewName + Path.GetExtension(item.Path));

            // rename the file
            try
            {
                _entityManager.MoveEntity(item.Path, newPath);
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
                _dialogView.FailedToMove(item.Path, newPath);
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
            _selection.Replace(_rectangleSelection.Select(item => item.Data));
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
            if (_selection.Count <= 0)
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

            var entityIndex = e.Index;
            var entities = View.Items.Select(item => item.Data);
            _state.OpenEntity(entities, entityIndex);
        }
        
        private async void View_CloseView(object sender, EventArgs eventArgs)
        {
            await CancelLoadAsync();
            Dispose();
        }

        private void View_ThumbnailSizeChanged(object sender, EventArgs e)
        {
            View.ItemSize = ComputeThumbnailSize();
            View.UpdateItems();
        }

        private void View_ThumbnailSizeCommit(object sender, EventArgs e)
        {
            // update the actual size of thumbnails in the view
            foreach (var item in View.Items)
            {
                item.Dispose();
                item.Thumbnail = GetThumbnail(item.Data);
            }

            // start loading the new thumbnails for visible items
            View.UpdateItems();
        }

        #endregion
        
    }
}
