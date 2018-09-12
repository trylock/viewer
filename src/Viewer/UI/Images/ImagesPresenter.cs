using System;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Data;
using Viewer.Images;
using Viewer.IO;
using Viewer.Query;
using Viewer.Core.UI;
using Viewer.Data.Properties;
using Viewer.Properties;
using Viewer.UI.Explorer;
using Viewer.UI.Forms;
using Viewer.UI.Presentation;
using Viewer.UI.QueryEditor;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    internal class ImagesPresenter : Presenter<IImagesView>
    {
        private readonly IEditor _editor;
        private readonly IExplorer _explorer;
        private readonly IPresentation _presentation;
        private readonly IFileSystemErrorView _dialogView;
        private readonly ISelection _selection;
        private readonly IEntityManager _entityManager;
        private readonly IClipboardService _clipboard;
        private readonly IQueryHistory _state;
        private readonly IQueryFactory _queryFactory;
        private readonly IQueryEvaluatorFactory _queryEvaluatorFactory;

        private static Size MinItemSize => new Size(133, 100);
        private static Size MaxItemSize => new Size(
            MinItemSize.Width * 3,
            MinItemSize.Height * 3
        );
        
        /// <summary>
        /// Current thumbnail size in the [0, 1] range. See <see cref="SetThumbnailSize"/>.
        /// </summary>
        private double _thumbnailSize = 0;

        /// <summary>
        /// Current state of the rectangle selection
        /// </summary>
        private readonly RectangleSelection<EntityView> _rectangleSelection = new RectangleSelection<EntityView>(new EntityViewPathComparer());

        /// <summary>
        /// Currently loaded query
        /// </summary>
        private QueryEvaluator _queryEvaluator;
        
        /// <summary>
        /// Minimal time in milliseconds between 2 poll events.
        /// </summary>
        private const int PollingRate = 100;
        
        /// <summary>
        /// Label in which status of current query evaluation is shown.
        /// If this is null, query evaluation status won't be shown.
        /// </summary>
        public IStatusBarItem StatusLabel { get; set; }

        /// <summary>
        /// Label which shows current number of items in the query result set.
        /// </summary>
        public IStatusBarItem ItemCountLabel { get; set; }

        /// <summary>
        /// Get current selection strategy based on the state of modifier keys.
        /// If a shift key is pressed, use <see cref="UnionSelectionStrategy{T}"/>.
        /// If a control key is pressed, use <see cref="SymetricDifferenceSelectionStrategy{T}"/>.
        /// Otherwise, use <see cref="ReplaceSelectionStrategy{T}"/>.
        /// </summary>
        public ISelectionStrategy<EntityView> CurrentSelectionStrategy
        {
            get
            {
                ISelectionStrategy<EntityView> strategy = ReplaceSelectionStrategy<EntityView>.Default;
                if (View.ModifierKeyState.HasFlag(Keys.Shift))
                {
                    strategy = UnionSelectionStrategy<EntityView>.Default;
                }
                else if (View.ModifierKeyState.HasFlag(Keys.Control))
                {
                    strategy = SymetricDifferenceSelectionStrategy<EntityView>.Default;
                }

                return strategy;
            }
        }

        /// <summary>
        /// Current item size. It depends on current thumbnail size (see <see cref="SetThumbnailSize"/>).
        /// </summary>
        public Size CurrentItemSize
        {
            get
            {
                var minimal = MinItemSize;
                var maximal = MaxItemSize;
                var weight = _thumbnailSize;
                return new Size(
                    (int)(MathUtils.Lerp(minimal.Width, maximal.Width, weight)),
                    (int)(MathUtils.Lerp(minimal.Height, maximal.Height, weight))
                );
            }
        }
        
        public ImagesPresenter(
            IImagesView view,
            IEditor editor,
            IExplorer explorer,
            IPresentation presentation,
            IFileSystemErrorView dialogView,
            ISelection selection, 
            IEntityManager entityManager,
            IClipboardService clipboard,
            IQueryHistory state,
            IQueryFactory queryFactory,
            IQueryEvaluatorFactory queryEvaluatorFactory)
        {
            View = view;
            _editor = editor;
            _explorer = explorer;
            _presentation = presentation;
            _dialogView = dialogView;
            _selection = selection;
            _entityManager = entityManager;
            _clipboard = clipboard;
            _state = state;
            _queryFactory = queryFactory;
            _queryEvaluatorFactory = queryEvaluatorFactory;

            _state.QueryExecuted += StateOnQueryExecuted;

            // initialize context menu options
            UpdateContextOptions();

            // initialize view
            View.ItemSize = CurrentItemSize;
            SubscribeTo(View, "View");
            SubscribeTo(View.History, "HistoryView");
        }

        private void StateOnQueryExecuted(object sender, QueryEventArgs e)
        {
            View.History.Items = _state
                .Distinct(QueryTextComparer.Default)
                .OfType<IQuery>()
                .Select(item => new QueryHistoryItem(item))
                .ToList();
            View.History.SelectedItem = View.History.Items
                .FirstOrDefault(item => QueryTextComparer.Default.Equals(item.Query, e.Query));
        }

        private void UpdateContextOptions()
        {
            View.ContextOptions = Settings.Default.ExternalApplications;
        }

        private bool _isDisposed = false;

        /// <summary>
        /// Dispose all resources used by current query
        /// </summary>
        private void DisposeQuery()
        {
            _queryEvaluator?.Dispose();
            _queryEvaluator = null;
            View.Items = null; // the items have been disposed by the query evaluator
        }
        
        public override void Dispose()
        {
            _isDisposed = true;
            DisposeQuery();
            _state.QueryExecuted -= StateOnQueryExecuted;
            base.Dispose();
        }
        
        /// <summary>
        /// Execute given query and show all entities in the result.
        /// </summary>
        /// <param name="query">Query to show</param>
        public async Task LoadQueryAsync(IQuery query)
        {
            // release all resources used by the previous query
            DisposeQuery();

            // reset presenter state
            _rectangleSelection.Clear();
            _selection.Clear();

            // start the query
            _queryEvaluator = _queryEvaluatorFactory.Create(query);
            View.Query = _queryEvaluator.Query.Text;
            View.Items = _queryEvaluator.Update();
            View.History.CanGoBackInHistory = _state.Previous != null;
            View.History.CanGoForwardInHistory = _state.Next != null;
            View.BeginLoading();
            View.BeginPolling(PollingRate);
            
            try
            {
                await _queryEvaluator.RunAsync();
                if (!_isDisposed)
                {
                    View.UpdateItems();
                }
            }   
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (!_isDisposed)
                {
                    View.EndLoading();
                }
            }
        }

        /// <summary>
        /// Set thumbnail size and update the view.
        /// </summary>
        /// <param name="thumbnailSize">Thumbnail size in the [0, 1] range</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="thumbnailSize"/> is not in the [0, 1] range</exception>
        public void SetThumbnailSize(double thumbnailSize)
        {
            if (thumbnailSize < 0 || thumbnailSize > 1)
                throw new ArgumentOutOfRangeException(nameof(thumbnailSize));

            _thumbnailSize = thumbnailSize;
            View.ItemSize = CurrentItemSize;
            View.UpdateItems();
        }

        private void ChangeSelection(IEnumerable<EntityView> newSelection)
        {
            var changed = _rectangleSelection.Set(newSelection, View.Items);
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
                item.State = _rectangleSelection.Contains(item) ? 
                    EntityViewState.Selected : 
                    EntityViewState.None;
            }

            View.UpdateItems();
        }

        private IEnumerable<IEntity> GetEntitiesInSelection()
        {
            return _rectangleSelection.Select(item => item.Data);
        }

        #region User input

        private void View_Poll(object sender, EventArgs e)
        {
            if (_queryEvaluator != null)
            {
                View.Items = _queryEvaluator.Update();
                View.ItemSize = CurrentItemSize;

                // update query evaluation status
                if (StatusLabel != null)
                {
                    var loadingFile = _queryEvaluator.Progress.LoadingFile;
                    var loadedCount = _queryEvaluator.Progress.FileCount;
                    StatusLabel.Text = loadingFile != null ? $"{loadedCount:N0}: {loadingFile}" : "Done.";
                }

                // update item count
                if (ItemCountLabel != null)
                {
                    ItemCountLabel.Text = string.Format(Resources.ItemCount_Label, View.Items?.Count);
                }
            }

            View.UpdateItems();
        }

        private void View_SelectionBegin(object sender, MouseEventArgs e)
        {
            _rectangleSelection.Begin(e.Location, CurrentSelectionStrategy);
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
            var strategy = CurrentSelectionStrategy;
            if (View.ModifierKeyState == Keys.Shift)
            {
                strategy = RangeSelectionStrategy<EntityView>.Default;
            }

            _rectangleSelection.Begin(Point.Empty, strategy);
            ChangeSelection(new[] { e.Entity });
            _rectangleSelection.End();
        }

        private void View_BeginDragItems(object sender, EventArgs e)
        {
            var dragFiles = GetPathsInSelection().ToArray();
            var data = new DataObject(DataFormats.FileDrop, dragFiles);
            View.BeginDragDrop(data, DragDropEffects.Move);
        }
        
        private void View_HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                ChangeSelection(View.Items);
            }
        }
        
        private void View_BeginEditItemName(object sender, EntityEventArgs e)
        {
            View.ShowItemEditForm(e.Entity);
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
                _dialogView.InvalidFileName(e.NewName);
                return;
            }

            // construct the new file path
            var item = e.Entity;
            var basePath = PathUtils.GetDirectoryPath(item.FullPath);
            var newPath = Path.Combine(basePath, e.NewName + Path.GetExtension(item.FullPath));

            // rename the file
            try
            {
                _entityManager.MoveEntity(item.Data, newPath);
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

            // update view
            if (_queryEvaluator != null)
            {
                View.Items = _queryEvaluator.Update();
                View.UpdateItems();
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
            var paths = GetPathsInSelection();
            try
            {
                _clipboard.SetFiles(new ClipboardFileDrop(paths, DragDropEffects.Copy));
            }
            catch (ExternalException ex)
            {
                _dialogView.ClipboardIsBusy(ex.Message);
            }
        }

        private void View_CutItems(object sender, EventArgs e)
        {
            var paths = GetPathsInSelection();
            try
            {
                _clipboard.SetFiles(new ClipboardFileDrop(paths, DragDropEffects.Move));
            }
            catch (ExternalException ex)
            {
                _dialogView.ClipboardIsBusy(ex.Message);
            }
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

            // delete entities
            var entitiesInSelection = _rectangleSelection.Select(item => item.Data);
            foreach (var entity in entitiesInSelection)
            {
                try
                {
                    _entityManager.RemoveEntity(entity);
                }
                catch (UnauthorizedAccessException)
                {
                    _dialogView.UnauthorizedAccess(entity.Path);
                }
                catch (DirectoryNotFoundException)
                {
                    // ignore
                }
                catch (PathTooLongException)
                {
                    _dialogView.PathTooLong(entity.Path);
                }
                catch (IOException)
                {
                    _dialogView.FileInUse(entity.Path);
                }
            }

            // clear selection
            ChangeSelection(Enumerable.Empty<EntityView>());

            // update view
            if (_queryEvaluator != null)
            {
                View.Items = _queryEvaluator.Update();
                View.UpdateItems();
            }
        }

        private async void View_ItemClick(object sender, EntityEventArgs e)
        {
            if (!(e.Entity.Data is FileEntity fileEntity))
            {
                return;
            }

            var items = View.Items.Select(item => item.Data).OfType<FileEntity>().ToList();
            var index = items.IndexOf(fileEntity);
            await _presentation.PreviewAsync(items, index);
        }

        private async void View_OpenItem(object sender, EntityEventArgs e)
        {
            if (!_rectangleSelection.Any())
            {
                return;
            }
            
            if (e.Entity.Data is FileEntity fileEntity)
            {
                var items = View.Items.Select(item => item.Data).OfType<FileEntity>().ToList();
                var index = items.IndexOf(fileEntity);
                await _presentation.OpenAsync(items, index < 0 ? 0 : index);
            }
            else
            {
                var query = _queryFactory.CreateQuery(e.Entity.FullPath);
                _state.ExecuteQuery(query);
            }
        }
        
        private void View_CloseView(object sender, EventArgs eventArgs)
        {
            if (StatusLabel != null)
            {
                StatusLabel.Text = "Done.";
            }

            if (ItemCountLabel != null)
            {
                ItemCountLabel.Text = "";
            }
            
            _selection.Clear();
        }
        
        private void HistoryView_GoBackInHistory(object sender, EventArgs e)
        {
            _state.Back();
        }

        private void HistoryView_GoForwardInHistory(object sender, EventArgs e)
        {
            _state.Forward();
        }

        private void HistoryView_UserSelectedItem(object sender, EventArgs e)
        {
            var query = View.History.SelectedItem?.Query;
            if (query == null || _state.Current == query)
            {
                return;
            }

            _state.ExecuteQuery(query);
        }

        private void HistoryView_GoUp(object sender, EventArgs e)
        {
            var query = _state.Current;
            if (query == null)
            {
                return;
            }

            // build the parent folder query
            IQuery nextQuery = null;
            foreach (var pattern in query.Patterns)
            {
                var parentPattern = pattern.GetParent();
                var parentQuery = _queryFactory.CreateQuery(parentPattern.Text);
                if (nextQuery == null)
                {
                    nextQuery = parentQuery;
                }
                else
                {
                    nextQuery = nextQuery.Union(parentQuery);
                }
            }

            if (nextQuery == null)
            {
                return;
            }

            // execute it
            _state.ExecuteQuery(nextQuery);
        }

        private IReadOnlyCollection<string> FindAllFolders()
        {
            if (_queryEvaluator == null)
            {
                return new List<string>();
            }

            return _queryEvaluator.GetSearchedDirectories().ToList();
        }

        private async Task CopyMoveFilesToViewAsync(
            string destinationDirectory, 
            DragDropEffects allowedEffects, 
            IEnumerable<string> files)
        {
            // pick the destination directory
            if (destinationDirectory == null)
            {
                var folders = FindAllFolders();
                if (folders.Count < 1)
                {
                    return;
                }
                if (folders.Count == 1)
                {
                    destinationDirectory = folders.First();
                }
                else
                {
                    try
                    {
                        destinationDirectory = await View.PickDirectoryAsync(folders);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }

            Trace.Assert(destinationDirectory != null);

            // ignore copy/move dest => dest operation
            var movedFiles = files.Where(path =>
                !string.Equals(path, destinationDirectory, StringComparison.CurrentCultureIgnoreCase));
            if (!movedFiles.Any())
            {
                return;
            }

            try
            {
                if ((allowedEffects & DragDropEffects.Move) != 0)
                {
                    await _explorer.MoveFilesAsync(destinationDirectory, movedFiles);
                }
                else if ((allowedEffects & DragDropEffects.Copy) != 0)
                {
                    await _explorer.CopyFilesAsync(destinationDirectory, movedFiles);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async void View_OnPaste(object sender, EventArgs e)
        {
            var files = _clipboard.GetFiles();
            if (!files.Any())
            {
                return;
            }

            await CopyMoveFilesToViewAsync(null, files.Effect, files);
        }

        private async void View_OnDrop(object sender, DropEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null)
            {
                return;
            }

            await CopyMoveFilesToViewAsync(e.Entity?.FullPath, e.AllowedEffect, files);
        }

        private void View_RefreshQuery(object sender, EventArgs e)
        {
            if (_state.Current != null)
            {
                _state.ExecuteQuery(_state.Current);
            }
        }

        private void View_ShowQuery(object sender, EventArgs e)
        {
            if (_state.Current != null)
            {
                var window = _editor.OpenNew(_state.Current.Text);
                window.Show(View.DockPanel, DockState.Document);
            }
        }

        #endregion
    }
}
