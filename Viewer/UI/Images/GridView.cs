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
using Viewer.Images;

namespace Viewer.UI.Images
{
    public partial class GridView : UserControl
    {
        /// <summary>
        /// Height of the area for file name
        /// </summary>
        public int NameHeight { get; set; } = 25;

        /// <summary>
        /// Size between thumbnail and an edge of an item
        /// </summary>
        public Size ItemPadding { get; set; } = new Size(8, 8);

        /// <summary>
        /// Size of a cell in the grid
        /// </summary>
        public Size ItemSize
        {
            get => _itemSize;
            set
            {
                _itemSize = value;
                Grid.MinCellWidth = _itemSize.Width + ItemPadding.Width * 2;
                Grid.CellHeight = _itemSize.Height + NameHeight + ItemPadding.Height * 2;
                UpdateScrollableSize();
                Refresh();
            }
        }

        /// <summary>
        /// Bounds of a selection 
        /// </summary>
        public Rectangle SelectionBounds
        {
            get => _selectionBounds;
            set
            {
                Invalidate(ProjectBounds(_selectionBounds));
                _selectionBounds = value;
                Invalidate(ProjectBounds(_selectionBounds));
            }
        }

        /// <summary>
        /// Grid of the view
        /// </summary>
        public Grid Grid { get; }

        /// <summary>
        /// Items to show in the component
        /// </summary>
        public IReadOnlyList<EntityView> Items
        {
            get => _items;
            set
            {
                _items = value;
                Grid.CellCount = _items == null ? 0 : _items.Count;
                UpdateScrollableSize();
                Refresh();
            }
        }

        private Size _itemSize;
        private Rectangle _selectionBounds;
        private IReadOnlyList<EntityView> _items;

        #region Graphics settings

        private readonly Color _highlightFillColor;
        private readonly Color _highlightStrokeColor;
        private readonly int _highlightedItemBorderSize = 1;

        private readonly Color _selectedFillColor;
        private readonly Color _selectedStrokeColor;
        private readonly int _selectedItemBorderSize = 1;

        private readonly Color _rangeSelectionFillColor;
        private readonly Color _rangeSelectionStrokeColor;

        #endregion

        public GridView()
        {
            Grid = new Grid { CellMargin = new Size(5, 5) };
            Grid.Resize(ClientSize.Width);

            InitializeComponent();

            _highlightFillColor = Color.FromArgb(226, 241, 255);
            _highlightStrokeColor = Color.FromArgb(221, 232, 248);

            _selectedFillColor = Color.FromArgb(221, 232, 248);
            _selectedStrokeColor = Color.FromArgb(210, 220, 236);

            _rangeSelectionFillColor = Color.FromArgb(150, 79, 143, 247);
            _rangeSelectionStrokeColor = Color.FromArgb(255, 79, 143, 247);
        }

        #region View interface

        public void InvalidateItem(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;
            var cell = Grid.GetCell(index);
            var bounds = cell.Bounds;
            Invalidate(ProjectBounds(bounds));
        }

        public IEnumerable<int> GetItemsIn(Rectangle bounds)
        {
            return Grid.GetCellsInBounds(bounds).Select(cell => cell.Index);
        }

        public int GetItemAt(Point location)
        {
            return Grid.GetCellAt(location).Index;
        }

        #endregion

        public Point GetThumbnailLocation(Rectangle cellBounds, Size thumbnailSize)
        {
            return new Point(
                // align the thumbnail to the center horizontally
                cellBounds.Location.X + (cellBounds.Width - thumbnailSize.Width) / 2,
                // align the thumbnail to the bottom vertically
                cellBounds.Location.Y + (cellBounds.Height - NameHeight - thumbnailSize.Height) / 2
            );
        }

        public Point GetNameLocation(Rectangle cellBounds)
        {
            return new Point(
                cellBounds.X + ItemPadding.Width,
                cellBounds.Y + (cellBounds.Height - NameHeight) - ItemPadding.Height
            );
        }

        public Size GetNameSize(Rectangle cellBounds)
        {
            return new Size(
                cellBounds.Width - 2 * ItemPadding.Width,
                NameHeight
            );
        }
        
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

        /// <summary>
        /// Move mouse location to UI coordinates
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public MouseEventArgs ConvertMouseEventArgs(MouseEventArgs e)
        {
            var location = UnprojectLocation(e.Location);
            return new MouseEventArgs(e.Button, e.Clicks, location.X, location.Y, e.Delta);
        }

        #endregion

        private void GridView_Paint(object sender, PaintEventArgs e)
        {
            // update invalid items
            var clipBounds = UnprojectBounds(e.ClipRectangle);
            var cells = Grid.GetCellsInBounds(clipBounds);
            foreach (var cell in cells)
            {
                PaintItem(e.Graphics, cell);
            }

            // update selection 
            if (!SelectionBounds.IsEmpty)
            {
                using (var brush = new SolidBrush(_rangeSelectionFillColor))
                using (var pen = new Pen(_rangeSelectionStrokeColor, 1))
                {
                    var selectionBounds = Rectangle.Inflate(ProjectBounds(SelectionBounds), -1, -1);
                    e.Graphics.FillRectangle(brush, selectionBounds);
                    e.Graphics.DrawRectangle(pen, selectionBounds);
                }
            }
        }

        private void PaintItem(Graphics graphics, GridCell cell)
        {
            // find thumbnail
            var item = Items[cell.Index];
            var bounds = ProjectBounds(cell.Bounds);

            // clear grid cell area
            graphics.FillRectangle(Brushes.White, bounds);

            var drawBounds = Rectangle.Inflate(bounds, -1, -1);
            if ((item.State & ResultItemState.Selected) != 0)
            {
                // draw selection 
                using (var brush = new SolidBrush(_selectedFillColor))
                using (var pen = new Pen(_selectedStrokeColor, _selectedItemBorderSize))
                {
                    graphics.FillRectangle(brush, drawBounds);
                    graphics.DrawRectangle(pen, drawBounds);
                }
            }
            else if ((item.State & ResultItemState.Active) != 0)
            {
                // draw highlight
                using (var brush = new SolidBrush(_highlightFillColor))
                using (var pen = new Pen(_highlightStrokeColor, _highlightedItemBorderSize))
                {
                    graphics.FillRectangle(brush, drawBounds);
                    graphics.DrawRectangle(pen, drawBounds);
                }
            }

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

            // draw the thumbnail
            var thumbnail = item.Thumbnail.Value;
            if (thumbnail == null)
            {
                return;
            }
            
            var thumbnailSize = ThumbnailGenerator.GetThumbnailSize(thumbnail.Size, ItemSize);
            var thumbnailLocation = GetThumbnailLocation(bounds, thumbnailSize);
            // we don't really need an interpolation as we are drawing the image in its original size
            graphics.InterpolationMode = InterpolationMode.Low;
            graphics.DrawImage(thumbnail, new Rectangle(thumbnailLocation, thumbnailSize));
        }

        private void GridView_Resize(object sender, EventArgs e)
        {
            UpdateScrollableSize();
            Refresh();
        }

        private void UpdateScrollableSize()
        {
            Grid.Resize(ClientSize.Width);
            AutoScrollMinSize = new Size(
                0, // we don't want to have a horizontal scroll bar
                Grid.GridSize.Height
            );
        }
    }
}
