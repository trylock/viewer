using System;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly IEntityManager _entityManager;
        private readonly IClipboardService _clipboard;
        private readonly IQueryHistory _queryHistory;
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
        /// Current state of selection
        /// </summary>
        private readonly SelectionState _selection;

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
            IQueryHistory queryHistory,
            IQueryFactory queryFactory,
            IQueryEvaluatorFactory queryEvaluatorFactory)
        {
            View = view;
            _editor = editor;
            _explorer = explorer;
            _presentation = presentation;
            _dialogView = dialogView;
            _selection = new SelectionState(View, selection);
            _entityManager = entityManager;
            _clipboard = clipboard;
            _queryFactory = queryFactory;
            _queryEvaluatorFactory = queryEvaluatorFactory;
            _queryHistory = queryHistory;
            
            // initialize view
            View.ItemSize = CurrentItemSize;
            View.ContextOptions = Settings.Default.ExternalApplications;

            // subsribe to events 
            _queryHistory.QueryExecuted += QueryHistory_QueryExecuted;
            Settings.Default.PropertyChanged += Settings_PropertyChanged;
            SubscribeTo(View, "View");
            SubscribeTo(View.History, "HistoryView");

            QueryHistory_QueryExecuted(this, new QueryEventArgs(_queryHistory.Current));
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
            _selection.Dispose();
            DisposeQuery();
            Settings.Default.PropertyChanged -= Settings_PropertyChanged;
            _queryHistory.QueryExecuted -= QueryHistory_QueryExecuted;
            base.Dispose();
        }
        
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ExternalApplications")
            {
                View.ContextOptions = Settings.Default.ExternalApplications;
            }
        }

        private void QueryHistory_QueryExecuted(object sender, QueryEventArgs e)
        {
            if (e.Query == null)
            {
                return;
            }

            View.History.Items = _queryHistory
                .Distinct(QueryTextComparer.Default)
                .Select(item => new QueryHistoryItem(item))
                .ToList();
            View.History.SelectedItem = View.History.Items
                .FirstOrDefault(item => QueryTextComparer.Default.Equals(item.Query, e.Query));
        }

        /// <summary>
        /// Execute given query and show all entities in the result.
        /// </summary>
        /// <param name="query">Query to show</param>
        public async Task LoadQueryAsync(IExecutableQuery query)
        {
            // release all resources used by the previous query
            DisposeQuery();
            
            _selection.Clear();

            // start the query
            _queryEvaluator = _queryEvaluatorFactory.Create(query);
            View.Query = _queryEvaluator.Query.Text;
            View.Items = _queryEvaluator.Update();
            View.History.CanGoBackInHistory = _queryHistory.Previous != null;
            View.History.CanGoForwardInHistory = _queryHistory.Next != null;
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
        
        private void View_BeginDragItems(object sender, EventArgs e)
        {
            var dragFiles = _selection.GetPathsInSelection().ToArray();
            var data = new DataObject(DataFormats.FileDrop, dragFiles);
            View.BeginDragDrop(data, DragDropEffects.Move);
        }
        
        private void View_BeginEditItemName(object sender, EventArgs e)
        {
            var entity = _selection.ActiveItem;
            if (entity == null)
            {
                return;
            }

            View.ShowItemEditForm(entity);
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
            var item = _selection.ActiveItem;
            if (item == null)
            {
                return;
            }

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
        
        private void View_CopyItems(object sender, EventArgs e)
        {
            var paths = _selection.GetPathsInSelection();
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
            var paths = _selection.GetPathsInSelection();
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
            if (!_selection.Any())
            {
                return;
            }

            // confirm delete
            var filesToDelete = _selection.GetPathsInSelection().ToArray();
            if (!_dialogView.ConfirmDelete(filesToDelete))
            {
                return;
            }

            // delete entities
            var entitiesInSelection = _selection.Select(item => item.Data);
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
            _selection.Clear();

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

        private async void View_OpenItem(object sender, EventArgs e)
        {
            if (!_selection.Any())
            {
                return;
            }

            var view = _selection.ActiveItem;
            if (view == null)
            {
                return;
            }
            
            if (view.Data is FileEntity fileEntity)
            {
                var items = View.Items.Select(item => item.Data).OfType<FileEntity>().ToList();
                var index = items.IndexOf(fileEntity);
                await _presentation.OpenAsync(items, index < 0 ? 0 : index);
            }
            else
            {
                var query = _queryFactory.CreateQuery(view.FullPath);
                _queryHistory.ExecuteQuery(query);
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
            _queryHistory.Back();
        }

        private void HistoryView_GoForwardInHistory(object sender, EventArgs e)
        {
            _queryHistory.Forward();
        }

        private void HistoryView_UserSelectedItem(object sender, EventArgs e)
        {
            var query = View.History.SelectedItem?.Query;
            if (query == null || _queryHistory.Current == query)
            {
                return;
            }

            _queryHistory.ExecuteQuery(query);
        }

        private void HistoryView_GoUp(object sender, EventArgs e)
        {
            var query = _queryHistory.Current;
            if (query == null)
            {
                return;
            }

            // build the parent folder query
            IExecutableQuery nextQuery = null;
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
                    nextQuery = _queryFactory.Union(parentQuery, nextQuery);
                }
            }

            if (nextQuery == null)
            {
                return;
            }

            // execute it
            _queryHistory.ExecuteQuery(nextQuery);
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
            if (_queryHistory.Current != null)
            {
                _queryHistory.ExecuteQuery(_queryHistory.Current);
            }
        }

        private void View_ShowQuery(object sender, EventArgs e)
        {
            if (_queryHistory.Current != null)
            {
                var window = _editor.OpenNew(_queryHistory.Current.Text);
                window.Show(View.DockPanel, DockState.Document);
            }
        }

        private void View_RunProgram(object sender, ProgramEventArgs e)
        {
            // select files for which the program will run
            var entities = _selection.Select(view => view.Data);
            if (!e.Program.RunWithDirectories)
            {
                entities = entities.OfType<FileEntity>();
            }

            if (!e.Program.RunWithFiles)
            {
                entities = entities.OfType<DirectoryEntity>();
            }

            // run the program iff there is at least one file
            if (!entities.Any())
            {
                return;
            }

            if (!e.Program.AllowMultiplePaths)
            {
                var active = _selection.ActiveItem;
                if (active == null)
                {
                    return;
                }

                entities = new[] { active.Data };
            }

            try
            {
                e.Program.Run(entities.Select(item => item.Path));
            }
            catch (Win32Exception)
            {
            }
        }

        #endregion
    }
}
