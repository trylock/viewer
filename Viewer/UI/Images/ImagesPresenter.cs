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
        private readonly IAttributeStorage _storage;
        private readonly IClipboardService _clipboard;
        private readonly IImageLoader _imageLoader;
        private readonly ExportFactory<PresentationPresenter> _presentationFactory;

        protected override ExportLifetimeContext<IImagesView> ViewLifetime { get; }

        // current state
        private IEntityManager _entities;
        private Size _minItemSize = new Size(133, 100);
        private readonly List<int> _previousSelection = new List<int>();
        private readonly HashSet<int> _currentSelection = new HashSet<int>();
        private readonly RectangleSelection _rectangleSelection = new RectangleSelection();
        private bool _isControl;
        private bool _isShift;

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
            IAttributeStorage storage,
            IClipboardService clipboard,
            IImageLoader imageLoader,
            ExportFactory<PresentationPresenter> presentationFactory)
        {
            ViewLifetime = viewFactory.CreateExport();
            _dialogView = dialogView;
            _selection = selection;
            _storage = storage;
            _clipboard = clipboard;
            _imageLoader = imageLoader;
            _presentationFactory = presentationFactory;

            View.ThumbnailSizeMinimum = 1;
            View.ThumbnailSizeMaximum = 100;
            View.ThumbnailSize = 1;
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
        /// Show all entities in the view.
        /// Previously loaded entities will be released.
        /// </summary>
        /// <param name="entities">Entities to show</param>
        public void LoadFromQueryResult(IEntityManager entities)
        {
            if (_entities != entities)
            {
                DisposeViewData();
                _entities = entities;
            }

            // find optimal item size
            _minItemSize = FindMinimalImageSize(entities);
            View.ItemSize = _minItemSize;

            // add entities with the default thumbnail
            foreach (var entity in _entities)
            {
                View.Items.Add(new EntityView(entity, GetThumbnail(entity)));
            }
            View.UpdateItems();
        }
        
        private void DisposeViewData()
        {
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
        /// Create lazily initialized thumbnail for an entity
        /// </summary>
        /// <param name="item">Entity</param>
        /// <returns>Thumbnail</returns>
        private Lazy<Image> GetThumbnail(IEntity item)
        {
            return new Lazy<Image>(() => _imageLoader.LoadThumbnail(item, View.ItemSize));
        }

        /// <summary>
        /// Determine minimal thumbnail size from the most common aspect ratio among images
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        private Size FindMinimalImageSize(IEnumerable<IEntity> entities)
        {
            var frequency = new Dictionary<Fraction, int>();
            foreach (var entity in entities)
            {
                var size = _imageLoader.GetImageSize(entity);
                var ratio = new Fraction(size.Width, size.Height);
                if (frequency.ContainsKey(ratio))
                {
                    frequency[ratio]++;
                }
                else
                {
                    frequency.Add(ratio, 1);
                }
            }

            if (frequency.Count == 0)
            {
                return new Size(133, 100);
            }

            // find the most common aspect ratio
            var maxFrequency = 0;
            var maxRatio = new Fraction(4, 3);
            foreach (var pair in frequency)
            {
                if (pair.Value > maxFrequency)
                {
                    maxFrequency = pair.Value;
                    maxRatio = pair.Key;
                }
            }
            
            // determine the size
            var aspectRatio = (double) maxRatio;
            if (aspectRatio > 1)
            {
                return new Size((int) (100 * aspectRatio), 100);
            }
            else
            {
                return new Size(100, (int) (100 / aspectRatio));
            }
        }
        
        private void UpdateSelection(Point endPoint)
        {
            var bounds = _rectangleSelection.GetBounds(endPoint);
            var newSelection = View.GetItemsIn(bounds);
            var strategy = SelectionStrategy.Replace;
            if (_isShift)
            {
                strategy = SelectionStrategy.Union;
            }
            else if (_isControl)
            {
                strategy = SelectionStrategy.SymetricDifference;
            }

            ChangeSelection(newSelection, strategy);
            View.ShowSelection(bounds);
        }

        private enum SelectionStrategy
        {
            /// <summary>
            /// Previous selection will be replaced with the new selection.
            /// </summary>
            Replace,

            /// <summary>
            /// Compute union with the old selection.
            /// </summary>
            Union,

            /// <summary>
            /// Compute symetric difference with the previous selection.
            /// </summary>
            SymetricDifference,
        }

        private void ChangeSelection(IEnumerable<int> newSelection, SelectionStrategy strategy)
        {
            var oldSelection = new int[_currentSelection.Count];
            _currentSelection.CopyTo(oldSelection, 0);

            // update current selection 
            _currentSelection.Clear();
            _currentSelection.UnionWith(newSelection);
            switch (strategy)
            {
                case SelectionStrategy.Union:
                    _currentSelection.UnionWith(_previousSelection);
                    break;
                case SelectionStrategy.SymetricDifference:
                    _currentSelection.SymmetricExceptWith(_previousSelection);
                    break;
            }

            // if the selection did not change, don't update the view or the model
            if (_currentSelection.SetEquals(oldSelection))
            {
                return;
            }

            // set global selection
            _selection.Replace(_entities, _currentSelection);
            
            // reset state of items in previous selection
            foreach (var item in oldSelection)
            {
                if (item < 0 || item >= View.Items.Count)
                    continue;
                View.Items[item].State = ResultItemState.None;
            }
            View.UpdateItems(oldSelection);

            // set state of items in current selection
            foreach (var item in _currentSelection)
            {
                View.Items[item].State = ResultItemState.Selected;
            }
            View.UpdateItems(_currentSelection);
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
                if (!_currentSelection.Contains(index))
                {
                    // make the active item the only item in selection
                    ChangeSelection(new []{ index }, SelectionStrategy.Replace);
                }
            }
            else
            {
                _rectangleSelection.StartSelection(e.Location);
                _previousSelection.Clear();
                _previousSelection.AddRange(_currentSelection);
            }
        }

        private void View_HandleMouseUp(object sender, MouseEventArgs e)
        {
            if (_rectangleSelection.IsActive)
            {
                UpdateSelection(e.Location);
                _rectangleSelection.EndSelection();
                View.HideSelection();
            }
        }

        private void View_HandleMouseMove(object sender, MouseEventArgs e)
        {
            // update active item
            var item = View.GetItemAt(e.Location);
            if (item != ActiveItem)
            {
                if (item >= 0 && !_currentSelection.Contains(item))
                {
                    View.Items[item].State = ResultItemState.Active;
                    View.UpdateItem(item);
                }

                if (ActiveItem >= 0 && !_currentSelection.Contains(ActiveItem))
                {
                    View.Items[ActiveItem].State = ResultItemState.None;
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
                var dragFiles = _currentSelection.Select(elem => View.Items[elem].FullPath).ToArray();
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
                ChangeSelection(Enumerable.Range(0, View.Items.Count), SelectionStrategy.Replace);
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
                _storage.Move(item.Path, newPath);
                var updatedEntity = item.ChangePath(newPath);
                _entities[index] = updatedEntity;
                View.Items[index].Data = updatedEntity;
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
            _selection.Replace(_entities, _currentSelection);
        }

        private IEnumerable<string> GetPathsInSelection()
        {
            return _selection.Select(index => _selection.Items[index].Path);
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
                var path = View.Items[item].FullPath;
                try
                {
                    _storage.Remove(path);
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
            _entities.RemoveAll(entity => deletedPaths.Contains(entity.Path));
            View.Items.RemoveAll(view => deletedPaths.Contains(view.FullPath));

            // clear selection
            ChangeSelection(Enumerable.Empty<int>(), SelectionStrategy.Replace);
            
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
            if (ActiveItem < 0 || ActiveItem >= _entities.Count)
            {
                return;
            }

            var presentationExport = _presentationFactory.CreateExport();
            presentationExport.Value.ShowEntity(_entities, ActiveItem);
            presentationExport.Value.View.CloseView += (s, args) =>
            {
                presentationExport.Dispose();
                presentationExport = null;
            };
            presentationExport.Value.ShowView("Presentation", DockState.Document);
        }
        
        private void View_CloseView(object sender, EventArgs eventArgs)
        {
            Dispose();
        }

        private void View_ThumbnailSizeChanged(object sender, EventArgs e)
        {
            // compute the new thumbnail size
            var scale = 1 + (View.ThumbnailSize - View.ThumbnailSizeMinimum) /
                        (double)(View.ThumbnailSizeMaximum - View.ThumbnailSizeMinimum);
            var itemSize = new Size(
                (int)(_minItemSize.Width * scale),
                (int)(_minItemSize.Height * scale)
            );

            // scale existing thumbnail
            View.ItemSize = itemSize;
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
