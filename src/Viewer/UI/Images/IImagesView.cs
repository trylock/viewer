using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Data;
using Viewer.Properties;
using Viewer.Core.Collections;
using Viewer.Core.UI;

namespace Viewer.UI.Images
{
    public enum FileViewState
    {
        None,
        Active,
        Selected,
    }
    
    public sealed class EntityView : IDisposable
    {
        public string Name
        {
            get
            {
                if (Data is FileEntity)
                {
                    return Path.GetFileNameWithoutExtension(Data.Path);
                }

                return Path.GetFileName(Data.Path);
            }
        } 

        public string FullPath
        {
            get => Data.Path;
            set => Data.ChangePath(value);
        }
        public FileViewState State { get; set; } = FileViewState.None;
        public ILazyThumbnail Thumbnail { get; }
        public IEntity Data { get; }
        
        public EntityView(IEntity data, ILazyThumbnail thumbnail)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Thumbnail = thumbnail ?? throw new ArgumentNullException(nameof(thumbnail));
        }

        public void Dispose()
        {
            Thumbnail?.Dispose();
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

    /// <inheritdoc />
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
        /// Entity which should be renamed
        /// </summary>
        public EntityView Entity { get; }

        /// <summary>
        /// New name of the file (just the name without directory separators and file extension)
        /// </summary>
        public string NewName { get; }

        public RenameEventArgs(EntityView entity, string newName)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
            NewName = newName;
        }
    }

    public class EntityEventArgs : EventArgs
    {
        public EntityView Entity { get; }

        public EntityEventArgs(EntityView entity)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Arguments used in the <see cref="E:Viewer.UI.Images.IImagesView.OnDrop" /> event.
    /// </summary>
    public class DropEventArgs : EventArgs
    {
        /// <summary>
        /// Entity on which user dropped some items. This can be null.
        /// </summary>
        public EntityView Entity { get; }

        /// <summary>
        /// Allowed drop effect.
        /// </summary>
        public DragDropEffects AllowedEffect { get; }

        /// <summary>
        /// Dropped data. This will never be null.
        /// </summary>
        public IDataObject Data { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new drop event arguments.
        /// </summary>
        /// <param name="entity">Entity on which the data was dropped. This can be null.</param>
        /// <param name="allowedEffect">Allowed effect of the drop operation.</param>
        /// <param name="data">Data dropped on the entity</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is null</exception>
        public DropEventArgs(EntityView entity, DragDropEffects allowedEffect, IDataObject data)
        {
            Entity = entity;
            AllowedEffect = allowedEffect;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }
    }

    public interface IHistoryView
    {
        /// <summary>
        /// Event occurs when user wants to go back in query history.
        /// </summary>
        event EventHandler GoBackInHistory;

        /// <summary>
        /// Event occurs when user wantc to go forward in query history.
        /// </summary>
        event EventHandler GoForwardInHistory;
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

    /// <summary>
    /// View in which user can select items of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of the items in selection</typeparam>
    public interface ISelectionView<out T>
    {
        /// <summary>
        /// Event called when user starts a new range selection.
        /// </summary>
        event MouseEventHandler SelectionBegin;

        /// <summary>
        /// Event called when user end a range selection (i.e. releases LMB)
        /// </summary>
        event MouseEventHandler SelectionEnd;

        /// <summary>
        /// Event called when user moves with a mouse with active selection
        /// </summary>
        event MouseEventHandler SelectionDrag;

        /// <summary>
        /// Event called when user selects a single item with a mouse click.
        /// </summary>
        event EventHandler<EntityEventArgs> SelectItem;

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
        IEnumerable<T> GetItemsIn(Rectangle bounds);

        /// <summary>
        /// Get index of an item at <paramref name="location"/>.
        /// </summary>
        /// <param name="location">Query location</param>
        /// <returns>
        ///     Index of an item at <paramref name="location"/>.
        ///     If there is no item at given location, it will return -1.
        /// </returns>
        T GetItemAt(Point location);
    }

    public interface IImagesView : IWindowView, IPolledView, ISelectionView<EntityView>, IHistoryView
    {
        event KeyEventHandler HandleKeyDown;
        event KeyEventHandler HandleKeyUp;

        /// <summary>
        /// Event occurs when user moves cursor over an item.
        /// </summary>
        event EventHandler<EntityEventArgs> ItemHover;

        /// <summary>
        /// Event occurs when user requests to edit file name
        /// </summary>
        event EventHandler<EntityEventArgs> BeginEditItemName;

        /// <summary>
        /// Event occurs when user requests to cancel file name edit.
        /// </summary>
        event EventHandler CancelEditItemName;

        /// <summary>
        /// Event occurs when user begins to drag items in selection.
        /// </summary>
        event EventHandler BeginDragItems;

        /// <summary>
        /// Event occurs when user requests to rename file
        /// </summary>
        event EventHandler<RenameEventArgs> RenameItem;

        /// <summary>
        /// Event occurs when user requests to copy items in selection.
        /// </summary>
        event EventHandler CopyItems;

        /// <summary>
        /// Event occurs when user requests to cut items in selection.
        /// </summary>
        event EventHandler CutItems;

        /// <summary>
        /// Event called when user requests to delete items in selection.
        /// </summary>
        event EventHandler DeleteItems;

        /// <summary>
        /// Event occurs when user tries to open an item
        /// </summary>
        event EventHandler<EntityEventArgs> OpenItem;

        /// <summary>
        /// Event occurs when user drops omething into the view.
        /// </summary>
        event EventHandler<DropEventArgs> OnDrop;

        /// <summary>
        /// Event occurs when user tries to refresh current query.
        /// </summary>
        event EventHandler RefreshQuery;

        /// <summary>
        /// Textual representation of the query of this component
        /// </summary>
        string Query { get; set; }

        /// <summary>
        /// Context options
        /// </summary>
        IReadOnlyList<ExternalApplication> ContextOptions { get; set; }

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
        /// Begin drag&amp;drop operation.
        /// </summary>
        /// <param name="data">Data to drag</param>
        /// <param name="effect">Drop effect (e.g. copy, move)</param>
        void BeginDragDrop(IDataObject data, DragDropEffects effect);

        /// <summary>
        /// Show edit form for given item.
        /// </summary>
        /// <param name="entityView">Entity for which the edit form will be shown</param>
        void ShowItemEditForm(EntityView entityView);

        /// <summary>
        /// Hide item edit form.
        /// If no edit for is currently visilbe, this will be nop.
        /// </summary>
        void HideItemEditForm();
    }
}
