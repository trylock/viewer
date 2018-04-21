using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Properties;
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

        #region Grid view

        public event MouseEventHandler HandleMouseDown;
        public event MouseEventHandler HandleMouseUp;
        public event MouseEventHandler HandleMouseMove;
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

        public event EventHandler OpenItem;
        public event EventHandler ThumbnailSizeChanged
        {
            add => ThumbnailSizeTrackBar.ValueChanged += value;
            remove => ThumbnailSizeTrackBar.ValueChanged -= value;
        }

        public event EventHandler ThumbnailSizeCommit;
        public event EventHandler CancelEditItemName;
        public event EventHandler<RenameEventArgs> RenameItem;
        public event EventHandler BeginEditItemName
        {
            add => RenameMenuItem.Click += value;
            remove => RenameMenuItem.Click -= value;
        }
        
        public int ThumbnailSize
        {
            get => ThumbnailSizeTrackBar.Value;
            set => ThumbnailSizeTrackBar.Value = value;
        }
        public int ThumbnailSizeMinimum
        {
            get => ThumbnailSizeTrackBar.Minimum;
            set => ThumbnailSizeTrackBar.Minimum = value;
        }
        public int ThumbnailSizeMaximum
        {
            get => ThumbnailSizeTrackBar.Maximum;
            set => ThumbnailSizeTrackBar.Maximum = value;
        }
        public List<EntityView> Items { get; set; }

        public void UpdateItems()
        {
            GridView.Items = Items;
            ItemsCountLabel.Text = string.Format(Resources.ItemCount_Label, Items.Count.ToString("N0"));
        }

        public void UpdateItems(IEnumerable<int> itemIndices)
        {
            foreach (var index in itemIndices)
            {
                UpdateItem(index);
            }
        }
        
        public void UpdateItem(int index)
        {
            GridView.InvalidateItem(index);
        }
        
        public void ShowSelection(Rectangle bounds)
        {
            GridView.SelectionBounds = bounds;
        }

        public void HideSelection()
        {
            GridView.SelectionBounds = Rectangle.Empty;
        }

        public void BeginDragDrop(IDataObject data, DragDropEffects effect)
        {
            DoDragDrop(data, effect);
        }

        public IEnumerable<int> GetItemsIn(Rectangle bounds)
        {
            return GridView.GetItemsIn(bounds);
        }

        public int GetItemAt(Point location)
        {
            return GridView.GetItemAt(location);
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

        public Size ItemSize
        {
            get => GridView.ItemSize;
            set => GridView.ItemSize = value;
        }

        #endregion

        #region GridView Events

        private void GridView_MouseDown(object sender, MouseEventArgs e)
        {
            HandleMouseDown?.Invoke(sender, GridView.ConvertMouseEventArgs(e));
        }

        private void GridView_MouseUp(object sender, MouseEventArgs e)
        {
            HandleMouseUp?.Invoke(sender, GridView.ConvertMouseEventArgs(e));
        }

        private void GridView_MouseMove(object sender, MouseEventArgs e)
        {
            HandleMouseMove?.Invoke(sender, GridView.ConvertMouseEventArgs(e));
        }
        
        private void GridView_DoubleClick(object sender, EventArgs e)
        {
            OpenItem?.Invoke(sender, e);
        }

        private void GridView_Scroll(object sender, ScrollEventArgs e)
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
        }

        private void ThumbnailSizeTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            ThumbnailSizeCommit?.Invoke(sender, e);
        }

        private void GridView_MouseWheel(object sender, MouseEventArgs e)
        {
            CancelEditItemName?.Invoke(sender, e);
        }
    }
}
