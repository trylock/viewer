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
using Viewer.Localization;
using Viewer.UI.Forms;
using Viewer.UI.Images.Layout;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    internal partial class ImagesView : WindowView, IImagesView
    {
        private ThumbnailGridView _view;
        
        public ImagesView()
        {
            InitializeComponent();
            
            RegisterView(_thumbnailGridView);
            _view = _thumbnailGridView;

            ViewerForm.Theme.ApplyTo(PickDirectoryContextMenu);
        }

        private void RegisterView(ThumbnailGridView view)
        {
            view.DragDrop += GridView_DragDrop;
            view.DragOver += GridView_DragOver;
            view.DoubleClick += GridView_DoubleClick;
            view.MouseDown += GridView_MouseDown;
            view.MouseLeave += GridView_MouseLeave;
            view.MouseMove += GridView_MouseMove;
            view.MouseUp += GridView_MouseUp;
            view.MouseWheel += GridView_MouseWheel;
            view.KeyDown += GridView_KeyDown;
            view.KeyUp += GridView_KeyUp;
            view.Resize += GridView_Resize;
            view.PreviewKeyDown += GridView_PreviewKeyDown;
            view.GroupLabelControl.MouseDown += GroupLabel_MouseDown;

            Controls.Add(view.GroupLabelControl);
            view.GroupLabelControl.Location = view.Location;
            view.GroupLabelControl.BringToFront();
        }

        #region ISelectionView
        
        public event MouseEventHandler ProcessMouseUp;
        public event MouseEventHandler ProcessMouseDown;
        public event MouseEventHandler ProcessMouseMove;
        public event EventHandler ProcessMouseLeave;

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

        public EntityView FindItem(EntityView currentItem, Point delta)
        {
            return _view.FindItem(currentItem, delta);
        }

        public EntityView FindFirstItemAbove(EntityView currentItem)
        {
            return _view.FindFirstItemAbove(currentItem);
        }

        public EntityView FindLastItemBelow(EntityView currentItem)
        {
            return _view.FindLastItemBelow(currentItem);
        }

        public void EnsureItemVisible(EntityView item)
        {
            _view.EnsureItemVisible(item);
        }

        public Group GetCurrentGroup()
        {
            var location = _view.PointToClient(MousePosition);
            var element = _view.ControlLayout.GetGroupAt(_view.UnprojectLocation(location));
            return element?.Item;
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
        
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IHistoryView History => HistoryView;

        public event KeyEventHandler HandleKeyDown;

        public event KeyEventHandler HandleKeyUp;

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
        public event EventHandler CancelEditItemName;
        public event EventHandler BeginDragItems;
        public event EventHandler BeginEditItemName;
        public event EventHandler OpenItem;
        public event EventHandler<RenameEventArgs> RenameItem;
        public event EventHandler<ProgramEventArgs> RunProgram;
        
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
                        item.Image?.Dispose();
                        item.Image = null;
                        remove.Add(item);
                    }
                }

                foreach (var item in remove)
                {
                    ItemContextMenu.Items.Remove(item);
                }

                // add new custom items
                foreach (var option in _contextOptions.Reverse())
                {
                    var optionCapture = option;
                    var item = new ToolStripMenuItem(option.Name)
                    {
                        Image = optionCapture.GetImage(),
                        Tag = "custom",
                    };
                    item.Click += (sender, args) =>
                    {
                        RunProgram?.Invoke(this, 
                            new ProgramEventArgs(optionCapture));
                    };
                    ItemContextMenu.Items.Insert(2, item);
                }
            }
        }

        public List<Group> Items
        {
            get => _view.Items;
            set
            {
                _view.Items = value;
                var itemCount = value?.Sum(group => group.Items.Count) ?? 0;
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
            Invalidate();
        }
        
        public void BeginDragDrop(IDataObject data, DragDropEffects effect)
        {
            DoDragDrop(data, effect);
        }

        public void ShowItemEditForm(EntityView entityView)
        {
            if (entityView == null)
                throw new ArgumentNullException(nameof(entityView));
            
            var bounds = _view.GetNameBounds(entityView);

            NameTextBox.Visible = true;
            NameTextBox.Text = entityView.Name;
            NameTextBox.Location = GridViewToControl(_view.ProjectLocation(bounds.Location));
            NameTextBox.Size = bounds.Size;
            NameTextBox.Focus();
        }

        public void HideItemEditForm()
        {
            NameTextBox.Visible = false;
        }

        #endregion

        #region GridView Events
        
        /// <summary>
        /// Distance in pixels after which we can begin the drag operation
        /// </summary>
        private const int BeginDragThreshold = 20;

        private Point ControlToGridView(Point location)
        {
            return new Point(
                location.X - _view.Location.X,
                location.Y - _view.Location.Y
            );
        }

        private Point GridViewToControl(Point location)
        {
            return new Point(
                location.X + _view.Location.X,
                location.Y + _view.Location.Y
            );
        }
        
        private bool _isDragging;
        private Point _dragOrigin;
        
        private void GridView_MouseDown(object sender, MouseEventArgs e)
        {
            // process history events
            if (e.Button.HasFlag(MouseButtons.XButton1))
            {
                HistoryView.GoBack();
            }
            else if (e.Button.HasFlag(MouseButtons.XButton2))
            {
                HistoryView.GoForward();
            }

            var location = _view.UnprojectLocation(e.Location);

            // if we have clicked on a group label
            var element = _view.ControlLayout.GetGroupLabelAt(location);
            if (element != null)
            {
                _view.ControlLayout.ToggleCollapse(element.Item);
                _view.UpdateItems();
                return;
            }

            // if we have clicked on an item
            var item = _view.GetItemAt(location);
            if (item != null)
            {
                _isDragging = true;
                _dragOrigin = location;
            }

            ProcessMouseDown?.Invoke(sender, 
                new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta));
        }
        
        private void GroupLabel_MouseDown(object sender, MouseEventArgs e)
        {
            var element = _view.ControlLayout.GetGroupAt(_view.UnprojectLocation(Point.Empty));
            if (element == null)
            {
                return;
            }
            _view.ControlLayout.ToggleCollapse(element.Item);
            _view.EnsureGroupVisible(element.Item);
            _view.UpdateItems();
        }

        private void GridView_MouseUp(object sender, MouseEventArgs e)
        {
            var location = _view.UnprojectLocation(e.Location);
            ProcessMouseUp?.Invoke(sender,
                new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta));
            
            _isDragging = false;
        }
        
        private void GridView_MouseMove(object sender, MouseEventArgs e)
        {
            var location = _view.UnprojectLocation(e.Location);
            ProcessMouseMove?.Invoke(sender,
                new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta));

            // update group labels around mouse cursor
            var element = _view.ControlLayout.GetGroupLabelAt(location);
            if (element != null)
            {
                var bounds = element.Bounds;
                bounds.Inflate(
                    _view.ControlLayout.GroupLabelSize.Width * 2,
                    _view.ControlLayout.GroupLabelSize.Height * 2);
                _view.Invalidate(_view.ProjectBounds(bounds));
            }

            if (_isDragging)
            {
                const int threshold = BeginDragThreshold * BeginDragThreshold;
                if (_dragOrigin.DistanceSquaredTo(location) > threshold)
                {
                    BeginDragItems?.Invoke(sender, e);
                    _isDragging = false;
                }
            }

            UpdateScrollPosition();
        }

        private void GridView_MouseLeave(object sender, EventArgs e)
        {
            ProcessMouseLeave?.Invoke(sender, e);
            _isDragging = false;
        }

        private void GridView_DoubleClick(object sender, EventArgs e)
        { 
            var location = _view.UnprojectLocation(ControlToGridView(PointToClient(MousePosition)));
            var item = _view.GetItemAt(location);
            if (item == null)
            {
                return;
            }

            OpenItem?.Invoke(sender, e);
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
            var location = _view.UnprojectLocation(ControlToGridView(PointToClient(MousePosition)));
            var item = _view.GetItemAt(location);
            OnDrop?.Invoke(sender, new DropEventArgs(
                item?.Data is DirectoryEntity ? item : null,
                e.AllowedEffect, 
                e.Data));
        }

        private void GridView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = !e.Alt && (
                               e.KeyCode == Keys.Left ||
                               e.KeyCode == Keys.Right ||
                               e.KeyCode == Keys.Up ||
                               e.KeyCode == Keys.Down);
        }
        
        private void GridView_KeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyDown?.Invoke(sender, e);

            if (e.KeyCode == Keys.Enter)
            {
                OpenItem?.Invoke(sender, e);
            }
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
                RenameItem?.Invoke(sender, new RenameEventArgs(NameTextBox.Text));
            }
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            OpenItem?.Invoke(sender, e);
        }

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            NameTextBox.BringToFront();
            BeginEditItemName?.Invoke(sender, e);
        }

        private void PasteMenuItem_Click(object sender, EventArgs e)
        {
            OnPaste?.Invoke(sender, e);
        }

        private void PreviousMenuItem_Click(object sender, EventArgs e)
        {
            HistoryView.GoBack();
        }

        private void NextMenuItem_Click(object sender, EventArgs e)
        {
            HistoryView.GoForward();
        }

        private void UpMenuItem_Click(object sender, EventArgs e)
        {
            HistoryView.GoToParent();
        }

        private void ShowQueryMenuItem_Click(object sender, EventArgs e)
        {
            ShowQuery?.Invoke(sender, e);
        }

        private void MoveTimer_Tick(object sender, EventArgs e)
        {
            UpdateScrollPosition();
        }

        private DateTime _lastUpdate = DateTime.Now;

        /// <summary>
        /// Scroll the thumbnail grid if a range selection is active and mouse cursor is outside
        /// of the control area.
        /// </summary>
        private void UpdateScrollPosition()
        {
            // update the position at most ~60 times per second
            var deltaTime = DateTime.Now - _lastUpdate;
            if (deltaTime.TotalMilliseconds < 16)
            {
                return;
            }
            _lastUpdate = DateTime.Now;

            // check if a range selection is active and whether the mouse cursor is outside
            var mouseLocation = _view.PointToClient(MousePosition);
            if (_view.SelectionBounds == Rectangle.Empty ||
                (mouseLocation.Y >= 0 && mouseLocation.Y <= _view.ClientSize.Height))
            {
                return;
            }

            // normalize distance to [0, 1]
            double distance = mouseLocation.Y < 0 ? 
                -mouseLocation.Y : 
                mouseLocation.Y - _view.ClientSize.Height;
            var maxDistance = Font.Height * 10;
            distance = distance.Clamp(0, maxDistance) / maxDistance;

            // update scroll position
            double speed = Math.Sign(mouseLocation.Y) * deltaTime.TotalMilliseconds;
            speed *= MathUtils.Lerp(0.1, 10, distance * distance);
            _thumbnailGridView.AutoScrollPosition = new Point(0, 
                -_thumbnailGridView.AutoScrollPosition.Y + (int) speed);

            // trigger an artificial MouseMove event to force the selection to update
            var uiCoords = _view.UnprojectLocation(mouseLocation);
            ProcessMouseMove?.Invoke(this,
                new MouseEventArgs(MouseButtons.Left, 0, uiCoords.X, uiCoords.Y, 0));
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
            StatusLabel.Text = Strings.Loading_Label;
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
            StatusLabel.Text = Strings.Empty_Label;
        }
    }
}
