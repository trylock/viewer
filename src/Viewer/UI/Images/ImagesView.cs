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

        public void EnsureItemVisible(EntityView item)
        {
            _view.EnsureItemVisible(item);
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
        public event EventHandler<EntityEventArgs> ItemClick;
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
            var item = _view.GetItemAt(location);
            if (item != null)
            {
                ItemClick?.Invoke(sender, new EntityEventArgs(item));

                _isDragging = true;
                _dragOrigin = location;
            }

            ProcessMouseDown?.Invoke(sender, 
                new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta));
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
            
            if (_isDragging)
            {
                const int threshold = BeginDragThreshold * BeginDragThreshold;
                if (_dragOrigin.DistanceSquaredTo(location) > threshold)
                {
                    BeginDragItems?.Invoke(sender, e);
                }
            }
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
            const Keys mask = Keys.Left | Keys.Right | Keys.Up | Keys.Down;
            if ((e.KeyCode & mask) != 0)
            {
                e.IsInputKey = true;
            }

            // make sure to reject shortcut keys used by the context menu
            foreach (var item in ItemContextMenu.Items)
            {
                if (!(item is ToolStripMenuItem menuItem))
                {
                    continue;
                }

                if (menuItem.ShortcutKeys != 0 && // check if this item has a shortcut
                    (e.KeyCode & menuItem.ShortcutKeys) == menuItem.ShortcutKeys)
                {
                    e.IsInputKey = false;
                }
            }
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
