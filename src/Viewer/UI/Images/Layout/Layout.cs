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
    public class GroupView
    {
        /// <summary>
        /// true iff the group is collapsed
        /// </summary>
        public bool IsCollapsed { get; set; }

        /// <summary>
        /// Current location of this group
        /// </summary>
        public Point Location { get; set; }
    }

    public class Group : IDisposable, IComparable<Group>
    {
        /// <summary>
        /// Common kye of items in this group
        /// </summary>
        public BaseValue Key { get; }

        /// <summary>
        /// All items in this group
        /// </summary>
        public List<EntityView> Items { get; set; } = new List<EntityView>();

        /// <summary>
        /// State of the UI object
        /// </summary>
        public GroupView View { get; } = new GroupView();

        public Group(BaseValue key)
        {
            Key = key;
        }

        /// <summary>
        /// Create a new group with values from <paramref name="other"/>. The items collection
        /// is copied (only a shallow copy is created). The rest of the properties are shared.
        /// </summary>
        /// <param name="other"></param>
        public Group(Group other)
        {
            Key = other.Key;
            Items = new List<EntityView>(other.Items);
            View = other.View;
        }

        public void Dispose()
        {
            foreach (var item in Items)
            {
                item.Dispose();
            }
        }

        public int CompareTo(Group other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (ReferenceEquals(null, other))
                return 1;
            return Comparer<BaseValue>.Default.Compare(Key, other.Key);
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

    /// <summary>
    /// This class manages layout of a thumbnail grid. It maintains an efficient datastructure
    /// which allows for fast spatial range queries on the layout. This is necessary because 
    /// there can be thousands of photos and photo groups at a time.
    /// </summary>
    internal abstract class ImagesLayout
    {
        private List<Group> _groups = new List<Group>();

        public List<Group> Groups
        {
            get => _groups;
            set
            {
                _groups = value;
                OnLayoutChanged();
            }
        }

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

        private Rectangle _clientBounds;
        
        /// <summary>
        /// Bounding box of the layout area which is currently visible to user.
        /// </summary>
        public Rectangle ClientBounds
        {
            get => _clientBounds;
            set
            {
                var oldBounds = _clientBounds;
                _clientBounds = new Rectangle(
                    value.Location,
                    new Size(Math.Max(value.Width, 0), Math.Max(value.Height, 0))
                );

                // only update the layout if the bounds have changed
                if (_clientBounds != oldBounds)
                {
                    OnLayoutChanged();
                }
            }
        }

        /// <summary>
        /// Number of columns visible in the viewport
        /// </summary>
        public abstract int ViewportColumnCount { get; }

        /// <summary>
        /// Number of rows visible in the viewport
        /// </summary>
        public abstract int ViewportRowCount { get; }

        /// <summary>
        /// Negate the <see cref="GroupView.IsCollapsed"/> property.
        /// </summary>
        /// <param name="group"></param>
        public virtual void ToggleCollapse(Group group)
        {
            group.View.IsCollapsed = !group.View.IsCollapsed;
            OnLayoutChanged();
        }

        protected virtual void OnLayoutChanged()
        {
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
        public abstract LayoutElement<Group> GetGroupLabelAt(Point location);

        /// <summary>
        /// Find all group in <paramref name="bounds"/>
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public abstract IEnumerable<LayoutElement<Group>> GetGroupsIn(Rectangle bounds);

        /// <summary>
        /// Find a group at <paramref name="location"/>
        /// </summary>
        /// <param name="location"></param>
        /// <returns>Group with its bounding rectangle or null</returns>
        public abstract LayoutElement<Group> GetGroupAt(Point location);

        /// <summary>
        /// Find all group labels in <paramref name="bounds"/>.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public abstract IEnumerable<LayoutElement<Group>> GetGroupLabelsIn(Rectangle bounds);

        /// <summary>
        /// Move <paramref name="location"/> so that it is at the start of an element in this
        /// layout (e.g. group label, grid row etc.)
        /// </summary>
        /// <param name="location"></param>
        /// <param name="roundUp">true iff location should be rounded up</param>
        /// <returns></returns>
        public abstract Point AlignLocation(Point location, bool roundUp);

        /// <summary>
        /// Find item in <paramref name="direction"/> from <paramref name="source"/>
        /// </summary>
        /// <param name="source">Source item</param>
        /// <param name="direction">Move direction</param>
        /// <returns>
        /// Neighbor of <paramref name="source"/>. If no item is found at queries location,
        /// null will be returned.
        /// </returns>
        public abstract LayoutElement<EntityView> FindItem(EntityView source, Point direction);
    }
}
