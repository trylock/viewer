using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    public partial class GridControl : WindowView, IImagesView
    {
        #region Graphics settings

        private readonly Brush _highlightedItemFill;
        private readonly Pen _highlightedItemStroke;
        private readonly int _highlightedItemBorderSize = 1;

        private readonly Brush _selectedItemFill;
        private readonly Pen _selectedItemStroke;
        private readonly int _selectedItemBorderSize = 1;

        private readonly Brush _rangeSelectionFill;
        private readonly Pen _rangeSelectionStroke;

        #endregion
        
        /// <summary>
        /// Height of the area for file name
        /// </summary>
        public int NameHeight { get; set; } = 25;

        private Grid _grid = new Grid();
        private Rectangle _selection = Rectangle.Empty;

        public GridControl(string name)
        {
            InitializeComponent();

            Text = name;

            _grid = new Grid();
            _grid.CellMargin = new Size(5, 5);

            _highlightedItemFill = new SolidBrush(Color.FromArgb(226, 241, 255));
            _highlightedItemStroke = new Pen(Color.FromArgb(221, 232, 248), _highlightedItemBorderSize);

            _selectedItemFill = new SolidBrush(Color.FromArgb(221, 232, 248));
            _selectedItemStroke = new Pen(Color.FromArgb(210, 220, 236), _selectedItemBorderSize);

            _rangeSelectionFill = new SolidBrush(Color.FromArgb(150, 79, 143, 247));
            _rangeSelectionStroke = new Pen(Color.FromArgb(255, 79, 143, 247));
        }

        #region Grid view

        public event MouseEventHandler HandleMouseDown;
        public event MouseEventHandler HandleMouseUp;
        public event MouseEventHandler HandleMouseMove;
        public event KeyEventHandler HandleKeyDown
        {
            add => KeyDown += value;
            remove => KeyUp -= value;
        }
        public event KeyEventHandler HandleKeyUp
        {
            add => KeyUp += value;
            remove => KeyUp -= value;
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
        public event EventHandler CancelEditItemName;
        public event EventHandler<RenameEventArgs> RenameItem;
        public event EventHandler BeginEditItemName
        {
            add => RenameMenuItem.Click += value;
            remove => RenameMenuItem.Click -= value;
        }

        public Size ItemSize { get; set; } = new Size(150, 100);
        public Size ItemPadding { get; set; } = new Size(8, 8);
        public List<EntityView> Items { get; set; }

        public void UpdateItems()
        {
            _grid.CellCount = Items.Count;
            UpdateSize();
            Refresh();
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
            if (index < 0 || index >= Items.Count)
                return;
            var cell = _grid.GetCell(index);
            var bounds = cell.Bounds;
            Invalidate(ProjectBounds(bounds));
        }
        
        public void ShowSelection(Rectangle bounds)
        {
            Invalidate(ProjectBounds(_selection));
            _selection = bounds;
            Invalidate(ProjectBounds(_selection));
        }

        public void HideSelection()
        {
            Invalidate(ProjectBounds(_selection));
            _selection = Rectangle.Empty;
        }

        public void BeginDragDrop(IDataObject data, DragDropEffects effect)
        {
            DoDragDrop(data, effect);
        }

        public IEnumerable<int> GetItemsIn(Rectangle bounds)
        {
            return _grid.GetCellsInBounds(bounds).Select(cell => cell.Index);
        }

        public int GetItemAt(Point location)
        {
            return _grid.GetCellAt(location).Index;
        }

        public void ShowItemEditForm(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;

            var item = Items[index];
            var cell = _grid.GetCell(index);

            NameTextBox.Visible = true;
            NameTextBox.Text = item.Name;
            NameTextBox.Location = ProjectLocation(GetNameLocation(cell.Bounds));
            NameTextBox.Size = GetNameSize(cell.Bounds);
            NameTextBox.Focus();
        }

        public void HideItemEditForm()
        {
            NameTextBox.Visible = false;
        }
        
        public void UpdateSize()
        {
            // update grid size
            _grid.Resize(ClientSize.Width);
            _grid.MinCellWidth = ItemSize.Width + ItemPadding.Width * 2;
            _grid.CellHeight = ItemSize.Height + NameHeight + ItemPadding.Height * 2;

            // update scroll size
            AutoScrollMinSize = new Size(
                0, // we don't want to have a horizontal scroll bar
                _grid.GridSize.Height
            );

            Refresh();
        }

        #endregion

        #region Utility conversion functions

        /// <summary>
        /// Project UI location to clip area coordinates
        /// </summary>
        /// <param name="uiLocation">UI location</param>
        /// <returns>
        ///     Location in clip area:
        ///     [0, 0] is the top left corner of the visible area,
        ///     [ClientSize.Width, ClientSize.Height] is the bottom right corner of the visible area
        /// </returns>
        public Point ProjectLocation(Point uiLocation)
        {
            return new Point(
                uiLocation.X + AutoScrollPosition.X,
                uiLocation.Y + AutoScrollPosition.Y);
        }

        /// <summary>
        /// Compute inverse of ProjectLocation
        /// </summary>
        /// <param name="clipAreaLocation">Point in clip area coordinates</param>
        /// <returns>Point in UI coordinates.</returns>
        public Point UnprojectLocation(Point clipAreaLocation)
        {
            return new Point(
                clipAreaLocation.X - AutoScrollPosition.X,
                clipAreaLocation.Y - AutoScrollPosition.Y);
        }

        /// <summary>
        /// Project rectange in UI coordinates to clip coordinates
        /// </summary>
        /// <param name="uiBounds">Rectangle in UI coordinates</param>
        /// <returns>Rectangle in clip coordinates</returns>
        public Rectangle ProjectBounds(Rectangle uiBounds)
        {
            return new Rectangle(ProjectLocation(uiBounds.Location), uiBounds.Size);
        }

        /// <summary>
        /// Inverse of ProjectBounds.
        /// </summary>
        /// <param name="clipBounds">Rectangle in clip coordinates</param>
        /// <returns>Rectangle in UI coordinates</returns>
        public Rectangle UnprojectBounds(Rectangle clipBounds)
        {
            return new Rectangle(UnprojectLocation(clipBounds.Location), clipBounds.Size);
        }

        private MouseEventArgs ConvertMouseEventArgs(MouseEventArgs e)
        {
            var location = UnprojectLocation(e.Location);
            return new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta);
        }

        #endregion

        private Point GetThumbnailLocation(Rectangle cellBounds, Size thumbnailSize)
        {
            return new Point(
                // align the thumbnail to the center horizontally
                cellBounds.Location.X + (cellBounds.Width - thumbnailSize.Width) / 2,
                // align the thumbnail to the bottom vertically
                cellBounds.Location.Y + (cellBounds.Height - NameHeight - thumbnailSize.Height) / 2
            );
        }

        private Point GetNameLocation(Rectangle cellBounds)
        {
            return new Point(
                cellBounds.X + ItemPadding.Width,
                cellBounds.Y + (cellBounds.Height - NameHeight) - ItemPadding.Height
            );
        }

        private Size GetNameSize(Rectangle cellBounds)
        {
            return new Size(
                cellBounds.Width - 2 * ItemPadding.Width,
                NameHeight
            );
        }

        #region GridControl Events

        private void GridControl_Paint(object sender, PaintEventArgs e)
        {
            // update invalid items
            var clipBounds = UnprojectBounds(e.ClipRectangle);
            var cells = _grid.GetCellsInBounds(clipBounds);
            foreach (var cell in cells)
            {
                PaintItem(e.Graphics, cell);
            }

            // update selection 
            if (!_selection.IsEmpty)
            {
                var selectionBounds = ProjectBounds(_selection);
                e.Graphics.FillRectangle(_rangeSelectionFill, selectionBounds.Shrink(1));
                e.Graphics.DrawRectangle(_rangeSelectionStroke, selectionBounds.Shrink(1));
            }
        }

        private void PaintItem(Graphics graphics, GridCell cell)
        {
            // find thumbnail
            var item = Items[cell.Index];
            var bounds = ProjectBounds(cell.Bounds);

            // clear grid cell area
            graphics.FillRectangle(Brushes.White, bounds);
            
            if ((item.State & ResultItemState.Selected) != 0)
            {
                // draw selection 
                graphics.FillRectangle(_selectedItemFill, bounds.Shrink(_selectedItemBorderSize));
                graphics.DrawRectangle(_selectedItemStroke, bounds.Shrink(_selectedItemBorderSize));
            }
            else if ((item.State & ResultItemState.Active) != 0)
            {
                // draw highlight
                graphics.FillRectangle(_highlightedItemFill, bounds.Shrink(_highlightedItemBorderSize));
                graphics.DrawRectangle(_highlightedItemStroke, bounds.Shrink(_highlightedItemBorderSize));
            }

            // draw the thumbnail
            var thumbnailSize = item.Thumbnail.Size;
            var thumbnailLocation = GetThumbnailLocation(bounds, thumbnailSize);
            // we don't really need an interpolation as we are drawing the image in its original size
            graphics.InterpolationMode = InterpolationMode.Low;
            graphics.DrawImage(item.Thumbnail, new Rectangle(thumbnailLocation, thumbnailSize));

            // draw name
            var nameLocation = GetNameLocation(bounds);
            var nameSize = GetNameSize(bounds);
            var nameForamt = new StringFormat { Alignment = StringAlignment.Center };
            graphics.DrawString(
                item.Name,
                Font,
                SystemBrushes.ControlText,
                new Rectangle(nameLocation, nameSize),
                nameForamt);
        }

        private void GridControl_MouseDown(object sender, MouseEventArgs e)
        {
            HandleMouseDown?.Invoke(sender, ConvertMouseEventArgs(e));
        }

        private void GridControl_MouseUp(object sender, MouseEventArgs e)
        {
            HandleMouseUp?.Invoke(sender, ConvertMouseEventArgs(e));
        }

        private void GridControl_MouseMove(object sender, MouseEventArgs e)
        {
            HandleMouseMove?.Invoke(sender, ConvertMouseEventArgs(e));
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
    }
}
