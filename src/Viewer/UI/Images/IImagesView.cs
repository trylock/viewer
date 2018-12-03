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
using Viewer.Core.UI;
using Viewer.Query;
using Viewer.UI.Images.Layout;

namespace Viewer.UI.Images
{
    internal class RenameEventArgs : EventArgs
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

    internal class EntityEventArgs : EventArgs
    {
        public EntityView Entity { get; }

        public EntityEventArgs(EntityView entity)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }

    internal class ProgramEventArgs : EventArgs
    {
        /// <summary>
        /// External application to run
        /// </summary>
        public ExternalApplication Program { get; }

        public ProgramEventArgs(ExternalApplication program)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Arguments used in the <see cref="E:Viewer.UI.Images.IImagesView.OnDrop" /> event.
    /// </summary>
    internal class DropEventArgs : EventArgs
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

    internal interface IPolledView
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
    /// View in which user can select items.
    /// </summary>
    internal interface ISelectionView : IWindowView
    {
        /// <summary>
        /// Event occurs whenever user releases a mouse button over this view
        /// </summary>
        event MouseEventHandler ProcessMouseUp;

        /// <summary>
        /// Event occurs whenever user presses a mouse button over this view
        /// </summary>
        event MouseEventHandler ProcessMouseDown;

        /// <summary>
        /// Event occurs whenever user moves with a mouse cursor over this view
        /// </summary>
        event MouseEventHandler ProcessMouseMove;

        /// <summary>
        /// Event occurs whenever mouse cursor leaves this view
        /// </summary>
        event EventHandler ProcessMouseLeave;

        /// <summary>
        /// Event occurs whenever user presses a keyboard key down
        /// </summary>
        event KeyEventHandler HandleKeyDown;

        /// <summary>
        /// Event occurs whenever user releases a keyboard key
        /// </summary>
        event KeyEventHandler HandleKeyUp;

        /// <summary>
        /// List of items to show 
        /// </summary>
        List<Group> Items { get; set; }

        /// <summary>
        /// Update all visible items in the <see cref="Items"/> collection.
        /// </summary>
        void UpdateItems();

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
        IEnumerable<EntityView> GetItemsIn(Rectangle bounds);

        /// <summary>
        /// Get index of an item at <paramref name="location"/>.
        /// </summary>
        /// <param name="location">Query location</param>
        /// <returns>
        ///     Index of an item at <paramref name="location"/>.
        ///     If there is no item at given location, it will return -1.
        /// </returns>
        EntityView GetItemAt(Point location);

        /// <summary>
        /// Find an item whose distance is <paramref name="delta"/> items from
        /// <paramref name="currentItem"/>.
        /// </summary>
        /// <param name="currentItem">Current item</param>
        /// <param name="delta">
        /// Distance from <paramref name="currentItem"/> (number of items in each dimension)
        /// </param>
        /// <returns>
        /// Item which is <paramref name="delta"/> items away from <paramref name="currentItem"/>
        /// or null if there is no such item.
        /// </returns>
        EntityView FindItem(EntityView currentItem, Point delta);

        /// <summary>
        /// Find the first visible item above <paramref name="currentItem"/>
        /// </summary>
        /// <param name="currentItem">Queried item</param>
        /// <returns>
        /// The first visible item which is directly above <paramref name="currentItem"/>
        /// </returns>
        EntityView FindFirstItemAbove(EntityView currentItem);

        /// <summary>
        /// Find the last visible item below <paramref name="currentItem"/>
        /// </summary>
        /// <param name="currentItem">Queried item</param>
        /// <returns>
        /// The last visible item which is directly below <paramref name="currentItem"/>
        /// </returns>
        EntityView FindLastItemBelow(EntityView currentItem);

        /// <summary>
        /// Make sure <paramref name="item"/> is visible. If it is fully visible, this won't do
        /// anything. Otherwise, it will scroll the view so that <paramref name="item"/> is visible
        /// </summary>
        /// <param name="item">Item which should be visible</param>
        void EnsureItemVisible(EntityView item);
    }

    internal interface IFileDropView
    {
        /// <summary>
        /// Event occurs when user drops something into the view.
        /// </summary>
        event EventHandler<DropEventArgs> OnDrop;

        /// <summary>
        /// Event occurs when user pastes clipboard into the view.
        /// </summary>
        event EventHandler OnPaste;

        /// <summary>
        /// Pick a directory from <paramref name="options"/>. If no option is selected, the
        /// returned task will throw <see cref="OperationCanceledException"/>
        /// </summary>
        /// <param name="options">Available options</param>
        /// <returns>
        /// Task which returns the selected option from <paramref name="options"/>. If user
        /// does not pick any directory, this task will be canceled (i.e., it will throw
        /// <see cref="OperationCanceledException"/>)
        /// </returns>
        Task<string> PickDirectoryAsync(IEnumerable<string> options);
    }
    
    internal interface IImagesView : IPolledView, ISelectionView, IFileDropView
    {
        /// <summary>
        /// Query history view
        /// </summary>
        IHistoryView History { get; }
        
        /// <summary>
        /// Event occurs when user requests to edit file name
        /// </summary>
        event EventHandler BeginEditItemName;

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
        event EventHandler OpenItem;

        /// <summary>
        /// Event occurs when user tries to refresh current query.
        /// </summary>
        event EventHandler RefreshQuery;

        /// <summary>
        /// Event occurs when user tries to open current query text in query editor.
        /// </summary>
        event EventHandler ShowQuery;

        /// <summary>
        /// Event occurs when user requests to run a program on current selection
        /// </summary>
        event EventHandler<ProgramEventArgs> RunProgram;

        /// <summary>
        /// Textual representation of the query of this component
        /// </summary>
        string Query { get; set; }

        /// <summary>
        /// Context options
        /// </summary>
        IReadOnlyList<ExternalApplication> ContextOptions { get; set; }

        /// <summary>
        /// Set an item size
        /// </summary>
        Size ItemSize { get; set; }

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
