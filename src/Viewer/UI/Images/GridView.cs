using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
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
        public int NameHeight { get; set; } = 30;

        /// <summary>
        /// Space between the thumbnail and the name label
        /// </summary>
        public int NameSpace { get; set; } = 5;

        /// <summary>
        /// Size between thumbnail and an edge of an item
        /// </summary>
        public Size ItemPadding { get; set; } = new Size(5, 5);

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
                Grid.CellHeight = _itemSize.Height + NameHeight + NameSpace + ItemPadding.Height * 2;
                UpdateScrollableSize();
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
                _selectionBounds = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Grid of the view
        /// </summary>
        public Grid Grid { get; }

        /// <summary>
        /// Items to show in the component
        /// </summary>
        public List<EntityView> Items
        {
            get => _items;
            set
            {
                _items = value;
                UpdateScrollableSize();
                Refresh();
            }
        }

        private Size _itemSize;
        private Rectangle _selectionBounds;
        private List<EntityView> _items;
        
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
            Grid = new Grid { CellMargin = new Size(10, 10) };
            Grid.Resize(ClientSize.Width);

            InitializeComponent();

            SetStyle(ControlStyles.DoubleBuffer, true);

            _highlightFillColor = Color.FromArgb(226, 241, 255);
            _highlightStrokeColor = Color.FromArgb(221, 232, 248);

            _selectedFillColor = Color.FromArgb(221, 232, 248);
            _selectedStrokeColor = Color.FromArgb(210, 220, 236);

            _rangeSelectionFillColor = Color.FromArgb(150, 79, 143, 247);
            _rangeSelectionStrokeColor = Color.FromArgb(255, 79, 143, 247);
        }

        #region View interface
        
        public void UpdateItems()
        {
            UpdateScrollableSize();
        }

        public IEnumerable<EntityView> GetItemsIn(Rectangle bounds)
        {
            return Grid.GetCellsInBounds(bounds)
                .Where(cell => cell.Index >= 0 && cell.Index < Items.Count)
                .Select(cell => Items[cell.Index]);
        }

        /// <summary>
        /// Get item at <paramref name="location"/>.
        /// </summary>
        /// <param name="location">Queried location</param>
        /// <returns>Item at <paramref name="location"/> or null if there is no item</returns>
        public EntityView GetItemAt(Point location)
        {
            var index = Grid.GetCellAt(location).Index;
            if (index < 0 || index >= Items.Count)
            {
                return null;
            }

            return Items[index];
        }

        public Rectangle GetNameBounds(int index)
        {
            var cell = Grid.GetCell(index);
            return new Rectangle(GetNameLocation(cell.Bounds), GetNameSize(cell.Bounds));
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
                cellBounds.Y + (cellBounds.Height - NameHeight) + NameSpace - ItemPadding.Height
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

            var drawBounds = bounds;
            if ((item.State & FileViewState.Selected) != 0)
            {
                // draw selection 
                using (var brush = new SolidBrush(_selectedFillColor))
                using (var pen = new Pen(_selectedStrokeColor, _selectedItemBorderSize))
                {
                    graphics.FillRectangle(brush, drawBounds);
                    graphics.DrawRectangle(pen, drawBounds);
                }
            }
            else if ((item.State & FileViewState.Active) != 0)
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
            using (var nameForamt = new StringFormat {Alignment = StringAlignment.Center})
            {
                graphics.DrawString(
                    item.Name,
                    Font,
                    SystemBrushes.ControlText,
                    new Rectangle(nameLocation, nameSize),
                    nameForamt);
            }

            // draw the thumbnail
            var thumbnail = item.Thumbnail.GetCurrent(ItemSize);
            if (thumbnail == null)
            {
                return;
            }

            var thumbnailSize = ThumbnailGenerator.GetThumbnailSize(thumbnail.Size, ItemSize);
            var thumbnailLocation = GetThumbnailLocation(bounds, thumbnailSize);
            // we don't really need an interpolation as we are drawing the image in its original size
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
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
            Grid.CellCount = _items?.Count ?? 0;
            AutoScrollMinSize = new Size(
                0, // we don't want to have a horizontal scroll bar
                Grid.GridSize.Height
            );
        }
    }
}
