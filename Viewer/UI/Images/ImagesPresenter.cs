using System;
using System.IO;
using System.Collections.Generic;
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

        protected override ExportLifetimeContext<IImagesView> ViewLifetime { get; }
        
        /// <summary>
        /// Minimal time in milliseconds between 2 view updates during a loading operation
        /// </summary>
        private const int MinViewUpdateDelay = 100;

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
        /// Index of an active item (item over which is a mouse cursor) 
        /// or -1 if no item is active.
        /// </summary>
        public int ActiveItem { get; private set; } = -1;

        /// <summary>
        /// Index of an item with focus or -1 it there is no such item.
        /// </summary>
        public int FocusedItem { get; private set; } = -1;

        [ImportingConstructor]
        public ImagesPresenter(
            ExportFactory<IImagesView> viewFactory,
            IFileSystemErrorView dialogView,
            ISelection selection, 
            IEntityManager entityManager,
            IClipboardService clipboard,
            IImageLoader imageLoader,
            IApplicationState state)
        {
            ViewLifetime = viewFactory.CreateExport();
            _dialogView = dialogView;
            _selection = selection;
            _entityManager = entityManager;
            _clipboard = clipboard;
            _imageLoader = imageLoader;
            _state = state;
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

            View.BeginLoading();
            try
            {
                _loadTask = Task.Run(() => LoadQueryBackground(query), _query.Cancellation.Token);
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

            // update view
            Clear();
        }

        /// <summary>
        /// Load entities from the query.
        /// This function assumes that it runs on a background thread
        /// </summary>
        /// <param name="query"></param>
        private void LoadQueryBackground(IQuery query)
        {
            var lastNofitication = DateTime.Now;
            var waiting = new Queue<IEntity>();
            foreach (var entity in query)
            {
                query.Cancellation.Token.ThrowIfCancellationRequested();

                waiting.Enqueue(entity);

                var delay = DateTime.Now - lastNofitication;
                if (delay.TotalMilliseconds >= MinViewUpdateDelay)
                {
                    var localQueue = waiting;
                    waiting = new Queue<IEntity>();
                    lastNofitication = DateTime.Now;
                    View.BeginInvoke(new Action(() => ViewEntities(localQueue)));
                }
            }

            if (waiting.Count > 0)
            {
                View.BeginInvoke(new Action(() => ViewEntities(waiting)));
            }
        }
        
        /// <summary>
        /// Reset presenter and view state
        /// </summary>
        private void Clear()
        {
            // reset state
            _thumbnailSizeCalculator.Reset();
            _rectangleSelection.Clear();
            _selection.Clear();
            ActiveItem = -1;
            FocusedItem = -1;

            // reset view
            if (View.Items == null)
            {
                View.Items = new List<EntityView>();
            }
            else
            {
                foreach (var item in View.Items)
                {
                    item.Dispose();
                }
                View.Items.Clear();
            }
        }
        
        /// <summary>
        /// Add entities to the view
        /// </summary>
        /// <param name="entities"></param>
        private void ViewEntities(IEnumerable<IEntity> entities)
        {
            foreach (var entity in entities)
            {
                _minItemSize = _thumbnailSizeCalculator.AddEntity(entity);
                AddEntityView(new EntityView(entity, GetThumbnail(entity)));
            }
            View.ItemSize = ComputeThumbnailSize();
            View.UpdateItems();
        }

        private void AddEntityView(EntityView view)
        {
            View.Items.Add(view);

            var index = View.Items.Count - 2;
            while (index >= 0)
            {
                var comparison = _query.Comparer.Compare(View.Items[index].Data, View.Items[index + 1].Data);
                if (comparison > 0)
                {
                    var tmp = View.Items[index];
                    View.Items[index] = View.Items[index + 1];
                    View.Items[index + 1] = tmp;
                    --index;
                }
                else
                {
                    break;
                }
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
        
        private void UpdateSelection(Point endPoint)
        {
            var bounds = _rectangleSelection.GetBounds(endPoint);
            var newSelection = View.GetItemsIn(bounds);
            ChangeSelection(newSelection);

            View.ShowSelection(bounds);
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
        
        private void View_HandleMouseDown(object sender, MouseEventArgs e)
        {
            var index = View.GetItemAt(e.Location);

            // update view
            View.EnsureVisible();
            View.HideItemEditForm();

            // udpate focused item
            FocusedItem = index;

            // update selection
            if (index >= 0)
            {
                if (!_rectangleSelection.Contains(View.Items[index]))
                {
                    // make the active item the only item in selection
                    ChangeSelection(new []{ index });
                }
            }
            else
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
        }

        private void View_HandleMouseUp(object sender, MouseEventArgs e)
        {
            if (_rectangleSelection.IsActive)
            {
                UpdateSelection(e.Location);
                _rectangleSelection.End();
                View.HideSelection();
            }
        }

        private void View_HandleMouseMove(object sender, MouseEventArgs e)
        {
            // update active item
            var item = View.GetItemAt(e.Location);
            if (item != ActiveItem)
            {
                if (item >= 0 && item < View.Items.Count && !_rectangleSelection.Contains(View.Items[item]))
                {
                    View.Items[item].State = EntityViewState.Active;
                    View.UpdateItem(item);
                }

                if (ActiveItem >= 0 && ActiveItem < View.Items.Count && !_rectangleSelection.Contains(View.Items[ActiveItem]))
                {
                    View.Items[ActiveItem].State = EntityViewState.None;
                    View.UpdateItem(ActiveItem);
                }

                ActiveItem = item;
            }

            // update focused item
            if ((e.Button & MouseButtons.Left) != 0 ||
                (e.Button & MouseButtons.Right) != 0)
            {
                FocusedItem = item;
            }

            // update selection
            if (_rectangleSelection.IsActive)
            {
                UpdateSelection(e.Location);
            }
            else if ((e.Button & MouseButtons.Left) != 0)
            {
                // begin a drag&drop operation
                var dragFiles = _rectangleSelection.Select(elem => elem.FullPath).ToArray();
                var data = new DataObject(DataFormats.FileDrop, dragFiles);
                View.BeginDragDrop(data, DragDropEffects.Copy);
            }
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

        private void View_BeginEditItemName(object sender, EventArgs e)
        {
            var index = FocusedItem;
            if (index < 0)
            {
                return;
            }

            View.ShowItemEditForm(index);
        }

        private void View_CancelEditItemName(object sender, EventArgs e)
        {
            View.HideItemEditForm();
        }

        private void View_RenameItem(object sender, RenameEventArgs e)
        {
            var index = FocusedItem;
            if (index < 0)
            {
                return;
            }

            // check the new file name
            if (!PathUtils.IsValidFileName(e.NewName))
            {
                _dialogView.InvalidFileName(e.NewName, PathUtils.GetInvalidFileCharacters());
                return;
            }

            // construct the new file path
            var item = View.Items[index].Data;
            var basePath = PathUtils.GetDirectoryPath(item.Path);
            var newPath = Path.Combine(basePath, e.NewName + Path.GetExtension(item.Path));

            // rename the file
            try
            {
                _entityManager.MoveEntity(item.Path, newPath);
                View.UpdateItem(index);
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
            
            // reset active and focused items if necessary
            if (ActiveItem >= View.Items.Count)
            {
                ActiveItem = -1;
            }

            if (FocusedItem >= View.Items.Count)
            {
                FocusedItem = -1;
            }

            View.UpdateItems();
        }

        private void View_OpenItem(object sender, EventArgs e)
        {
            if (ActiveItem < 0 || ActiveItem >= View.Items.Count)
            {
                return;
            }

            var entities = View.Items.Select(item => item.Data);
            _state.OpenEntity(entities, ActiveItem);
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
