using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Properties;
using Viewer.Core.Collections;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.UI.Forms;
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

        #region IHistoryView

        public event EventHandler GoBackInHistory;

        public event EventHandler GoForwardInHistory;

        #endregion
        
        #region ISelectionView

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
        
        public IEnumerable<EntityView> GetItemsIn(Rectangle bounds)
        {
            return GridView.GetItemsIn(bounds);
        }

        public EntityView GetItemAt(Point location)
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
        /// Index of the last item user clicked on with left mouse button.
        /// </summary>
        private EntityView _activeItem = null;
        
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

        public event EventHandler CutItems
        {
            add => CutMenuItem.Click += value;
            remove => CutMenuItem.Click -= value;
        }

        public event EventHandler DeleteItems
        {
            add => DeleteMenuItem.Click += value;
            remove => DeleteMenuItem.Click -= value;
        }

        public event EventHandler<EntityEventArgs> OpenItem;
        public event EventHandler<DropEventArgs> OnDrop;
        public event EventHandler CancelEditItemName;
        public event EventHandler BeginDragItems;
        public event EventHandler<RenameEventArgs> RenameItem;
        public event EventHandler<EntityEventArgs> BeginEditItemName;
        
        public string Query { get; set; }

        private IReadOnlyList<ExternalApplication> _contextOptions;
        public IReadOnlyList<ExternalApplication> ContextOptions
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
                        if (_activeItem == null)
                        {
                            return;
                        }

                        optionCapture.Run(_activeItem.FullPath);
                    };
                    ItemContextMenu.Items.Insert(1, item);
                }
            }
        }

        public SortedList<EntityView> Items
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

        public void ShowItemEditForm(EntityView entityView)
        {
            if (entityView == null)
                throw new ArgumentNullException(nameof(entityView));

            var index = Items.IndexOf(entityView);
            var cell = GridView.Grid.GetCell(index);

            NameTextBox.Visible = true;
            NameTextBox.Text = entityView.Name;
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
        
        // internal state changed by GridView events 
        private bool _selectItemTriggered = false;
        private bool _isSelectionActive = false;
        private bool _isDragging = false;
        private Point _dragOrigin;

        /// <summary>
        /// Distance in pixels after which we can begin the drag operation
        /// </summary>
        private const int BeginDragThreshold = 10;
        
        private void GridView_MouseDown(object sender, MouseEventArgs e)
        {
            // process history events
            if (e.Button.HasFlag(MouseButtons.XButton1))
            {
                GoBackInHistory?.Invoke(sender, e);
            }
            else if (e.Button.HasFlag(MouseButtons.XButton2))
            {
                GoForwardInHistory?.Invoke(sender, e);
            }

            // process events on grid view item
            var location = GridView.UnprojectLocation(e.Location);
            var item = GridView.GetItemAt(location);
            if (item != null)
            {
                // set last item user clicked on using any button
                _activeItem = item;
                
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    if (item.State != FileViewState.Selected)
                    {
                        SelectItem?.Invoke(sender, new EntityEventArgs(item));
                        _selectItemTriggered = true;
                    }

                    // the MouseMove event will determine whether we will select this item
                    // on MouseUp or drag the whole selection on MouseMove
                    _isDragging = true;
                    _dragOrigin = location;
                }
                else if (e.Button.HasFlag(MouseButtons.Right))
                {
                    if (item.State != FileViewState.Selected)
                    {
                        SelectItem?.Invoke(sender, new EntityEventArgs(item));
                    }
                    else
                    {
                        // just open context menu for the whole selection
                    }
                }
            }
            else // there is no item at current mouse position
            {
                if (e.Button.HasFlag(MouseButtons.Left))
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
            // finish selection
            if (_isSelectionActive)
            {
                var location = GridView.UnprojectLocation(e.Location);
                SelectionEnd?.Invoke(sender, new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta));
                _isSelectionActive = false;
            }
            else if (e.Button.HasFlag(MouseButtons.Left))
            {
                // select item
                if (_activeItem != null && !_selectItemTriggered)
                {
                    SelectItem?.Invoke(sender, new EntityEventArgs(_activeItem));
                }
            }

            // reset state
            _isDragging = false;
            _selectItemTriggered = false;
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
                if (item == null)
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
            var item = GridView.GetItemAt(location);
            if (item == null)
            {
                return;
            }
            
            OpenItem?.Invoke(sender, new EntityEventArgs(item));
        }

        private void GridView_Scroll(object sender, ScrollEventArgs e)
        {
            CancelEditItemName?.Invoke(sender, e);
        }

        private void GridView_MouseWheel(object sender, MouseEventArgs e)
        {
            CancelEditItemName?.Invoke(sender, e);
        }

        private void GridView_DragOver(object sender, DragEventArgs e)
        {
            var location = GridView.UnprojectLocation(PointToClient(MousePosition));
            var item = GridView.GetItemAt(location);
            e.Effect = item?.Data is DirectoryEntity ? 
                DragDropEffects.Move : 
                DragDropEffects.None;
        }

        private void GridView_DragDrop(object sender, DragEventArgs e)
        {
            var location = GridView.UnprojectLocation(PointToClient(MousePosition));
            var item = GridView.GetItemAt(location);
            if (item?.Data is DirectoryEntity)
            {
                OnDrop?.Invoke(sender, new DropEventArgs(item, e.AllowedEffect, e.Data));
            }
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
                RenameItem?.Invoke(sender, new RenameEventArgs(_activeItem, NameTextBox.Text));
            }
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            if (_activeItem == null)
            {
                return;
            }
            
            OpenItem?.Invoke(sender, new EntityEventArgs(_activeItem));
        }

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            if (_activeItem == null)
            {
                return;
            }
            
            NameTextBox.BringToFront();
            BeginEditItemName?.Invoke(sender, new EntityEventArgs(_activeItem));
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
