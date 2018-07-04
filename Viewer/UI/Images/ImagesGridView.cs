﻿using System;
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
            get => 1.0 + (ThumbnailSizeTrackBar.Value - ThumbnailSizeTrackBar.Minimum) /
                   (double)(ThumbnailSizeTrackBar.Maximum - ThumbnailSizeTrackBar.Minimum);
            set
            {
                if (value < 1.0 || value > 2.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                ThumbnailSizeTrackBar.Value = (int)MathUtils.Lerp(
                    ThumbnailSizeTrackBar.Minimum,
                    ThumbnailSizeTrackBar.Maximum,
                    value - 1.0
                );
            }
        }

        #endregion

        #region ISelectionView
        
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

        #region IImagesView

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
        public event EventHandler CancelEditItemName;
        public event EventHandler<RenameEventArgs> RenameItem;
        public event EventHandler BeginEditItemName
        {
            add => RenameMenuItem.Click += value;
            remove => RenameMenuItem.Click -= value;
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
            ControlPanel.Visible = Items.Count > 0;
            ItemsCountLabel.Text = string.Format(Resources.ItemCount_Label, Items.Count.ToString("N0"));
            GridView.UpdateItemCount();
        }

        public void UpdateItems()
        {
            UpdateItemCount();
            Refresh();
        }

        public void UpdateItems(IEnumerable<int> itemIndices)
        {
            UpdateItemCount();
            foreach (var index in itemIndices)
            {
                UpdateItem(index);
            }
        }
        
        public void UpdateItem(int index)
        {
            UpdateItemCount();
            GridView.InvalidateItem(index);
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
    }
}
