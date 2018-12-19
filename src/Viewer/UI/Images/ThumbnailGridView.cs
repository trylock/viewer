using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Data;
using Viewer.Images;
using Viewer.Properties;
using Viewer.UI.Images.Layout;

namespace Viewer.UI.Images
{
    /// <summary>
    /// The sole purpose of this control is to draw whichever layout it gets.
    /// Logic of this control is handeled entirely by its parent control
    /// (<see cref="ImagesView"/>)
    /// </summary>
    internal partial class ThumbnailGridView : UserControl
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
                ControlLayout.ThumbnailAreaSize = _itemSize;
                UpdateScrollableSize();
            }
        }

        private Size _itemSize;

        private Rectangle _selectionBounds;

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
        public ImagesLayout ControlLayout { get; set; }
        
        /// <summary>
        /// Items to show in the component
        /// </summary>
        public List<Group> Items
        {
            get => ControlLayout.Groups;
            set
            {
                ControlLayout.Groups = value;
                UpdateScrollableSize();
                Invalidate();
            }
        }

        /// <summary>
        /// Control which is used to draw current group label
        /// </summary>
        /// <remarks>
        /// This control is added to the parent control so that it is not affected by current
        /// scroll position.
        /// </remarks>
        public Control GroupLabelControl { get; }
        
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

        private class DoubleBufferedControl : Control
        {
            public DoubleBufferedControl()
            {
                DoubleBuffered = true;
            }
        }

        public ThumbnailGridView()
        {
            InitializeComponent();

            // setup current group label control
            GroupLabelControl = new DoubleBufferedControl
            {
                Location = new Point(0, 0),
                Size = new Size(800, 30)
            };
            GroupLabelControl.Paint += GroupLabelControl_Paint;

            // setup control layout
            NameHeight = Font.Height * 2;
            NameSpace = Font.Height;
            ControlLayout = new VerticalGridLayout
            {
                GroupLabelSize = new Size(0, Font.Height * 3),
                ItemPadding = new Padding(5, 5, 5, 5 + NameHeight + NameSpace),
                ItemMargin = new Padding(5)
            };
            UpdateClientBounds();

            MouseWheel += GridView_MouseWheel;
            Scroll += GridView_Scroll;

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
            Invalidate();
        }

        public IEnumerable<EntityView> GetItemsIn(Rectangle bounds)
        {
            return ControlLayout.GetItemsIn(bounds).Select(element => element.Item);
        }

        /// <summary>
        /// Get item at <paramref name="location"/>.
        /// </summary>
        /// <param name="location">Queried location</param>
        /// <returns>Item at <paramref name="location"/> or null if there is no item</returns>
        public EntityView GetItemAt(Point location)
        {
            return ControlLayout.GetItemAt(location);
        }

        public Rectangle GetNameBounds(EntityView item)
        {
            var bounds = ControlLayout.GetItemBounds(item);
            return new Rectangle(GetNameLocation(bounds), GetNameSize(bounds));
        }

        public EntityView FindItem(EntityView currentItem, Point delta)
        {
            var element = ControlLayout.FindItem(currentItem, delta);
            if (element == null)
            {
                return currentItem;
            }

            return element.Item;
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
            var bounds = ControlLayout.GetItemBounds(item);
            if (bounds.IsEmpty)
            {
                return; // item has not been found
            }

            // if the item is at the top of the viewport, we should leave space for group label
            bounds.Y -= GroupLabelControl.Height;
            bounds.Height += GroupLabelControl.Height;

            // transform viewport bounds and set new scroll position
            var viewport = UnprojectBounds(new Rectangle(Point.Empty, ClientSize));
            var transformed = viewport.EnsureContains(bounds);
            AutoScrollPosition = transformed.Location;

            Invalidate();
        }

        public void EnsureGroupVisible(Group group)
        {
            AutoScrollPosition = group.View.Location;
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
                cellBounds.X + ControlLayout.ItemPadding.Left,
                cellBounds.Y + (cellBounds.Height - NameHeight) + 
                    NameSpace - ControlLayout.ItemPadding.Top
            );
        }

        public Size GetNameSize(Rectangle cellBounds)
        {
            return new Size(
                cellBounds.Width - ControlLayout.ItemPadding.Horizontal,
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
            // update invalid area
            var clipBounds = UnprojectBounds(e.ClipRectangle);
            var elements = ControlLayout.GetItemsIn(clipBounds);

            using (var background = new SolidBrush(Color.FromArgb(unchecked((int) 0xFFeeeef2))))
            {
                e.Graphics.FillRectangle(background, e.ClipRectangle);
            }
            
            // draw thumbnails
            foreach (var element in elements)
            {
                PaintItem(e.Graphics, element);
            }

            // draw group labels
            foreach (var element in ControlLayout.GetGroupLabelsIn(clipBounds))
            {
                PaintGroupLabel(e.Graphics, element);
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

        /// <summary>
        /// Draw group label
        /// </summary>
        /// <param name="graphics">Graphics context</param>
        /// <param name="element">Group and its label bounds</param>
        private void PaintGroupLabel(Graphics graphics, LayoutElement<Group> element)
        {
            var bounds = ProjectBounds(element.Bounds);
            Group group = element.Item;

            var name = group.Key.IsNull ? "Default" : group.Key.ToString(CultureInfo.CurrentCulture);
            var label = string.Format(Resources.Group_Label, name, group.Items.Count);
            
            PointF offset = new PointF(0, 0);
            using (var iconPen = new Pen(Color.FromArgb(unchecked((int)0xFF6d6d8d))))
            using (var separatorPen = new Pen(Color.FromArgb(unchecked((int)0xFFd4d4de))))
            using (var textBrush = new SolidBrush(Color.FromArgb(unchecked((int)0xFF272768))))
            using (var defaultBrush = new SolidBrush(Color.FromArgb(unchecked((int)0xFFeeeef2))))
            using (var activeBrush = new SolidBrush(Color.FromArgb(unchecked((int)0xFFf9f9fb))))
            using (var font = new Font(Font.FontFamily, 1.25f * Font.Size))
            {
                // draw background
                var cursorLocation = UnprojectLocation(PointToClient(MousePosition));
                var boundsWithoutMargin = new Rectangle(
                    element.Bounds.X + ControlLayout.GroupLabelMargin.Left,
                    element.Bounds.Y + ControlLayout.GroupLabelMargin.Top,
                    element.Bounds.Width - ControlLayout.GroupLabelMargin.Horizontal,
                    element.Bounds.Height - ControlLayout.GroupLabelMargin.Vertical);
                if (boundsWithoutMargin.Contains(cursorLocation))
                {
                    graphics.FillRectangle(activeBrush, ProjectBounds(boundsWithoutMargin));
                }
                else
                {
                    graphics.FillRectangle(defaultBrush, ProjectBounds(boundsWithoutMargin));
                }

                // draw collapsed state icon
                var icon = VectorIcons.GoForwardIcon;
                var iconHeight = font.Height * 0.8f;
                var iconBounds = icon.GetBounds();

                var state = graphics.Save();
                try
                {
                    // translate icon origin to [0, 0]
                    graphics.TranslateTransform(
                        -iconBounds.X - iconBounds.Width / 2,
                        -iconBounds.Y - iconBounds.Height / 2, MatrixOrder.Append);
                    // scale the icon to the label size 
                    graphics.ScaleTransform(
                        iconHeight / 2f / iconBounds.Width,
                        iconHeight / iconBounds.Height, MatrixOrder.Append);
                    graphics.RotateTransform(group.View.IsCollapsed ? 0 : 90, MatrixOrder.Append);
                    // translate the icon back to the group label position
                    graphics.TranslateTransform(
                        bounds.X + ControlLayout.GroupLabelMargin.Left + iconHeight,
                        bounds.Y + ControlLayout.GroupLabelMargin.Top + ControlLayout.GroupLabelSize.Height / 2, MatrixOrder.Append);
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.DrawPath(iconPen, icon);

                    offset.X += iconHeight;
                }
                finally
                {
                    graphics.Restore(state);
                }

                // draw group label
                var measure = graphics.MeasureString(label, font);
                offset.X += bounds.X + ControlLayout.GroupLabelMargin.Left + iconHeight;
                offset.Y += bounds.Y +
                            ControlLayout.GroupLabelMargin.Top +
                            ControlLayout.GroupLabelSize.Height / 2 -
                            measure.Height / 2 + 1;
                graphics.DrawString(
                    label,
                    font,
                    textBrush,
                    offset.X,
                    offset.Y);

                // draw filler line
                offset.Y += measure.Height / 2;
                offset.X += measure.Width + Font.Height;
                if (offset.X >= ClientSize.Width - Font.Height)
                    return; // the line is completely hidden
                graphics.DrawLine(separatorPen,
                    offset.X,
                    offset.Y,
                    ClientSize.Width - Font.Height,
                    offset.Y);
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

        private void GroupLabelControl_Paint(object sender, PaintEventArgs e)
        {
            var location = UnprojectLocation(Point.Empty);
            var element = ControlLayout.GetGroupAt(location);
            if (element == null)
            {
                return;
            }
            
            PaintGroupLabel(e.Graphics, new LayoutElement<Group>(
                new Rectangle(UnprojectLocation(Point.Empty), GroupLabelControl.Size), 
                element.Item));
        }

        private void GridView_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta <= -120 || e.Delta >= 120)
            {
                AlignScrollLocation(e.Delta < 0);
            }

            GroupLabelControl.Invalidate();
        }
        
        private void GridView_Scroll(object sender, ScrollEventArgs e)
        {
            var newGroup = ControlLayout.GetGroupAt(new Point(0, e.NewValue));
            var oldGroup = ControlLayout.GetGroupAt(new Point(0, e.OldValue));
            if (newGroup != oldGroup)
            {
                GroupLabelControl.Invalidate();
            }
        }

        private void GridView_Resize(object sender, EventArgs e)
        {
            UpdateScrollableSize();
            Invalidate();
        }

        private void AlignScrollLocation(bool alignLowerBoundary)
        {
            var location = UnprojectLocation(Point.Empty);
            if (alignLowerBoundary)
            {
                location.Y += ClientSize.Height;
            }

            location = ControlLayout.AlignLocation(location, alignLowerBoundary);

            // transform location of the lower boundary back up
            // add space for group label
            if (alignLowerBoundary)
            {
                location.Y -= ClientSize.Height;
            }
            else
            {
                location.Y -= GroupLabelControl.Height;
            }
            
            AutoScrollPosition = location;
        }

        private void UpdateClientBounds()
        {
            ControlLayout.ClientBounds = new Rectangle(
                new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y),
                ClientSize
            );
        }

        private void UpdateLabelSize()
        {
            if (GroupLabelControl == null)
            {
                return;
            }

            GroupLabelControl.Width = ClientSize.Width;
            GroupLabelControl.Height = ControlLayout.GroupLabelSize.Height;
            GroupLabelControl.Invalidate();
        }

        private void UpdateScrollableSize()
        {
            if (ControlLayout == null)
            {
                return;
            }

            UpdateLabelSize();
            AutoScrollMinSize = new Size(
                0, // we don't want to have a horizontal scroll bar
                ControlLayout.GetSize().Height
            );
            UpdateClientBounds();
        }
    }
}
