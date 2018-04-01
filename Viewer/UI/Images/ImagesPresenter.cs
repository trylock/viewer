using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.IO;
using Viewer.UI.Explorer;

namespace Viewer.UI.Images
{
    public class ImagesPresenter
    {
        // dependencies
        private readonly IImagesView _imagesView;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IAttributeStorage _storage;
        private readonly IClipboardService _clipboard;
        private readonly IThumbnailGenerator _thumbnailGenerator;

        // current state
        private Size _itemSize = new Size(150, 100);
        private readonly List<AttributeCollection> _items = new List<AttributeCollection>();

        public ImagesPresenter(
            IImagesView imagesView, 
            IFileSystemErrorView dialogView,
            IAttributeStorage storage, 
            IClipboardService clipboard,
            IThumbnailGenerator thumbnailGenerator)
        {
            _storage = storage;
            _clipboard = clipboard;
            _thumbnailGenerator = thumbnailGenerator;

            _dialogView = dialogView;

            _imagesView = imagesView;
            _imagesView.HandleMouseDown += View_MouseDown;
            _imagesView.HandleMouseUp += View_MouseUp;
            _imagesView.HandleMouseMove += View_MouseMove;
            _imagesView.HandleKeyDown += View_HandleShortcuts;
            _imagesView.HandleKeyDown += View_CaptureControlKeys;
            _imagesView.HandleKeyUp += View_CaptureControlKeys;
            _imagesView.BeginEditItemName += View_BeginEditItemName;
            _imagesView.CancelEditItemName += View_CancelEditItemName;
            _imagesView.DeleteItems += View_DeleteItems;
            _imagesView.RenameItem += View_RenameItem;
            _imagesView.CopyItems += View_CopyItems;
            _imagesView.Resize += View_Resize;

            _imagesView.UpdateSize();
        }
        
        public void AddItemsInternal(IEnumerable<AttributeCollection> items)
        {
            _items.AddRange(items);
        }

        public void LoadDirectory(string path)
        {
            // dispose old query
            foreach (var item in _items)
            {
                item.Dispose();
            }
            _items.Clear();

            // load new data
            foreach (var file in Directory.EnumerateFiles(path))
            {
                var attrs = _storage.Load(file);
                _items.Add(attrs);
            }

            _storage.Flush();

            // update view
            _imagesView.Items = _items.Select(attrs => new ResultItemView(attrs, GetThumbnail(attrs))).ToList();
            _imagesView.UpdateItems();
        }

        private Image GetThumbnail(AttributeCollection item)
        {
            var image = ((ImageAttribute)item["thumbnail"]).Value;
            try
            {
                return _thumbnailGenerator.GetThumbnail(image, _itemSize);
            }
            finally
            {
                image.Dispose();
            }
        }

        private void StartSelection(Point location)
        {
            _isSelectionActive = true;
            _selectionStartPoint = location;
            _previousSelection.Clear();
            _previousSelection.AddRange(_currentSelection);
        }

        private void EndSelection(Point endPoint)
        {
            _isSelectionActive = false;
            UpdateSelection(endPoint);
            _imagesView.HideSelection();
        }

        private Rectangle GetSelectionBounds(Point endPoint)
        {
            var minX = Math.Min(_selectionStartPoint.X, endPoint.X);
            var maxX = Math.Max(_selectionStartPoint.X, endPoint.X);
            var minY = Math.Min(_selectionStartPoint.Y, endPoint.Y);
            var maxY = Math.Max(_selectionStartPoint.Y, endPoint.Y);
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
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

        private void UpdateSelection(Point endPoint)
        {
            var selectionStrategy = SelectionStrategy.Replace;
            if (_isShift)
            {
                selectionStrategy = SelectionStrategy.Union;
            }
            else if (_isControl)
            {
                selectionStrategy = SelectionStrategy.SymetricDifference;
            }

            var bounds = GetSelectionBounds(endPoint);
            var newSelection = _imagesView.GetItemsIn(bounds);
            ChangeSelection(newSelection, selectionStrategy);
            _imagesView.ShowSelection(bounds);
        }
        
        private void ChangeSelection(IEnumerable<int> newSelection, SelectionStrategy strategy)
        {
            // reset state of items in current selection
            foreach (var item in _currentSelection)
            {
                _imagesView.Items[item].State = ResultItemState.None;
            }
            _imagesView.UpdateItems(_currentSelection);

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

            // set state of items in current selection
            foreach (var item in _currentSelection)
            {
                _imagesView.Items[item].State = ResultItemState.Selected;
            }
            _imagesView.UpdateItems(_currentSelection);
        }

        #region User input

        // presenter UI state 
        private readonly List<int> _previousSelection = new List<int>();
        private readonly HashSet<int> _currentSelection = new HashSet<int>();
        private Point _selectionStartPoint;
        private bool _isSelectionActive = false;
        private bool _isControl = false;
        private bool _isShift = false;

        /// <summary>
        /// Index of an active item (item over which is a mouse cursor) 
        /// or -1 if no item is active.
        /// </summary>
        public int ActiveItem { get; private set; } = -1;

        /// <summary>
        /// Index of an item with focus or -1 it there is no such item.
        /// </summary>
        public int FocusedItem { get; private set; } = -1;

        /// <summary>
        /// List of items currently in selection
        /// </summary>
        public IEnumerable<int> Selection => _currentSelection;

        private void View_MouseDown(object sender, MouseEventArgs e)
        {
            var index = _imagesView.GetItemAt(e.Location);

            // update view
            _imagesView.MakeActive();
            _imagesView.HideItemEditForm();

            // udpate focused item
            FocusedItem = index;

            // update selection
            if (index >= 0)
            {
                if (!_currentSelection.Contains(index))
                {
                    // make the active item the only item in selection
                    ChangeSelection(
                        Enumerable.Repeat(index, 1), 
                        SelectionStrategy.Replace);
                }

                // begin the move operation on files in selection
                if ((e.Button & MouseButtons.Left) != 0)
                {
                    var dragFiles = _currentSelection.Select(item => _items[item].Path).ToArray();
                    var data = new DataObject(DataFormats.FileDrop, dragFiles);
                    _imagesView.BeginDragDrop(data, DragDropEffects.Copy);
                }
            }
            else
            {
                StartSelection(e.Location);
            }
        }

        private void View_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isSelectionActive)
            {
                EndSelection(e.Location);
            }
        }

        private void View_MouseMove(object sender, MouseEventArgs e)
        {
            // update active item
            var item = _imagesView.GetItemAt(e.Location);
            if (item != ActiveItem)
            {
                if (item >= 0 && !_currentSelection.Contains(item))
                {
                    _imagesView.Items[item].State = ResultItemState.Active;
                    _imagesView.UpdateItem(item);
                }

                if (ActiveItem >= 0 && !_currentSelection.Contains(ActiveItem))
                {
                    _imagesView.Items[ActiveItem].State = ResultItemState.None;
                    _imagesView.UpdateItem(ActiveItem);
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
            if (_isSelectionActive)
            {
                UpdateSelection(e.Location);
            }
        }

        private void View_Resize(object sender, EventArgs e)
        {
            _imagesView.UpdateSize();
        }
        
        private void View_CaptureControlKeys(object sender, KeyEventArgs e)
        {
            _isControl = e.Control;
            _isShift = e.Shift;
        }

        private void View_HandleShortcuts(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                ChangeSelection(Enumerable.Range(0, _items.Count), SelectionStrategy.Replace);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _imagesView.HideItemEditForm();
            }
        }
        
        private void View_BeginEditItemName(object sender, EventArgs e)
        {
            var index = FocusedItem;
            if (index < 0)
            {
                return;
            }

            _imagesView.ShowItemEditForm(index);
        }

        private void View_CancelEditItemName(object sender, EventArgs e)
        {
            _imagesView.HideItemEditForm();
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
            var item = _items[index];
            var basePath = item.Path.Substring(0, item.Path.LastIndexOfAny(PathUtils.PathSeparators));
            var newPath = Path.Combine(basePath, e.NewName + Path.GetExtension(item.Path));

            // rename the file
            try
            {
                File.Move(item.Path, newPath);
                item.Path = newPath;
                _imagesView.UpdateItem(index);
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
                _imagesView.HideItemEditForm();
            }
        }

        private void View_CopyItems(object sender, EventArgs e)
        {
            _clipboard.SetFiles(_currentSelection.Select(index => _items[index].Path));
            _clipboard.SetPreferredEffect(DragDropEffects.Copy);
        }

        private void View_DeleteItems(object sender, EventArgs e)
        {
            if (_currentSelection.Count <= 0)
            {
                return;
            }

            // confirm delete
            var filesToDelete = _currentSelection.Select(index => _items[index].Path);
            if (!_dialogView.ConfirmDelete(filesToDelete))
            {
                return;
            }

            // delete files 
            var actuallyDeleted = new HashSet<string>();
            foreach (var index in _currentSelection)
            {
                var item = _items[index];
                try
                {
                    File.Delete(item.Path);
                    _imagesView.Items[index].Dispose();
                    actuallyDeleted.Add(item.Path);
                }
                catch (UnauthorizedAccessException)
                {
                    _dialogView.UnauthorizedAccess(item.Path);
                }
                catch (DirectoryNotFoundException ex)
                {
                    _dialogView.DirectoryNotFound(ex.Message);
                }
                catch (PathTooLongException)
                {
                    _dialogView.PathTooLong(item.Path);
                }
                catch (IOException)
                {
                    _dialogView.FileInUse(item.Path);
                }
            }

            // remove deleted items from the view
            _imagesView.Items = (
                from item in _imagesView.Items
                where !actuallyDeleted.Contains(item.FullPath)
                select item
            ).ToList();
            _imagesView.UpdateItems();

            // remove deleted items from the result
            _items.RemoveAll(item => actuallyDeleted.Contains(item.Path));
        }

        #endregion
    }
}
