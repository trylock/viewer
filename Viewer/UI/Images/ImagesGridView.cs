using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Properties;
using Viewer.Collections;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export(typeof(IImagesView))]
    public partial class ImagesGridView : WindowView, IImagesView
    {
        public ImagesGridView()
        {
            InitializeComponent();

            GridView.MouseWheel += GridView_MouseWheel;
        }

        #region IThumbnailView

        public event EventHandler ThumbnailSizeChanged
        {
            add => ThumbnailSizeTrackBar.ValueChanged += value;
            remove => ThumbnailSizeTrackBar.ValueChanged -= value;
        }

        public event EventHandler ThumbnailSizeCommit;

        public double ThumbnailScale
        {
            get => (ThumbnailSizeTrackBar.Value - ThumbnailSizeTrackBar.Minimum) /
                   (double)(ThumbnailSizeTrackBar.Maximum - ThumbnailSizeTrackBar.Minimum);
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                ThumbnailSizeTrackBar.Value = (int)MathUtils.Lerp(
                    ThumbnailSizeTrackBar.Minimum,
                    ThumbnailSizeTrackBar.Maximum,
                    value
                );
            }
        }

        #endregion

        #region ISelectionView

        private bool _isSelectionActive = false;

        public event MouseEventHandler SelectionBegin;
        public event MouseEventHandler SelectionEnd;
        public event MouseEventHandler SelectionDrag;
        public event EventHandler<EntityEventArgs> SelectItem;

        public void ShowSelection(Rectangle bounds)
        {
            GridView.SelectionBounds = bounds;
        }

        public void HideSelection()
        {
            GridView.SelectionBounds = Rectangle.Empty;
        }
        
        public IEnumerable<int> GetItemsIn(Rectangle bounds)
        {
            return GridView.GetItemsIn(bounds);
        }

        public int GetItemAt(Point location)
        {
            return GridView.GetItemAt(location);
        }

        #endregion

        #region IPolledView

        public event EventHandler Poll;

        public void BeginPolling(int delay)
        {
            if (delay <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delay));
            }

            PollTimer.Interval = delay;
            PollTimer.Enabled = true;
        }

        public void EndPolling()
        {
            PollTimer.Enabled = false;
            Poll?.Invoke(this, EventArgs.Empty);
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            Poll?.Invoke(sender, e);
        }

        #endregion

        #region IImagesView

        /// <summary>
        /// Index of the last item user clicked on with left or right mouse button.
        /// </summary>
        private int _activeItemIndex = -1;
        
        public event KeyEventHandler HandleKeyDown
        {
            add => GridView.KeyDown += value;
            remove => GridView.KeyDown -= value;
        }

        public event KeyEventHandler HandleKeyUp
        {
            add => GridView.KeyUp += value;
            remove => GridView.KeyUp -= value;
        }

        public event EventHandler<EntityEventArgs> ItemHover;

        public event EventHandler CopyItems
        {
            add => CopyMenuItem.Click += value;
            remove => CopyMenuItem.Click -= value;
        }

        public event EventHandler DeleteItems
        {
            add => DeleteMenuItem.Click += value;
            remove => DeleteMenuItem.Click -= value;
        }

        public event EventHandler<EntityEventArgs> OpenItem;
        public event EventHandler ShowCode;
        public event EventHandler CancelEditItemName;
        public event EventHandler BeginDragItems;
        public event EventHandler<RenameEventArgs> RenameItem;
        public event EventHandler<EntityEventArgs> BeginEditItemName;
        
        public string Query { get; set; }

        private IReadOnlyList<IContextOption<IFileView>> _contextOptions;
        public IReadOnlyList<IContextOption<IFileView>> ContextOptions
        {
            get => _contextOptions;
            set
            {
                _contextOptions = value;
                if (_contextOptions == null)
                {
                    return;
                }

                // remove custom items
                var remove = new List<ToolStripItem>();
                foreach (ToolStripItem item in ItemContextMenu.Items)
                {
                    if ((string) item.Tag == "custom")
                    {
                        remove.Add(item);
                    }
                }

                foreach (var item in remove)
                {
                    ItemContextMenu.Items.Remove(item);
                }

                // add new custom items
                foreach (var option in _contextOptions)
                {
                    var optionCapture = option;
                    var item = new ToolStripMenuItem(option.Name)
                    {
                        Tag = "custom",
                    };
                    item.Click += (sender, args) =>
                    {
                        if (_activeItemIndex < 0)
                        {
                            return;
                        }

                        optionCapture.Run(Items[_activeItemIndex]);
                    };
                    ItemContextMenu.Items.Insert(1, item);
                }
            }
        }

        public SortedList<IFileView> Items
        {
            get => GridView.Items;
            set => GridView.Items = value;
        }

        public Size ItemSize
        {
            get => GridView.ItemSize;
            set => GridView.ItemSize = value;
        }

        private void UpdateItemCount()
        {
            ControlPanel.Visible = Items.Count > 0;
            ItemsCountLabel.Text = string.Format(Resources.ItemCount_Label, Items.Count.ToString("N0"));
            GridView.UpdateItemCount();
        }
        
        public void UpdateItems()
        {
            UpdateItemCount();
            Refresh();
        }
        
        public void BeginDragDrop(IDataObject data, DragDropEffects effect)
        {
            DoDragDrop(data, effect);
        }

        public void ShowItemEditForm(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;

            var item = Items[index];
            var cell = GridView.Grid.GetCell(index);

            NameTextBox.Visible = true;
            NameTextBox.Text = item.Name;
            NameTextBox.Location = GridView.ProjectLocation(GridView.GetNameLocation(cell.Bounds));
            NameTextBox.Size = GridView.GetNameSize(cell.Bounds);
            NameTextBox.Focus();
        }

        public void HideItemEditForm()
        {
            NameTextBox.Visible = false;
        }

        #endregion

        #region GridView Events

        private bool _isDragging = false;
        private Point _dragOrigin;

        /// <summary>
        /// Distance in pixels after which we can begin the drag operation
        /// </summary>
        private const int BeginDragThreshold = 10;

        private void GridView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                var location = GridView.UnprojectLocation(e.Location);
                var item = GridView.GetItemAt(location);
                if (item >= 0)
                {
                    // user clicked on an item
                    _activeItemIndex = item;
                    SelectItem?.Invoke(sender, new EntityEventArgs(item));

                    // start dragging
                    _isDragging = true;
                    _dragOrigin = e.Location;
                }
                else
                {
                    // start a range selection
                    _isSelectionActive = true;
                    SelectionBegin?.Invoke(sender,
                        new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta));
                }
            }
        }

        private void GridView_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;

            if (_isSelectionActive)
            {
                var location = GridView.UnprojectLocation(e.Location);
                SelectionEnd?.Invoke(sender, new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta));
                _isSelectionActive = false;
            }
        }

        private void GridView_MouseMove(object sender, MouseEventArgs e)
        {
            var location = GridView.UnprojectLocation(e.Location);
            if (_isSelectionActive)
            {
                SelectionDrag?.Invoke(sender, new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta));
            }
            else
            {
                // trigger the ItemHover event
                var item = GridView.GetItemAt(location);
                if (item < 0)
                {
                    return;
                }
                ItemHover?.Invoke(sender, new EntityEventArgs(item));

                // begin the drag operation
                if (_isDragging)
                {
                    var distanceSquared = e.Location.DistanceSquaredTo(_dragOrigin);
                    if (distanceSquared >= BeginDragThreshold * BeginDragThreshold)
                    {
                        BeginDragItems?.Invoke(sender, e);
                    }
                }
            }
        }

        private void GridView_MouseLeave(object sender, EventArgs e)
        {
            _isDragging = false;
        }

        private void GridView_DoubleClick(object sender, EventArgs e)
        { 
            var location = GridView.UnprojectLocation(PointToClient(MousePosition));
            var index = GridView.GetItemAt(location);
            if (index < 0)
            {
                return;
            }
            OpenItem?.Invoke(sender, new EntityEventArgs(index));
        }

        private void GridView_Scroll(object sender, ScrollEventArgs e)
        {
            CancelEditItemName?.Invoke(sender, e);
        }

        private void GridView_MouseWheel(object sender, MouseEventArgs e)
        {
            CancelEditItemName?.Invoke(sender, e);
        }

        #endregion

        private void NameTextBox_Leave(object sender, EventArgs e)
        {
            CancelEditItemName?.Invoke(sender, e);
        }

        private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                CancelEditItemName?.Invoke(sender, e);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                RenameItem?.Invoke(sender, new RenameEventArgs(_activeItemIndex, NameTextBox.Text));
            }
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            if (_activeItemIndex < 0)
            {
                return;
            }

            OpenItem?.Invoke(sender, new EntityEventArgs(_activeItemIndex));
        }

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            NameTextBox.BringToFront();
            BeginEditItemName?.Invoke(sender, new EntityEventArgs(_activeItemIndex));
        }

        private void ThumbnailSizeTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            ThumbnailSizeCommit?.Invoke(sender, e);
        }

        private void ThumbnailSizeTrackBar_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                ThumbnailSizeCommit?.Invoke(sender, e);
            }
        }

        private void ShowQueryButton_Click(object sender, EventArgs e)
        {
            ShowCode?.Invoke(sender, e);
        }

        protected override string GetPersistString()
        {
            if (Query != null)
            {
                return base.GetPersistString() + ";" + Query;
            }
            return base.GetPersistString();
        }

        public override void BeginLoading()
        {
            GridView.IsLoading = true;
        }

        public override void EndLoading()
        {
            GridView.IsLoading = false;
        }
    }
}
