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
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.UI.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    internal partial class ImagesView : WindowView, IImagesView
    {
        private GridView _view;

        public ImagesView()
        {
            InitializeComponent();

            PreviousMenuItem.ShortcutKeyDisplayString = "Alt + Left, MB4";
            NextMenuItem.ShortcutKeyDisplayString = "Alt + Right, MB5";
            
            RegisterView(GridView);
            _view = GridView;

            ViewerForm.Theme.ApplyTo(PickDirectoryContextMenu);
        }

        private void RegisterView(Control view)
        {
            view.DragDrop += GridView_DragDrop;
            view.DragOver += GridView_DragOver;
            view.Click += GridView_Click;
            view.DoubleClick += GridView_DoubleClick;
            view.MouseDown += GridView_MouseDown;
            view.MouseLeave += GridView_MouseLeave;
            view.MouseMove += GridView_MouseMove;
            view.MouseUp += GridView_MouseUp;
            view.MouseWheel += GridView_MouseWheel;
            view.KeyDown += GridView_KeyDown;
            view.KeyUp += GridView_KeyUp;
            view.Resize += GridView_Resize;
        }

        #region IHistoryView

        public bool CanGoForwardInHistory
        {
            get => NextMenuItem.Enabled;
            set => NextMenuItem.Enabled = value;
        }

        public bool CanGoBackInHistory
        {
            get => PreviousMenuItem.Enabled;
            set => PreviousMenuItem.Enabled = value;
        }

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
            _view.SelectionBounds = bounds;
        }

        public void HideSelection()
        {
            _view.SelectionBounds = Rectangle.Empty;
        }
        
        public IEnumerable<EntityView> GetItemsIn(Rectangle bounds)
        {
            return _view.GetItemsIn(bounds);
        }

        public EntityView GetItemAt(Point location)
        {
            return _view.GetItemAt(location);
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

        #region IFileDropView
        
        public event EventHandler<DropEventArgs> OnDrop;
        public event EventHandler OnPaste;

        public Task<string> PickDirectoryAsync(IEnumerable<string> options)
        {
            var promise = new TaskCompletionSource<string>();

            // make sure the root form is fully visible
            Form form = this;
            while (form.ParentForm != null)
            {
                form = form.ParentForm;
            }
            form.Activate();

            // pick an option form context menu
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                PickDirectoryContextMenu.Items.Clear();
                foreach (var option in options)
                {
                    var optionCapture = option;
                    var item = new ToolStripMenuItem(option);
                    item.Click += (sender, args) => { promise.TrySetResult(optionCapture); };
                    PickDirectoryContextMenu.Items.Add(item);
                }

                PickDirectoryContextMenu.Closed += (sender, args) =>
                {
                    if (args.CloseReason != ToolStripDropDownCloseReason.ItemClicked)
                    {
                        promise.TrySetCanceled();
                    }
                };
                PickDirectoryContextMenu.Show(Cursor.Position);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            return promise.Task;
        }

        #endregion 

        #region IImagesView

        /// <summary>
        /// Index of the last item user clicked on with left mouse button.
        /// </summary>
        private EntityView _activeItem = null;

        public event KeyEventHandler HandleKeyDown;

        public event KeyEventHandler HandleKeyUp;

        public event EventHandler<EntityEventArgs> ItemHover;
        public event EventHandler<EntityEventArgs> ItemClick;

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

        public event EventHandler RefreshQuery
        {
            add => RefreshMenuItem.Click += value;
            remove => RefreshMenuItem.Click -= value;
        }

        public event EventHandler ShowQuery;
        public event EventHandler GoUp;
        public event EventHandler<EntityEventArgs> OpenItem;
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

        public List<EntityView> Items
        {
            get => _view.Items;
            set
            {
                _view.Items = value;
                var itemCount = value?.Count ?? 0;
                StatusLabel.Visible = itemCount <= 0;
            }
        }

        public Size ItemSize
        {
            get => _view.ItemSize;
            set => _view.ItemSize = value;
        }
        
        public void UpdateItems()
        {
            _view.UpdateItems();
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
            var bounds = _view.GetNameBounds(index);

            NameTextBox.Visible = true;
            NameTextBox.Text = entityView.Name;
            NameTextBox.Location = _view.ProjectLocation(bounds.Location);
            NameTextBox.Size = bounds.Size;
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
        private const int BeginDragThreshold = 20;
        
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
            var location = _view.UnprojectLocation(e.Location);
            var item = _view.GetItemAt(location);
            if (item != null)
            {
                // set last item user clicked on using any button
                _activeItem = item;
                
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    if (item.State != EntityViewState.Selected)
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
                    if (item.State != EntityViewState.Selected)
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
                var location = _view.UnprojectLocation(e.Location);
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
            var location = _view.UnprojectLocation(e.Location);
            if (_isSelectionActive)
            {
                SelectionDrag?.Invoke(sender, new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta));

                // scroll up, down if we are outside of the control area
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    var delta = 0;
                    if (e.Location.Y < 0)
                    {
                        // scroll up
                        delta = 10;
                    }
                    else if (e.Location.Y > ClientSize.Height)
                    {
                        // scroll down
                        delta = -10;
                    }
                    
                    if (delta != 0)
                    {
                        _view.AutoScrollPosition = new Point(0, -_view.AutoScrollPosition.Y - delta);
                    }
                }
            }
            else
            {
                // trigger the ItemHover event
                var item = _view.GetItemAt(location);
                if (item == null)
                {
                    return;
                }
                
                ItemHover?.Invoke(sender, new EntityEventArgs(item));

                // begin the drag operation
                if (_isDragging)
                {
                    var distanceSquared = location.DistanceSquaredTo(_dragOrigin);
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
        
        private void GridView_Click(object sender, EventArgs e)
        {
            var location = _view.UnprojectLocation(PointToClient(MousePosition));
            var item = _view.GetItemAt(location);
            if (item == null)
            {
                return;
            }

            ItemClick?.Invoke(sender, new EntityEventArgs(item));
        }

        private void GridView_DoubleClick(object sender, EventArgs e)
        { 
            var location = _view.UnprojectLocation(PointToClient(MousePosition));
            var item = _view.GetItemAt(location);
            if (item == null)
            {
                return;
            }
            
            OpenItem?.Invoke(sender, new EntityEventArgs(item));
        }
        
        private void GridView_MouseWheel(object sender, MouseEventArgs e)
        {
            CancelEditItemName?.Invoke(sender, e);
        }

        private void GridView_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void GridView_DragDrop(object sender, DragEventArgs e)
        {
            var location = _view.UnprojectLocation(PointToClient(MousePosition));
            var item = _view.GetItemAt(location);
            OnDrop?.Invoke(sender, new DropEventArgs(
                item?.Data is DirectoryEntity ? item : null,
                e.AllowedEffect, 
                e.Data));
        }

        private void GridView_KeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyDown?.Invoke(sender, e);
        }

        private void GridView_KeyUp(object sender, KeyEventArgs e)
        {
            HandleKeyUp?.Invoke(sender, e);
        }

        private void GridView_Resize(object sender, EventArgs e)
        {
            StatusLabel.Location = new Point(
                ClientSize.Width / 2 - StatusLabel.Width / 2,
                ClientSize.Height / 2 - StatusLabel.Height / 2
            );
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

        private void PasteMenuItem_Click(object sender, EventArgs e)
        {
            OnPaste?.Invoke(sender, e);
        }

        private void PreviousMenuItem_Click(object sender, EventArgs e)
        {
            GoBackInHistory?.Invoke(sender, e);
        }

        private void NextMenuItem_Click(object sender, EventArgs e)
        {
            GoForwardInHistory?.Invoke(sender, e);
        }

        private void UpMenuItem_Click(object sender, EventArgs e)
        {
            GoUp?.Invoke(sender, e);
        }

        private void ShowQueryMenuItem_Click(object sender, EventArgs e)
        {
            ShowQuery?.Invoke(sender, e);
        }

        protected override string GetPersistString()
        {
            if (Query != null)
            {
                return base.GetPersistString() + ";" + Query;
            }
            return base.GetPersistString();
        }

        private int _loadingCount = 0;

        public override void BeginLoading()
        {
            StatusLabel.Text = Resources.Loading_Label;
            ++_loadingCount;
        }

        public override void EndLoading()
        {
            --_loadingCount;
            if (_loadingCount > 0)
            {
                return;
            }
            var itemCount = Items?.Count ?? 0;
            StatusLabel.Visible = itemCount <= 0;
            StatusLabel.Text = Resources.Empty_Label;
        }
    }
}
