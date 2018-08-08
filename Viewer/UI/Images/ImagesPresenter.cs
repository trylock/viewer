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
using Viewer.Images;
using Viewer.IO;
using Viewer.Query;
using Viewer.Collections;
using Viewer.UI.Explorer;
using Viewer.UI.QueryEditor;
using Viewer.UI.Settings;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ImagesPresenter : Presenter<IImagesView>
    {
        private readonly IEditor _editor;
        private readonly IFileSystem _fileSystem;
        private readonly IFileSystemErrorView _dialogView;
        private readonly ISystemExplorer _explorer;
        private readonly ISelection _selection;
        private readonly IEntityManager _entityManager;
        private readonly IClipboardService _clipboard;
        private readonly IImageLoader _imageLoader;
        private readonly IApplicationState _state;
        private readonly IQueryFactory _queryFactory;
        private readonly IThumbnailSizeCalculator _thumbnailSizeCalculator;
        private readonly IQueryEvaluatorFactory _queryEvaluatorFactory;
        private readonly ISettings _settings;

        protected override ExportLifetimeContext<IImagesView> ViewLifetime { get; }

        private Size _currentItemSize = new Size(133, 100);
        private Size _minItemSize = new Size(133, 100);
        private Size MaxItemSize => new Size(
            _minItemSize.Width * 3,
            _minItemSize.Height * 3
        );

        /// <summary>
        /// Current state of the rectangle selection
        /// </summary>
        private readonly RectangleSelection<IFileView> _rectangleSelection = new RectangleSelection<IFileView>(new FileViewPathComparer());
        
        /// <summary>
        /// true iff constrol is pressed
        /// </summary>
        private bool _isControl;

        /// <summary>
        /// true iff shift is pressed
        /// </summary>
        private bool _isShift;

        /// <summary>
        /// Currently loaded query
        /// </summary>
        private QueryEvaluator _queryEvaluator;

        /// <summary>
        /// Minimal time in milliseconds between 2 poll events.
        /// </summary>
        private const int PollingRate = 200;
        
        [ImportingConstructor]
        public ImagesPresenter(
            ExportFactory<IImagesView> viewFactory,
            IEditor editor,
            IFileSystem fileSystem,
            IFileSystemErrorView dialogView,
            ISystemExplorer explorer,
            ISelection selection, 
            IEntityManager entityManager,
            IClipboardService clipboard,
            IImageLoader imageLoader,
            IApplicationState state,
            IQueryFactory queryFactory,
            IQueryEvaluatorFactory queryEvaluatorFactory,
            ISettings settings)
        {
            ViewLifetime = viewFactory.CreateExport();
            _editor = editor;
            _fileSystem = fileSystem;
            _dialogView = dialogView;
            _explorer = explorer;
            _selection = selection;
            _entityManager = entityManager;
            _clipboard = clipboard;
            _imageLoader = imageLoader;
            _state = state;
            _queryFactory = queryFactory;
            _queryEvaluatorFactory = queryEvaluatorFactory;
            _thumbnailSizeCalculator = new FrequentRatioThumbnailSizeCalculator(_imageLoader, 100);

            // initialize context menu options
            _settings = settings;
            _settings.Changed += SettingsOnChanged;
            UpdateContextOptions();

            // initialize view
            View.ItemSize = _currentItemSize;
            SubscribeTo(View, "View");
        }

        private void SettingsOnChanged(object sender, EventArgs e)
        {
            UpdateContextOptions();
        }

        private void UpdateContextOptions()
        {
            View.ContextOptions = (
                from app in _settings.Applications
                select new ExternalApplicationOption(app)
            ).ToList();
        }
        
        public override void Dispose()
        {
            foreach (var item in View.Items)
            {
                item.Dispose();
            }
            _settings.Changed -= SettingsOnChanged;
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
            _queryEvaluator = _queryEvaluatorFactory.Create(query);

            View.Query = _queryEvaluator.Query.Text;
            View.Items = new SortedList<IFileView>(_queryEvaluator.Comparer);
            View.BeginLoading();
            View.BeginPolling(PollingRate);

            try
            {
                await _queryEvaluator.RunAsync();
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
            if (_queryEvaluator != null)
            {
                // cancel previous load operation
                _queryEvaluator.Cancellation.Cancel();

                // wait for it to end
                try
                {
                    await _queryEvaluator.LoadTask;
                }
                catch (OperationCanceledException)
                {
                }
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
        /// Compute current thumbnail size based on the current minimal thumbnail size
        /// and View.ThumbnailScale
        /// </summary>
        /// <returns></returns>
        private Size ComputeThumbnailSize()
        {
            var minimal = _minItemSize;
            var maximal = MaxItemSize;
            var weight = View.ThumbnailScale;
            return new Size(
                (int)(MathUtils.Lerp(minimal.Width, maximal.Width, weight)),
                (int)(MathUtils.Lerp(minimal.Height, maximal.Height, weight))
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
            var items = _queryEvaluator.Consume();
            if (items.Count > 0)
            {
                // update thumbnail size 
                foreach (var item in items)
                {
                    if (item is FileView view)
                    {
                        _minItemSize = _thumbnailSizeCalculator.AddEntity(view.Data);
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
            var strategy = SelectionStrategy.Replace;
            if (_isShift)
            {
                strategy = SelectionStrategy.Union;
            }
            else if (_isControl)
            {
                strategy = SelectionStrategy.SymetricDifference;
            }
            _rectangleSelection.Begin(Point.Empty, strategy);
            ChangeSelection(new[] { e.Index });
            _rectangleSelection.End();
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
                View.UpdateItems();
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
            return _rectangleSelection.Select(item => item.FullPath);
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
            var filesInSelection = _rectangleSelection.OfType<FileView>().Select(item => item.FullPath);
            foreach (var path in filesInSelection)
            {
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

            // delete folders
            var foldersInSelection = _rectangleSelection.OfType<DirectoryView>().Select(item => item.FullPath);
            foreach (var folder in foldersInSelection)
            {
                try
                {
                    _fileSystem.DeleteDirectory(folder, true);

                    deletedPaths.Add(folder);
                }
                catch (UnauthorizedAccessException)
                {
                    _dialogView.UnauthorizedAccess(folder);
                }
                catch (DirectoryNotFoundException)
                {
                    // ignore directory not found
                    deletedPaths.Add(folder);
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

        private void View_OpenItemInExplorer(object sender, EntityEventArgs e)
        {
            if (e.Index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(e));
            }

            _explorer.OpenFile(View.Items[e.Index].FullPath);
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

            _currentItemSize = View.ItemSize;
        }

        private void View_ThumbnailSizeCommit(object sender, EventArgs e)
        {
            View.UpdateItems();
        }

        private void View_ShowCode(object sender, EventArgs e)
        {
            _editor.OpenNew(_queryEvaluator.Query.Text, DockState.Document);
        }

        #endregion
        
    }
}
