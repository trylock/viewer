using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;

namespace Viewer.UI.Images
{
    public class ImagesPresenter
    {
        // dependencies
        private readonly IImagesView _imagesView;
        private readonly IAttributeStorage _storage;
        private readonly IThumbnailGenerator _thumbnailGenerator;

        // current state
        private Size _itemSize = new Size(150, 100);
        private readonly List<AttributeCollection> _items = new List<AttributeCollection>();

        public ImagesPresenter(IImagesView imagesView, IAttributeStorage storage, IThumbnailGenerator thumbnailGenerator)
        {
            _storage = storage;
            _thumbnailGenerator = thumbnailGenerator;
            _imagesView = imagesView;
            _imagesView.HandleMouseDown += View_MouseDown;
            _imagesView.HandleMouseUp += View_MouseUp;
            _imagesView.HandleMouseMove += View_MouseMove;
            _imagesView.HandleKeyDown += View_HandleShortcuts;
            _imagesView.HandleKeyDown += View_CaptureControlKeys;
            _imagesView.HandleKeyUp += View_CaptureControlKeys;
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
            _imagesView.LoadItems(_items.Select(attrs => new ResultItemView(attrs, GetThumbnail(attrs))));
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
            None,
            Union,
            SymetricDifference,
        }

        private void UpdateSelection(Point endPoint)
        {
            var selectionStrategy = SelectionStrategy.None;
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

        private void ChangeSelection(IEnumerable<int> newSelection, SelectionStrategy selection)
        {
            // reset state of items in current selection
            foreach (var item in _currentSelection)
            {
                _imagesView.SetState(item, ResultItemState.None);
            }
            _imagesView.UpdateItems(_currentSelection);

            // update current selection 
            _currentSelection.Clear();
            _currentSelection.UnionWith(newSelection);
            switch (selection)
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
                _imagesView.SetState(item, ResultItemState.Selected);
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
            // activate the view
            _imagesView.MakeActive();

            // update selection
            var index = _imagesView.GetItemAt(e.Location);
            if (index >= 0)
            {
                if (!_currentSelection.Contains(index))
                {
                    // make the active item the only item in selection
                    _previousSelection.Clear();
                    _previousSelection.AddRange(_currentSelection);

                    _currentSelection.Clear();
                    _currentSelection.Add(index);

                    // update item states
                    foreach (var item in _previousSelection)
                    {
                        _imagesView.SetState(item, ResultItemState.None);
                    }
                    _imagesView.SetState(index, ResultItemState.Selected);
                    _imagesView.UpdateItems(_previousSelection);
                    _imagesView.UpdateItems(_currentSelection);
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
            EndSelection(e.Location);
        }

        private void View_MouseMove(object sender, MouseEventArgs e)
        {
            // update active item
            var item = _imagesView.GetItemAt(e.Location);
            if (item != ActiveItem)
            {
                if (!_currentSelection.Contains(item))
                {
                    _imagesView.SetState(item, ResultItemState.Active);
                    _imagesView.UpdateItem(item);
                }

                if (!_currentSelection.Contains(ActiveItem))
                {
                    _imagesView.SetState(ActiveItem, ResultItemState.None);
                    _imagesView.UpdateItem(ActiveItem);
                }

                ActiveItem = item;
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
                ChangeSelection(Enumerable.Range(0, _items.Count), SelectionStrategy.None);
            }
        }

        #endregion
    }
}
