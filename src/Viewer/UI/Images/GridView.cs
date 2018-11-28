using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Images;
using Viewer.UI.Images.Layout;

namespace Viewer.UI.Images
{
    internal partial class GridView : UserControl
    {
        /// <summary>
        /// Height of the area for file name
        /// </summary>
        public int NameHeight { get; set; }

        /// <summary>
        /// Space between the thumbnail and the name label
        /// </summary>
        public int NameSpace { get; set; }

        /// <summary>
        /// Size of a cell in the grid
        /// </summary>
        public Size ItemSize
        {
            get => _itemSize;
            set
            {
                _itemSize = value;
                Layout.ThumbnailAreaSize = _itemSize;
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
        /// Layout of this component
        /// </summary>
        public ImagesLayout Layout { get; set; }

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
            Layout = new VerticalGridLayout
            {
                GroupLabelSize = new Size(0, 30),
                ItemPadding = new Padding(5),
                ItemMargin = new Padding(5)
            };
            Layout.Resize(ClientSize);

            InitializeComponent();

            NameHeight = Font.Height * 2;
            NameSpace = Font.Height;

            SetStyle(ControlStyles.DoubleBuffer, true);

            _highlightFillColor = Color.FromArgb(226, 241, 255);
            _highlightStrokeColor = Color.FromArgb(221, 232, 248);

            _selectedFillColor = Color.FromArgb(218, 231, 251);
            _selectedStrokeColor = Color.FromArgb(194, 208, 229);

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
            return Layout.GetItemsIn(bounds).Select(element => element.Item);
        }

        /// <summary>
        /// Get item at <paramref name="location"/>.
        /// </summary>
        /// <param name="location">Queried location</param>
        /// <returns>Item at <paramref name="location"/> or null if there is no item</returns>
        public EntityView GetItemAt(Point location)
        {
            return Layout.GetItemAt(location);
        }

        public Rectangle GetNameBounds(EntityView item)
        {
            var bounds = Layout.GetItemBounds(item);
            return new Rectangle(GetNameLocation(bounds), GetNameSize(bounds));
        }

        public EntityView FindItem(EntityView currentItem, Point delta)
        {
            throw new NotImplementedException();
        }

        public EntityView FindFirstItemAbove(EntityView currentItem)
        {
            throw new NotImplementedException();
        }

        public EntityView FindLastItemBelow(EntityView currentItem)
        {
            throw new NotImplementedException();
        }

        public void EnsureItemVisible(EntityView item)
        {
            var bounds = Layout.GetItemBounds(item);
            if (bounds.IsEmpty)
            {
                return; // item has not been found
            }

            var viewport = UnprojectBounds(new Rectangle(Point.Empty, ClientSize));
            var transformed = viewport.EnsureContains(bounds);
            AutoScrollPosition = transformed.Location;

            Invalidate();
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
                cellBounds.X + Layout.ItemPadding.Left,
                cellBounds.Y + (cellBounds.Height - NameHeight) + NameSpace - Layout.ItemPadding.Vertical
            );
        }

        public Size GetNameSize(Rectangle cellBounds)
        {
            return new Size(
                cellBounds.Width - Layout.ItemPadding.Horizontal,
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
            var elements = Layout.GetItemsIn(clipBounds);

            using (var background = new SolidBrush(Color.FromArgb(unchecked((int) 0xFFeeeef2))))
            {
                e.Graphics.FillRectangle(background, e.ClipRectangle);
            }

            foreach (var element in elements)
            {
                PaintItem(e.Graphics, element);
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

        private void PaintItem(Graphics graphics, LayoutElement<EntityView> element)
        {
            // find thumbnail
            var item = element.Item;
            var bounds = ProjectBounds(element.Bounds);

            // clear grid cell area
            graphics.FillRectangle(Brushes.White, bounds);

            var drawBounds = bounds;
            if ((item.State & EntityViewState.Selected) != 0)
            {
                // draw selection 
                using (var brush = new SolidBrush(_selectedFillColor))
                using (var pen = new Pen(_selectedStrokeColor, _selectedItemBorderSize))
                {
                    graphics.FillRectangle(brush, drawBounds);
                    graphics.DrawRectangle(pen, drawBounds);
                }
            }
            else if ((item.State & EntityViewState.Active) != 0)
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
            Layout.Resize(ClientSize);
            AutoScrollMinSize = new Size(
                0, // we don't want to have a horizontal scroll bar
                Layout.GetSize().Height
            );
        }
    }
}
