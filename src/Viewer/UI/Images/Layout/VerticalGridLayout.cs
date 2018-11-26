using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;

namespace Viewer.UI.Images.Layout
{
    internal class VerticalGridLayout : ImagesLayout
    {
        public int Width { get; set; }

        public Padding ItemMargin { get; set; }

        public Padding ItemPadding { get; set; }

        public Size GroupLabelSize { get; set; }

        public Padding GroupLabelMargin { get; set; }

        private Size ItemSize => new Size(
            ThumbnailAreaSize.Width + ItemPadding.Horizontal,
            ThumbnailAreaSize.Height + ItemPadding.Vertical);

        public Size CellSize => new Size(
            (Width + ItemMargin.Horizontal) / ColumnCount - ItemMargin.Horizontal,
            ItemSize.Height
        );

        private Size CellSizeWithMargin => new Size(
            CellSize.Width + ItemMargin.Horizontal,
            CellSize.Height + ItemMargin.Vertical);

        private int ColumnCount => 
            Math.Max(
                (Width + ItemMargin.Horizontal) / (ItemSize.Width + ItemMargin.Horizontal),
                1
            );

        public override Size GetSize()
        {
            var result = new Size(Width, 0);
            foreach (var pair in Groups)
            {
                var group = pair.Value;
                result.Height += GetGroupHeight(group);
            }

            return result;
        }

        private int GetGroupHeight(Group group)
        {
            // group label
            int height = GroupLabelSize.Height + GroupLabelMargin.Vertical;

            // group content
            if (!group.IsCollapsed)
            {
                var rowCount = group.Items.Count.RoundUpDiv(ColumnCount);
                height += rowCount * (ItemSize.Height + ItemMargin.Vertical);

                // we don't use vertical item margin for the top and the bottom row
                if (rowCount > 0)
                {
                    height -= ItemMargin.Vertical;
                }
            }

            return height;
        }

        private LayoutElement<Group> FindGroup(Point location)
        {
            if (location.X < 0 || location.X > Width)
                return null;
            if (location.Y < 0)
                return null;

            Group group = null;
            var bounds = new Rectangle(0, 0, Width, 0);
            foreach (var pair in Groups)
            {
                group = pair.Value;
                bounds.Height = GetGroupHeight(group);
                if (bounds.Y + bounds.Height > location.Y)
                    break;
                bounds.Y += bounds.Height;
            }

            if (group == null)
            {
                return null; // location is below all groups
            }

            return new LayoutElement<Group>(bounds, group);
        }

        private Point TransformToGroup(LayoutElement<Group> element, Point location)
        {
            return new Point(
                location.X - element.Bounds.X,
                location.Y - element.Bounds.Y - GroupLabelSize.Height - GroupLabelMargin.Vertical);
        }

        private Rectangle TransformToGroup(LayoutElement<Group> element, Rectangle bounds)
        {
            return new Rectangle(TransformToGroup(element, bounds.Location), bounds.Size);
        }

        public override EntityView GetItemAt(Point location)
        {
            var element = FindGroup(location);
            if (element == null || element.Item.IsCollapsed) 
            {
                return null;
            }

            var localPoint = TransformToGroup(element, location);
            if (localPoint.X < 0 || localPoint.Y < 0)
            {
                return null;
            }
            
            var column = localPoint.X / CellSizeWithMargin.Width;
            var row = localPoint.Y / CellSizeWithMargin.Height;

            if ((localPoint.X % CellSizeWithMargin.Width) > CellSize.Width ||
                (localPoint.Y % CellSizeWithMargin.Height) > CellSize.Height)
            {
                return null; // the location is in an empty space between items
            }

            return element.Item.Items[row * ColumnCount + column];
        }

        public override IEnumerable<LayoutElement<EntityView>> GetItemsIn(Rectangle bounds)
        {
            foreach (var element in GetGroupsIn(bounds))
            {
                var localBounds = TransformToGroup(element, bounds);
                var gridBounds = new Rectangle(
                    0, 0, 
                    Width, 
                    element.Bounds.Height - GroupLabelSize.Height - GroupLabelMargin.Vertical);
                localBounds.Intersect(gridBounds);

                if (localBounds.IsEmpty)
                    continue;

                // find start and end row/column
                var minColumn = localBounds.X / CellSizeWithMargin.Width;
                var minRow = localBounds.Y / CellSizeWithMargin.Height;
                if ((localBounds.X % CellSizeWithMargin.Width) > CellSize.Width)
                    ++minColumn;
                if ((localBounds.Y % CellSizeWithMargin.Height) > CellSize.Height)
                    ++minRow;

                var maxColumn = localBounds.Right / CellSizeWithMargin.Width + 1;
                var maxRow = localBounds.Bottom / CellSizeWithMargin.Height + 1;

                // make sure to only iterate over cells in the grid
                var rowCount = element.Item.Items.Count.RoundUpDiv(ColumnCount);
                maxColumn = Math.Min(maxColumn, ColumnCount);
                maxRow = Math.Min(maxRow, rowCount);

                // retrun all cells in this grid range
                for (var i = minRow; i < maxRow; ++i)
                {
                    // the last row has fewer cells
                    var columnCount = maxColumn;
                    if (i + 1 == rowCount)
                    {
                        columnCount = element.Item.Items.Count % columnCount;
                    }

                    for (var j = minColumn; j < columnCount; ++j)
                    {
                        var item = element.Item.Items[i * ColumnCount + j];
                        var itemBounds = new Rectangle(
                            j * CellSizeWithMargin.Width, 
                            i * CellSizeWithMargin.Height + element.Bounds.Top + GroupLabelSize.Height + GroupLabelMargin.Vertical,
                            CellSize.Width, 
                            CellSize.Height);
                        yield return new LayoutElement<EntityView>(itemBounds, item);
                    }
                }
            }
        }

        public override Group GetGroupLabelAt(Point location)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<LayoutElement<Group>> GetGroupsIn(Rectangle bounds)
        {
            var top = 0;
            foreach (var pair in Groups)
            {
                Group group = pair.Value;
                var height = GetGroupHeight(group);
                var groupBounds = new Rectangle(0, top, Width, height);
                if (bounds.IntersectsWith(groupBounds))
                {
                    yield return new LayoutElement<Group>(groupBounds, group);
                }

                top += height;
            }
        }
    }
}
