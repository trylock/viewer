﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.Properties;

namespace Viewer.UI.Images
{
    public enum EntityViewState
    {
        None,
        Active,
        Selected,
    }

    public class EntityView : IDisposable
    {
        /// <summary>
        /// Name of the file which is shown to the user
        /// </summary>
        public string Name => Path.GetFileNameWithoutExtension(FullPath);

        /// <summary>
        /// Path to a file
        /// </summary>
        public string FullPath => Data.Path;

        /// <summary>
        /// Current state of the item
        /// </summary>
        public EntityViewState State { get; set; } = EntityViewState.None;

        /// <summary>
        /// Image representation of the file
        /// </summary>
        public Lazy<Image> Thumbnail { get; set; }

        /// <summary>
        /// Underlying entity
        /// </summary>
        public IEntity Data { get; set; }
        
        public EntityView(IEntity data, Lazy<Image> thumbnail)
        {
            Data = data;
            Thumbnail = thumbnail;
        }

        public void Dispose()
        {
            if (Thumbnail != null && Thumbnail.IsValueCreated)
            {
                Thumbnail.Value?.Dispose();
            }
        }
    }
    
    public class EntityViewPathComparer : IEqualityComparer<EntityView>
    {
        public bool Equals(EntityView x, EntityView y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.FullPath == y.FullPath;
        }

        public int GetHashCode(EntityView obj)
        {
            return obj.FullPath.GetHashCode();
        }
    }

    /// <summary>
    /// EntityView comparer which compares entity views by their underlying entity.
    /// </summary>
    public class EntityViewComparer : IComparer<EntityView>
    {
        private readonly IComparer<IEntity> _entityComparer;

        public EntityViewComparer(IComparer<IEntity> entityComparer)
        {
            _entityComparer = entityComparer;
        }

        public int Compare(EntityView x, EntityView y)
        {
            return _entityComparer.Compare(x.Data, y.Data);
        }
    }

    public class RenameEventArgs : EventArgs
    {
        /// <summary>
        /// New name of the file (just the name without directory separators and file extension)
        /// </summary>
        public string NewName { get; }

        public RenameEventArgs(string newName)
        {
            NewName = newName;
        }
    }

    public interface IPolledView
    {
        /// <summary>
        /// Event called once every k milliseconds.
        /// The rate of polling depends on the argument in the BeginPolling method.
        /// </summary>
        event EventHandler Poll;

        /// <summary>
        /// Start triggering the Poll event in regular intervals.
        /// Subsequent calls to this method will change the interval.
        /// </summary>
        /// <param name="delay">Minimal time in milliseconds between 2 poll events</param>
        void BeginPolling(int delay);

        /// <summary>
        /// Trigger one last Poll event instantaneously and stop triggering the event.
        /// </summary>
        void EndPolling();
    }

    public interface IThumbnailView
    {
        /// <summary>
        /// Event called when user changes the thumbnail size
        /// </summary>
        event EventHandler ThumbnailSizeChanged;

        /// <summary>
        /// Event called when user set the thumbnail size
        /// </summary>
        event EventHandler ThumbnailSizeCommit;

        /// <summary>
        /// Current scale of a thumbnail. It will always be in the [1, 2] internal.
        /// 1.0 is a minimal thumbnail size, 2.0 is a maximal thumbnail size
        /// </summary>
        double ThumbnailScale { get; set; }
    }

    public interface ISelectionView
    {
        /// <summary>
        /// Draw rectangular selection area.
        /// </summary>
        /// <param name="bounds">Area of the selection</param>
        void ShowSelection(Rectangle bounds);

        /// <summary>
        /// Hide current rectengular selection.
        /// </summary>
        void HideSelection();

        /// <summary>
        /// Get items in given rectangle.
        /// </summary>
        /// <param name="bounds">Query area</param>
        /// <returns>Indicies of items in this area</returns>
        IEnumerable<int> GetItemsIn(Rectangle bounds);

        /// <summary>
        /// Get index of an item at <paramref name="location"/>.
        /// </summary>
        /// <param name="location">Query location</param>
        /// <returns>
        ///     Index of an item at <paramref name="location"/>.
        ///     If there is no item at given location, it will return -1.
        /// </returns>
        int GetItemAt(Point location);
    }

    public interface IImagesView : IWindowView, IPolledView, IThumbnailView, ISelectionView
    {
        event MouseEventHandler HandleMouseDown;
        event MouseEventHandler HandleMouseUp;
        event MouseEventHandler HandleMouseMove;
        event KeyEventHandler HandleKeyDown;
        event KeyEventHandler HandleKeyUp;

        /// <summary>
        /// Event called when user requests to edit file name
        /// </summary>
        event EventHandler BeginEditItemName;

        /// <summary>
        /// Event called when user requests to cancel file name edit.
        /// </summary>
        event EventHandler CancelEditItemName;

        /// <summary>
        /// Event called when user requests to rename file
        /// </summary>
        event EventHandler<RenameEventArgs> RenameItem;

        /// <summary>
        /// Event called when user requests to copy items
        /// </summary>
        event EventHandler CopyItems;

        /// <summary>
        /// Event called when user requests to delete items
        /// </summary>
        event EventHandler DeleteItems;

        /// <summary>
        /// Event called when user tries to open an item
        /// </summary>
        event EventHandler OpenItem;
        
        /// <summary>
        /// List of items to show 
        /// </summary>
        SortedList<EntityView> Items { get; set; }

        /// <summary>
        /// Set an item size
        /// </summary>
        Size ItemSize { get; set; }
        
        /// <summary>
        /// Notify the view that the Items collection has changed.
        /// </summary>
        void UpdateItems();

        /// <summary>
        /// Update items in the view.
        /// </summary>
        /// <param name="itemIndices">Indicies of items to update</param>
        void UpdateItems(IEnumerable<int> itemIndices);
        
        /// <summary>
        /// Update a single item in the view.
        /// Noop, if the <paramref name="index"/> is not a valid index of any item.
        /// </summary>
        /// <param name="index">Index of an item</param>
        void UpdateItem(int index);

        /// <summary>
        /// Begin drag&amp;drop operation.
        /// </summary>
        /// <param name="data">Data to drag</param>
        /// <param name="effect">Drop effect (e.g. copy, move)</param>
        void BeginDragDrop(IDataObject data, DragDropEffects effect);

        /// <summary>
        /// Show edit form for given item.
        /// Noop, if <paramref name="index"/> is out of range.
        /// </summary>
        /// <param name="index">Index of an item</param>
        void ShowItemEditForm(int index);

        /// <summary>
        /// Hide item edit form.
        /// </summary>
        void HideItemEditForm();
    }
}
