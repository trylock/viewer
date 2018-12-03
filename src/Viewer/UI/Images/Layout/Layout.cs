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
    public class Group : IDisposable
    {
        public BaseValue Key { get; }

        public List<EntityView> Items { get; set; } = new List<EntityView>();

        public bool IsCollapsed { get; set; }

        public Group(BaseValue key)
        {
            Key = key;
        }

        public void Dispose()
        {
            foreach (var item in Items)
            {
                item.Dispose();
            }
            Items.Clear();
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
        /// Space around each item
        /// </summary>
        public Padding ItemMargin { get; set; }

        /// <summary>
        /// Inner space in each item
        /// </summary>
        public Padding ItemPadding { get; set; }

        /// <summary>
        /// Size of the label of each group
        /// </summary>
        public Size GroupLabelSize { get; set; }

        /// <summary>
        /// Space around group label
        /// </summary>
        public Padding GroupLabelMargin { get; set; }

        /// <summary>
        /// Size of the client area in which this layout is used
        /// </summary>
        /// <remarks>
        /// Layout can be larger than the client size. In that case, the client should draw
        /// scroll bars accordingly.
        /// </remarks>
        public Size ClientSize { get; private set; }

        public virtual void Resize(Size clientSize)
        {
            ClientSize = new Size(Math.Max(clientSize.Width, 0), Math.Max(clientSize.Height, 0));
        }

        /// <summary>
        /// Compute size of the whole layout
        /// </summary>
        /// <returns></returns>
        public abstract Size GetSize();

        /// <summary>
        /// Find item bounds 
        /// </summary>
        /// <param name="item"></param>
        /// <returns>
        /// <paramref name="item"/> bounds or an empty rectangle if <paramref name="item"/>
        /// has not been found.
        /// </returns>
        public abstract Rectangle GetItemBounds(EntityView item);

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
