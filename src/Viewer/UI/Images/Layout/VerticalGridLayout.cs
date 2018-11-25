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
                height -= ItemMargin.Vertical;
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

            var itemSizeWithMargin = new Size(
                ItemSize.Width + ItemMargin.Horizontal,
                ItemSize.Height + ItemMargin.Vertical);
            var column = localPoint.X / itemSizeWithMargin.Width;
            var row = localPoint.Y / itemSizeWithMargin.Height;

            if ((localPoint.X % itemSizeWithMargin.Width) > ItemSize.Width ||
                (localPoint.Y % itemSizeWithMargin.Height) > ItemSize.Height)
            {
                return null; // the location is in an empty space between items
            }

            return element.Item.Items[row * ColumnCount + column];
        }

        public override IEnumerable<LayoutElement<EntityView>> GetItemsIn(Rectangle bounds)
        {
            throw new NotImplementedException();
        }

        public override Group GetGroupLabelAt(Point location)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<LayoutElement<Group>> GetGroupsIn(Rectangle bounds)
        {
            throw new NotImplementedException();
        }
    }
}
