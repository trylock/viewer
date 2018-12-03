using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Data;

namespace Viewer.UI.Images.Layout
{
    internal class VerticalGridLayout : ImagesLayout
    {
        private Size LabelSizeWithMargin => new Size(
            GroupLabelSize.Width + GroupLabelMargin.Horizontal,
            GroupLabelSize.Height + GroupLabelMargin.Vertical);

        /// <summary>
        /// Non-stretched item size (i.e., it does not change with the layout client size)
        /// </summary>
        private Size ItemSize => new Size(
            ThumbnailAreaSize.Width + ItemPadding.Horizontal,
            ThumbnailAreaSize.Height + ItemPadding.Vertical);

        /// <summary>
        /// Stretched item size (it tries to fill the whole layout in the horizontal axis)
        /// </summary>
        public Size CellSize => new Size(
            (ClientSize.Width + ItemMargin.Horizontal) / ColumnCount - ItemMargin.Horizontal,
            ItemSize.Height
        );

        private Size CellSizeWithMargin => new Size(
            CellSize.Width + ItemMargin.Horizontal,
            CellSize.Height + ItemMargin.Vertical);

        /// <summary>
        /// Number of grid columns of each group
        /// </summary>
        private int ColumnCount => 
            Math.Max(
                (ClientSize.Width + ItemMargin.Horizontal) / (ItemSize.Width + ItemMargin.Horizontal),
                1
            );

        public override Size GetSize()
        {
            if (Groups == null)
            {
                return Size.Empty;
            }

            var result = new Size(ClientSize.Width, 0);
            foreach (var group in Groups)
            {
                result.Height += MeasureGroupHeight(group);
            }

            return result;
        }

        public override Rectangle GetItemBounds(EntityView item)
        {
            if (item == null || Groups == null)
            {
                return Rectangle.Empty;
            }

            // find group of the item
            var top = 0;
            Group itemGroup = null;
            int itemIndex = -1;
            foreach (var group in Groups)
            {
                itemGroup = group;
                itemIndex = group.Items.IndexOf(item);
                if (itemIndex >= 0)
                    break;
                top += MeasureGroupHeight(group);
            }

            // check if the item is in a non-collapsed group
            if (itemIndex < 0 || itemGroup.IsCollapsed)
                return Rectangle.Empty;

            // find bounds of the item within the group
            var row = itemIndex / ColumnCount;
            var column = itemIndex % ColumnCount;
            return new Rectangle(
                column * CellSizeWithMargin.Width, 
                row * CellSizeWithMargin.Height + top + LabelSizeWithMargin.Height,
                ItemSize.Width,
                ItemSize.Height);
        }

        private int MeasureGroupHeight(Group group)
        {
            // group label
            int height = LabelSizeWithMargin.Height;

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
            if (Groups == null)
                return null;
            if (location.X < 0 || location.X > ClientSize.Width)
                return null;
            if (location.Y < 0)
                return null;

            Group group = null;
            bool found = false;
            var bounds = new Rectangle(0, 0, ClientSize.Width, 0);
            foreach (var item in Groups)
            {
                group = item;
                bounds.Height = MeasureGroupHeight(group);
                if (bounds.Y + bounds.Height > location.Y)
                {
                    found = true;
                    break;
                }

                bounds.Y += bounds.Height;
            }

            if (group == null || !found)
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

            var index = row * ColumnCount + column;
            if (index >= element.Item.Items.Count)
            {
                return null;
            }

            return element.Item.Items[index];
        }

        public override IEnumerable<LayoutElement<EntityView>> GetItemsIn(Rectangle bounds)
        {
            foreach (var element in GetGroupsIn(bounds))
            {
                var localBounds = TransformToGroup(element, bounds);
                var gridBounds = new Rectangle(
                    0, 0,
                    ClientSize.Width, 
                    element.Bounds.Height - LabelSizeWithMargin.Height);
                localBounds.Intersect(gridBounds);

                if (localBounds.IsEmpty || element.Item.IsCollapsed)
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
                    var remainder = element.Item.Items.Count % ColumnCount;
                    if (i + 1 == rowCount && remainder != 0)
                    {
                        columnCount = Math.Min(remainder, columnCount);
                    }

                    for (var j = minColumn; j < columnCount; ++j)
                    {
                        var item = element.Item.Items[i * ColumnCount + j];
                        var itemBounds = new Rectangle(
                            j * CellSizeWithMargin.Width, 
                            i * CellSizeWithMargin.Height + element.Bounds.Top + LabelSizeWithMargin.Height,
                            CellSize.Width, 
                            CellSize.Height);
                        yield return new LayoutElement<EntityView>(itemBounds, item);
                    }
                }
            }
        }

        public override Group GetGroupLabelAt(Point location)
        {
            var element = FindGroup(location);
            if (element == null)
            {
                return null;
            }

            var labelBounds = new Rectangle(
                GroupLabelMargin.Left + element.Bounds.X, 
                GroupLabelMargin.Top + element.Bounds.Y,
                element.Bounds.Width - GroupLabelMargin.Horizontal, 
                GroupLabelSize.Height);
            if (labelBounds.Contains(location))
            {
                return element.Item;
            }

            return null;
        }

        public override IEnumerable<LayoutElement<Group>> GetGroupsIn(Rectangle bounds)
        {
            if (Groups == null)
            {
                yield break;
            }

            var top = 0;
            foreach (var group in Groups)
            {
                var height = MeasureGroupHeight(group);
                var groupBounds = new Rectangle(0, top, ClientSize.Width, height);
                if (bounds.IntersectsWith(groupBounds))
                {
                    yield return new LayoutElement<Group>(groupBounds, group);
                }

                top += height;
            }
        }

        public override IEnumerable<LayoutElement<Group>> GetGroupLabelsIn(Rectangle bounds)
        {
            foreach (var element in GetGroupsIn(bounds))
            {
                var labelBounds = new Rectangle(
                    element.Bounds.Location, 
                    new Size(element.Bounds.Width, LabelSizeWithMargin.Height));
                if (bounds.Contains(labelBounds))
                {
                    yield return new LayoutElement<Group>(labelBounds, element.Item);
                }
            }
        }
    }
}
