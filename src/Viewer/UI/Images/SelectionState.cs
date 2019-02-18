using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Core.Collections;
using Viewer.Data;

namespace Viewer.UI.Images
{
    /// <summary>
    /// State of selection in thumbnail grid
    /// </summary>
    /// <remarks>
    /// This class contains selection logic for any thumbnail grid. It does not assume much
    /// about its view. 
    /// </remarks>
    internal class SelectionState : IEnumerable<IEntity>, IDisposable
    {
        private readonly ISelectionView _view;
        private readonly ISelection _selection;

        /// <summary>
        /// Event occurs whenever <see cref="ActiveItem"/> changes
        /// </summary>
        public event EventHandler ActiveItemChanged;

        /// <summary>
        /// The last item which user interacted with. This is used for range selection
        /// </summary>
        public EntityView ActiveItem { get; private set; }

        private EntityView _rangeSelectAnchorItem;

        /// <summary>
        /// Second item in the item range select operation (i.e., the operation will take all items
        /// between this and <see cref="ActiveItem"/>). If no item has been set as an anchor item,
        /// the first item from the view will be used.
        /// </summary>
        private EntityView RangeSelectAnchorItem
        {
            get
            {
                if (_rangeSelectAnchorItem == null)
                {
                    return GetFirstItem();
                }

                return _rangeSelectAnchorItem;
            }
            set => _rangeSelectAnchorItem = value;
        }

        /// <summary>
        /// Origin point of current range select 
        /// </summary>
        private Point _rangeOrigin;

        /// <summary>
        /// Target point of current range select
        /// </summary>
        private Point _rangeTarget;

        /// <summary>
        /// true iff range select is active 
        /// </summary>
        private bool _isRangeSelect;

        private readonly HashSet<IEntity> _currentSelection =
            new HashSet<IEntity>();

        private readonly HashSet<IEntity> _previousSelection =
            new HashSet<IEntity>();

        public SelectionState(ISelectionView view, ISelection selection)
        {
            _view = view;
            _selection = selection;

            _view.ProcessMouseDown += View_ProcessMouseDown;
            _view.ProcessMouseUp += View_ProcessMouseUp;
            _view.ProcessMouseMove += View_ProcessMouseMove;
            _view.HandleKeyDown += View_HandleKeyDown;
            _view.ViewActivated += View_ViewActivated;
            _view.ViewDeactivated += View_ViewDeactivated;
            _view.ItemDraw += View_ItemDraw;
        }

        public void Dispose()
        {
            _view.ProcessMouseDown -= View_ProcessMouseDown;
            _view.ProcessMouseUp -= View_ProcessMouseUp;
            _view.ProcessMouseMove -= View_ProcessMouseMove;
            _view.HandleKeyDown -= View_HandleKeyDown;
            _view.ViewActivated -= View_ViewActivated;
            _view.ViewDeactivated -= View_ViewDeactivated;
            _view.ItemDraw -= View_ItemDraw;
        }

        public void Clear()
        {
            _previousSelection.Clear();
            _currentSelection.Clear();
            _selection.Clear();
            ActiveItem = null;
            _rangeSelectAnchorItem = null;
        }

        public IEnumerable<string> GetPathsInSelection()
        {
            return _currentSelection.Select(view => view.Path);
        }

        public IEnumerator<IEntity> GetEnumerator()
        {
            return _currentSelection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Replace global selection (<see cref="ISelection"/>) with current selection
        /// (<see cref="_currentSelection"/>)
        /// </summary>
        private void SetGlobalSelection()
        {
            var newSelection = _currentSelection
                .ToHashSet(EntityPathEqualityComparer.Default);
            if (newSelection.SetEquals(_selection))
            {
                return;
            }

            _selection.Replace(newSelection);
        }

        #region Range selection

        /// <summary>
        /// Find minimal rectangle which contains both <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="a">Point in rectangle</param>
        /// <param name="b">Point in rectangle</param>
        /// <returns>
        /// Minimal rectangle which contains both <paramref name="a"/> and <paramref name="b"/>
        /// </returns>
        private static Rectangle RectangleFromPoints(Point a, Point b)
        {
            var minX = Math.Min(a.X, b.X);
            var maxX = Math.Max(a.X, b.X);
            var minY = Math.Min(a.Y, b.Y);
            var maxY = Math.Max(a.Y, b.Y);
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private bool HasRangeSelectionChanged(Point location)
        {
            if (!_isRangeSelect)
            {
                return true;
            }

            var oldBounds = RectangleFromPoints(_rangeOrigin, _rangeTarget);
            var newBounds = RectangleFromPoints(_rangeOrigin, location);
            return !_view.ItemLayout.AreSameQueries(oldBounds, newBounds);
        }

        private void ProcessRangeSelection(Point location, bool showRangeSelection)
        {
            _rangeTarget = location;

            if (!_isRangeSelect) // start range selection
            {
                _isRangeSelect = true;
                _rangeOrigin = location;

                // move current selection to the previous selection
                if ((_view.ModifierKeyState & (Keys.Control | Keys.Shift)) == 0)
                {
                    _previousSelection.Clear();
                }
                _previousSelection.UnionWith(_currentSelection);
            }
            else // update range selection
            {
                // calculate range selection bounding rectangle
                var bounds = RectangleFromPoints(location, _rangeOrigin);

                // update selection
                _currentSelection.Clear();

                if (_view.ModifierKeyState.HasFlag(Keys.Control))
                {
                    ProcessSymetricDifferenceSelection(bounds);
                }
                else if (_view.ModifierKeyState.HasFlag(Keys.Shift))
                {
                    ProcessUnionSelection(bounds);
                }
                else
                {
                    ProcessReplaceSelection(bounds);
                }
                
                // update items in the view
                if (showRangeSelection)
                {
                    _view.ShowSelection(bounds);
                }
            }

            SetGlobalSelection();
            _view.UpdateItems();
        }

        /// <summary>
        /// Combine <see cref="_previousSelection"/> and all items in <paramref name="bounds"/>
        /// using the symetric difference strategy.
        /// </summary>
        /// <remarks>
        /// The result is stored in <see cref="_currentSelection"/>. The caller should
        /// clear all items from <see cref="_currentSelection"/> before calling this method.
        /// </remarks>
        /// <param name="bounds">Seletion bounds</param>
        private void ProcessSymetricDifferenceSelection(Rectangle bounds)
        {
            var items = _view.GetItemsIn(bounds).Select(item => item.Data);
            _currentSelection.UnionWith(_previousSelection);
            _currentSelection.SymmetricExceptWith(items);
        }

        /// <summary>
        /// Combine <see cref="_previousSelection"/> and all items in <paramref name="bounds"/>
        /// using the union strategy.
        /// </summary>
        /// <remarks>
        /// The result is stored in <see cref="_currentSelection"/>. The caller should
        /// clear all items from <see cref="_currentSelection"/> before calling this method.
        /// </remarks>
        /// <param name="bounds">Seletion bounds</param>
        private void ProcessUnionSelection(Rectangle bounds)
        {
            var items = _view.GetItemsIn(bounds).Select(item => item.Data);
            _currentSelection.UnionWith(_previousSelection);
            _currentSelection.UnionWith(items);
        }

        /// <summary>
        /// Combine <see cref="_previousSelection"/> and all items in <paramref name="bounds"/>
        /// using the replace strategy.
        /// </summary>
        /// <remarks>
        /// The result is stored in <see cref="_currentSelection"/>. The caller should
        /// clear all items from <see cref="_currentSelection"/> before calling this method.
        /// </remarks>
        /// <param name="bounds">Seletion bounds</param>
        private void ProcessReplaceSelection(Rectangle bounds)
        {
            var items = _view.GetItemsIn(bounds).Select(item => item.Data);
            _currentSelection.UnionWith(items);
        }

        /// <summary>
        /// Finish range selection at <paramref name="location"/>.
        /// </summary>
        /// <param name="location">The last position of current range selection.</param>
        private void EndRangeSelection(Point location)
        {
            ProcessRangeSelection(location, false);
            _isRangeSelect = false;
            _view.HideSelection();
        }

        #endregion

        #region Item selection

        private void ProcessAdditionalItemSelection(EntityView item)
        {
            if (_currentSelection.Contains(item.Data))
            {
                _currentSelection.Remove(item.Data);
            }
            else
            {
                _currentSelection.Add(item.Data);
            }
        }

        private void ProcessRangeItemSelection(EntityView item)
        {
            _currentSelection.Clear();

            var isStart = false;
            var views = _view.Items.SelectMany(pair => pair.Items);
            foreach (var view in views)
            {
                if (!isStart)
                {
                    if (view == item || view == _rangeSelectAnchorItem)
                    {
                        isStart = true;
                        _currentSelection.Add(view.Data);

                        if (item == _rangeSelectAnchorItem)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    _currentSelection.Add(view.Data);

                    if (view == item || view == _rangeSelectAnchorItem)
                    {
                        break;
                    }
                }
            }
        }

        private void ProcessReplaceItemSelection(EntityView item)
        {
            _currentSelection.Clear();
            _currentSelection.Add(item.Data);
        }

        private void ProcessItemSelection(EntityView item, bool resetSelectedItem)
        {
            // update selection
            if (resetSelectedItem)
            {
                if (_view.ModifierKeyState.HasFlag(Keys.Control))
                {
                    ProcessAdditionalItemSelection(item);
                }
                else if (_view.ModifierKeyState.HasFlag(Keys.Shift))
                {
                    ProcessRangeItemSelection(item);
                }
                else 
                {
                    ProcessReplaceItemSelection(item);
                }
            }
            else if (!_currentSelection.Contains(item.Data))
            {
                ProcessReplaceItemSelection(item);
            }

            SetGlobalSelection();

            // update view
            _view.UpdateItems();
        }

        #endregion

        private EntityView GetFirstItem()
        {
            if (_view.Items.Count == 0)
                return null;
            return _view.Items.First().Items.FirstOrDefault();
        }

        private void CaptureAnchorItem(EntityView item)
        {
            RangeSelectAnchorItem = item;
        }

        private void CaptureActiveItem(EntityView item)
        {
            ActiveItem = item;
            ActiveItemChanged?.Invoke(this, EventArgs.Empty);
        }
        
        private void View_ViewActivated(object sender, EventArgs e)
        {
            if (_isRangeSelect)
            {
                _isRangeSelect = false;
                _view.HideSelection();
            }
        }

        private void View_ViewDeactivated(object sender, EventArgs e)
        {
            if (_isRangeSelect)
            {
                _isRangeSelect = false;
                _view.HideSelection();
            }
        }

        private void View_ProcessMouseDown(object sender, MouseEventArgs e)
        {
            var item = _view.GetItemAt(e.Location);
            if (item == null)
            {
                if ((e.Button & MouseButtons.Left) != 0)
                {
                    ProcessRangeSelection(e.Location, true);
                }
            }
            else
            {
                CaptureActiveItem(item);

                if (!_currentSelection.Contains(item.Data) ||
                    _view.ModifierKeyState.HasFlag(Keys.Control) ||
                    _view.ModifierKeyState.HasFlag(Keys.Shift))
                {
                    ProcessItemSelection(item, e.Button.HasFlag(MouseButtons.Left));
                }
            }
        }

        private void View_ProcessMouseMove(object sender, MouseEventArgs e)
        {
            if (_isRangeSelect)
            {
                if (HasRangeSelectionChanged(e.Location))
                {
                    ProcessRangeSelection(e.Location, true);
                }
                else // update only the selection rectangle
                {
                    _view.ShowSelection(RectangleFromPoints(_rangeOrigin, e.Location));
                }
            }
        }

        private void View_ProcessMouseUp(object sender, MouseEventArgs e)
        {
            if (_isRangeSelect)
            {
                EndRangeSelection(e.Location); 
            }
            else 
            {
                var item = _view.GetItemAt(e.Location);
                if (item == null && e.Button == MouseButtons.Right)
                {
                    _currentSelection.Clear();
                    SetGlobalSelection();
                    _view.UpdateItems();
                }

                if (item != null && !_view.ModifierKeyState.HasFlag(Keys.Shift))
                {
                    CaptureAnchorItem(item);
                }

                if (item != null && 
                    _currentSelection.Contains(item.Data) && 
                    _selection.Count() > 1 &&
                    _view.ModifierKeyState == 0)
                {
                    ProcessItemSelection(item, true);
                }
            }
        }

        private void View_HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                _currentSelection.Clear();
                _currentSelection.UnionWith(_view.Items
                    .SelectMany(pair => pair.Items)
                    .Select(item => item.Data));
                SetGlobalSelection();
            }

            // get current active item
            var activeItem = ActiveItem ?? GetFirstItem();
            if (activeItem == null)
            {
                return; // there are no items
            }

            // move current active item
            EntityView target = null;
            switch (e.KeyCode)
            {
                case Keys.Home when _view.Items.Count > 0:
                    target = GetFirstItem();
                    break;
                case Keys.End when _view.Items.Count > 0:
                    target = _view.Items.Last().Items.LastOrDefault();
                    break;
                case Keys.PageUp:
                    target = _view.FindFirstItemAbove(activeItem);
                    break;
                case Keys.PageDown:
                    target = _view.FindLastItemBelow(activeItem);
                    break;
                case Keys.Left:
                    target = _view.FindItem(activeItem, new Point(-1, 0));
                    break;
                case Keys.Right:
                    target = _view.FindItem(activeItem, new Point(1, 0));
                    break;
                case Keys.Up:
                    target = _view.FindItem(activeItem, new Point(0, -1));
                    break;
                case Keys.Down:
                    target = _view.FindItem(activeItem, new Point(0, 1));
                    break;
            }
            
            if (target == null)
            {
                return;
            }
            
            ProcessItemSelection(target, true);
            CaptureActiveItem(target);

            if (!e.Shift)
            {
                CaptureAnchorItem(target);
            }

            _view.EnsureItemVisible(target);
        }
        
        private void View_ItemDraw(object sender, DrawEventArgs e)
        {
            if (_currentSelection.Contains(e.View.Data))
            {
                e.State = EntityViewState.Selected;
            }
        }
    }
}
