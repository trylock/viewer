using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Data;

namespace Viewer.UI.Images.Layout
{
    internal class Group
    {
        public BaseValue Key { get; }

        public List<EntityView> Items { get; } = new List<EntityView>();

        public bool IsCollapsed { get; set; }

        public Group(BaseValue key)
        {
            Key = key;
        }
    }

    internal class LayoutElement<T>
    {
        public Rectangle Bounds { get; }

        public T Item { get; }

        public LayoutElement(Rectangle bounds, T item)
        {
            Bounds = bounds;
            Item = item;
        }
    }

    internal abstract class ImagesLayout
    {
        public SortedDictionary<BaseValue, Group> Groups { get; set; } = 
            new SortedDictionary<BaseValue, Group>();

        /// <summary>
        /// Size of the area for a thumbnail
        /// </summary>
        public Size ThumbnailAreaSize { get; set; }

        /// <summary>
        /// Compute size of the whole layout
        /// </summary>
        /// <returns></returns>
        public abstract Size GetSize();

        /// <summary>
        /// Find item at given location.
        /// </summary>
        /// <param name="location">Location in layout coordinates</param>
        /// <returns>Found item or null if there is none</returns>
        public abstract EntityView GetItemAt(Point location);

        /// <summary>
        /// Find all items in <paramref name="bounds"/>
        /// </summary>
        /// <remarks>
        /// For an item to be returned by this method, it has to be in <paramref name="bounds"/>
        /// at least partially.
        /// </remarks>
        /// <param name="bounds">Bounds in layout coordinates</param>
        /// <returns>All items in <paramref name="bounds"/></returns>
        public abstract IEnumerable<LayoutElement<EntityView>> GetItemsIn(Rectangle bounds);

        /// <summary>
        /// Find group label at given location.
        /// </summary>
        /// <param name="location">Location in layout coordinates</param>
        /// <returns>
        /// Group of the group label at <paramref name="location"/> or null if there is no
        /// group label at <paramref name="location"/>
        /// </returns>
        public abstract Group GetGroupLabelAt(Point location);

        /// <summary>
        /// Find all group in <paramref name="bounds"/>
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public abstract IEnumerable<LayoutElement<Group>> GetGroupsIn(Rectangle bounds);
    }
}
