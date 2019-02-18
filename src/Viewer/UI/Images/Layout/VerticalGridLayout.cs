﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Core.Collections;
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
            (ClientBounds.Width + ItemMargin.Horizontal) / ColumnCount - ItemMargin.Horizontal,
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
                (ClientBounds.Width + ItemMargin.Horizontal) / 
                    (ItemSize.Width + ItemMargin.Horizontal),
                1
            );

        public override int ViewportColumnCount => ColumnCount;

        public override int ViewportRowCount
        {
            get
            {
                // there is always at least 1 group label visible in the viewport
                var labelCount = GetGroupLabelsIn(ClientBounds).Count();
                var areaForThumbnails = ClientBounds.Height + 
                                        ItemMargin.Vertical - 
                                        labelCount * LabelSizeWithMargin.Height;
                return areaForThumbnails.RoundUpDiv(CellSizeWithMargin.Height);
            }
        }

        private Point GetGroupLocation(int index)
        {
            return Groups[index].View.Location;
        }

        /// <summary>
        /// Make sure group locations are computed correctly for all groups
        /// </summary>
        private void RecomputeGroupLocations()
        {
            if (Groups == null || Groups.Count <= 0)
            {
                return; // nothing else to do if the layout is empty
            }
            
            // recompute group location
            Groups[0].View.Location = Point.Empty;
            for (var i = 0; i < Groups.Count - 1; ++i)
            {
                var prevView = Groups[i].View;
                var nextView = Groups[i + 1].View;
                nextView.Location = new Point(
                    0, 
                    prevView.Location.Y + MeasureGroupHeight(Groups[i]));
            }
        }

        protected override void OnLayoutChanged()
        {
            base.OnLayoutChanged();
            RecomputeGroupLocations();
        }

        public override Size GetSize()
        {
            if (Groups == null || Groups.Count <= 0)
            {
                return Size.Empty;
            }

            var lastGroup = Groups[Groups.Count - 1];
            var result = new Size(
                ClientBounds.Width, 
                lastGroup.View.Location.Y + MeasureGroupHeight(lastGroup));
            return result;
        }

        private Rectangle GetItemBounds(Group itemGroup, int itemIndex)
        {
            // check if the item is in a non-collapsed group
            if (itemIndex < 0 || itemGroup.View.IsCollapsed)
                return Rectangle.Empty;

            // find bounds of the item within the group
            var row = itemIndex / ColumnCount;
            var column = itemIndex % ColumnCount;
            return new Rectangle(
                column * CellSizeWithMargin.Width,
                row * CellSizeWithMargin.Height + 
                    itemGroup.View.Location.Y + 
                    LabelSizeWithMargin.Height,
                CellSize.Width,
                CellSize.Height);
        }

        public override Rectangle GetItemBounds(EntityView item)
        {
            if (item == null || Groups == null)
            {
                return Rectangle.Empty;
            }

            // find group of the item
            var group = Groups.Find(g => g.Items.Contains(item));
            if (group == null)
            {
                return Rectangle.Empty;
            }

            var itemIndex = group.Items.IndexOf(item);
            return GetItemBounds(group, itemIndex);
        }

        /// <summary>
        /// Measure height of given group. It takes into account the group label and all items.
        /// If the group <see cref="GroupView.IsCollapsed"/>, height of the items is not counted.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private int MeasureGroupHeight(Group group)
        {
            // group label
            int height = LabelSizeWithMargin.Height;

            // group content
            if (!group.View.IsCollapsed)
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

        private class VerticalPointComparer : IComparer<Group>
        {
            public int Compare(Group x, Group y)
            {
                return Comparer<int?>.Default.Compare(x?.View.Location.Y, y?.View.Location.Y);
            }
        }
        
        /// <summary>
        /// Find group at <paramref name="location"/>
        /// </summary>
        /// <param name="location"></param>
        /// <returns>
        /// Group index or -1 if there is no group at <paramref name="location"/>
        /// </returns>
        private int FindGroupIndex(Point location)
        {
            if (Groups == null)
                return -1;
            if (location.X < 0 || location.X > ClientBounds.Width)
                return -1;
            if (location.Y < 0)
                return -1;

            // find group using a binary search
            location = new Point(
                location.X, 
                // if the location is at a boundary of 2 groups, we want to find the second 
                // group (the one which is lower)
                location.Y + 1
            );
            var dummyGroup = new Group(new IntValue(null));
            dummyGroup.View.Location = location;
            var index = Groups.LowerBound(dummyGroup, new VerticalPointComparer()) - 1;
            if (index < 0)
                return index;

            // check if the point is below all groups
            var maxY = GetGroupLocation(index).Y + MeasureGroupHeight(Groups[index]);
            if (location.Y > maxY)
            {
                return -1;
            }
            return index;
        }

        /// <summary>
        /// Find group at <paramref name="location"/>. Group area includes its label with all
        /// margins around it and the area for photo grid of this group.
        /// </summary>
        /// <param name="location"></param>
        /// <returns>
        /// Group at <paramref name="location"/> together with its bounding rectangle
        /// </returns>
        private LayoutElement<Group> FindGroup(Point location)
        {
            var index = FindGroupIndex(location);
            if (index < 0)
            {
                return null;
            }

            var group = Groups[index];
            var bounds = new Rectangle(
                GetGroupLocation(index), 
                new Size(ClientBounds.Width, MeasureGroupHeight(group)));
            
            return new LayoutElement<Group>(bounds, group);
        }

        /// <summary>
        /// Transform <paramref name="location"/> to coordinates of <paramref name="element"/>
        /// </summary>
        /// <param name="element"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        private Point ToGroupCoordinates(LayoutElement<Group> element, Point location)
        {
            return new Point(
                location.X - element.Bounds.X,
                location.Y - element.Bounds.Y - GroupLabelSize.Height - GroupLabelMargin.Vertical);
        }

        private Rectangle ToGroupCoordinates(LayoutElement<Group> element, Rectangle bounds)
        {
            return new Rectangle(ToGroupCoordinates(element, bounds.Location), bounds.Size);
        }

        public override EntityView GetItemAt(Point location)
        {
            var element = FindGroup(location);
            if (element == null || element.Item.View.IsCollapsed) 
            {
                return null;
            }

            var localPoint = ToGroupCoordinates(element, location);
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

            Group group = element.Item;
            var index = row * ColumnCount + column;
            if (index >= group.Items.Count)
            {
                return null;
            }

            return group.Items[index];
        }

        public override IEnumerable<LayoutElement<EntityView>> GetItemsIn(Rectangle bounds)
        {
            foreach (var element in GetGroupsIn(bounds))
            {
                Group group = element.Item;
                var localBounds = ToGroupCoordinates(element, bounds);
                var gridBounds = new Rectangle(
                    0, 0,
                    ClientBounds.Width, 
                    element.Bounds.Height - LabelSizeWithMargin.Height);
                localBounds.Intersect(gridBounds);

                if (localBounds.IsEmpty || group.View.IsCollapsed)
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
                var rowCount = group.Items.Count.RoundUpDiv(ColumnCount);
                maxColumn = Math.Min(maxColumn, ColumnCount);
                maxRow = Math.Min(maxRow, rowCount);

                // retrun all cells in this grid range
                for (var i = minRow; i < maxRow; ++i)
                {
                    // the last row has fewer cells
                    var columnCount = maxColumn;
                    var remainder = group.Items.Count % ColumnCount;
                    if (i + 1 == rowCount && remainder != 0)
                    {
                        columnCount = Math.Min(remainder, columnCount);
                    }

                    for (var j = minColumn; j < columnCount; ++j)
                    {
                        var item = group.Items[i * ColumnCount + j];
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

        public override LayoutElement<Group> GetGroupLabelAt(Point location)
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
                return new LayoutElement<Group>(labelBounds, element.Item);
            }

            return null;
        }

        public override IEnumerable<LayoutElement<Group>> GetGroupsIn(Rectangle bounds)
        {
            if (Groups == null)
            {
                yield break;
            }

            // find bounds intersection with the grid 
            if (bounds.X < 0)
                bounds.Width = Math.Max(bounds.Width + bounds.X, 0);
            if (bounds.Y < 0)
                bounds.Height = Math.Max(bounds.Height + bounds.Y, 0);

            if (bounds.IsEmpty)
            {
                yield break;
            }

            bounds.Location = new Point(Math.Max(bounds.X, 0), Math.Max(bounds.Y, 0));
            
            var index = FindGroupIndex(bounds.Location);
            if (index < 0)
            {
                yield break;
            }
            
            // return all groups in the bounds
            for (; index < Groups.Count; ++index)
            {
                Group group = Groups[index];
                var height = MeasureGroupHeight(group);
                var groupBounds = new Rectangle(
                    GetGroupLocation(index), 
                    new Size(ClientBounds.Width, height));
                if (!groupBounds.IntersectsWith(bounds))
                    break;
                yield return new LayoutElement<Group>(groupBounds, group);
            }
        }

        public override LayoutElement<Group> GetGroupAt(Point location)
        {
            return FindGroup(location);
        }

        public override IEnumerable<LayoutElement<Group>> GetGroupLabelsIn(Rectangle bounds)
        {
            foreach (var element in GetGroupsIn(bounds))
            {
                var labelBounds = new Rectangle(
                    element.Bounds.Location, 
                    new Size(element.Bounds.Width, LabelSizeWithMargin.Height));

                if (bounds.IntersectsWith(labelBounds))
                {
                    yield return new LayoutElement<Group>(labelBounds, element.Item);
                }
            }
        }

        public override Point AlignLocation(Point location, bool roundUp)
        {
            // clamp location to the grid area
            var size = GetSize();
            location.X = Math.Max(Math.Min(location.X, size.Width), 0);
            location.Y = Math.Max(Math.Min(location.Y, size.Height), 0);

            // align location
            var element = GetGroupAt(location);
            if (element == null)
            {
                return location;
            }
            
            var coords = ToGroupCoordinates(element, location);
            if (coords.Y < 0)
            {
                if (!roundUp)
                {
                    return new Point(
                        location.X,
                        Math.Max(element.Bounds.Y - CellSize.Height, 0));
                }

                coords.Y = 0;
            }

            var rowIndex = coords.Y / CellSizeWithMargin.Height;
            if (roundUp)
            {
                ++rowIndex;
            }

            var top = element.Bounds.Y + 
                      LabelSizeWithMargin.Height + 
                      rowIndex * CellSizeWithMargin.Height;
            if (roundUp)
            {
                top -= ItemMargin.Vertical;
            }
            
            return new Point(
                location.X, 
                top);
        }

        public override bool AreSameQueries(Rectangle queryA, Rectangle queryB)
        {
            // check if the queries are in different columns
            var columnA = GetColumnRange(queryA);
            var columnB = GetColumnRange(queryB);
            if (columnA.Min != columnB.Min || columnA.Max != columnB.Max)
            {
                return false;
            }

            // check if the queries are in different rows
            var topA = FindRowInGroup(new Point(0, queryA.Top), true);
            var topB = FindRowInGroup(new Point(0, queryB.Top), true);
            if (topA.Group?.Item != topB.Group?.Item || topA.Row != topB.Row)
            {
                return false;
            }

            var bottomA = FindRowInGroup(new Point(0, queryA.Bottom), false);
            var bottomB = FindRowInGroup(new Point(0, queryB.Bottom), false);
            return bottomA.Group?.Item == bottomB.Group?.Item && bottomA.Row == bottomB.Row;
        }

        /// <summary>
        /// Find row of <paramref name="location"/> is group at that location.
        /// </summary>
        /// <param name="location">Queries point</param>
        /// <param name="roundUp">
        /// If true and <paramref name="location"/> is in a gap between rows, the bottom row
        /// will be returned.
        /// </param>
        /// <returns></returns>
        private (LayoutElement<Group> Group, int Row) FindRowInGroup(Point location, bool roundUp)
        {
            var layoutSize = GetSize();
            location.X = location.X.Clamp(0, layoutSize.Width);
            location.Y = location.Y.Clamp(0, layoutSize.Height);

            var group = FindGroup(location);
            if (group == null)
            {
                return (null, -1);
            }

            // transform location to the group
            var groupLocation = ToGroupCoordinates(group, location);
            groupLocation.Y = groupLocation.Y.Clamp(0, group.Bounds.Height);

            var row = groupLocation.Y / CellSizeWithMargin.Height;
            if (roundUp && groupLocation.Y % CellSizeWithMargin.Height > CellSize.Height)
            {
                ++row;
            }

            return (group, row);
        }

        /// <summary>
        /// Find range of columns which are in <paramref name="bounds"/>.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private (int Min, int Max) GetColumnRange(Rectangle bounds)
        {
            var min = bounds.Left / CellSizeWithMargin.Width;
            var max = bounds.Right.RoundUpDiv(CellSizeWithMargin.Width);
            if (bounds.Left % CellSizeWithMargin.Width > CellSize.Width)
            {
                ++min; // the left corner is in a gap
            }

            if (bounds.Right % CellSizeWithMargin.Width > CellSize.Width)
            {
                --max; // the right corner is in a gap
            }

            return (min, max);
        }

        public override LayoutElement<EntityView> FindItem(EntityView source, Point direction)
        {
            var groupIndex = Groups.FindIndex(g => g.Items.Any(i => i.Data == source.Data));
            if (groupIndex < 0)
            {
                return null; // the source is not in the grid
            }

            var group = Groups[groupIndex];
            var sourceIndex = group.Items.FindIndex(i => i.Data == source.Data);
            var index = sourceIndex + ColumnCount * direction.Y + direction.X;

            // move up to the next group
            while ((index < 0 || group.View.IsCollapsed) && groupIndex > 0)
            {
                --groupIndex;
                group = Groups[groupIndex];

                if (!group.View.IsCollapsed)
                {
                    var gridItemCount = group.Items.Count.AlignUp(ColumnCount);
                    index = Math.Min(gridItemCount + index, group.Items.Count - 1);
                }
            }

            // move down to the next group
            while ((index >= group.Items.Count || group.View.IsCollapsed) && 
                   groupIndex + 1 < Groups.Count)
            {
                if (!group.View.IsCollapsed)
                {
                    var gridItemCount = group.Items.Count.AlignUp(ColumnCount);
                    index = index - gridItemCount;
                    if (index < 0)
                    {
                        index = group.Items.Count - 1;
                        break;
                    }
                }

                ++groupIndex;
                group = Groups[groupIndex];
            }

            if (index < 0 || index >= group.Items.Count)
            {
                return null;
            }

            var bounds = GetItemBounds(group, index);
            return new LayoutElement<EntityView>(bounds, group.Items[index]);
        }
    }
}
