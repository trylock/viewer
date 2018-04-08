using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;

namespace Viewer.UI.Images
{
    [Flags]
    public enum ResultItemState
    {
        None = 0x0,
        Active = 0x1,
        Selected = 0x2,
    }

    public class ResultItemView : IDisposable
    {
        /// <summary>
        /// Name of the file which should be shown to the user
        /// </summary>
        public string Name => Path.GetFileNameWithoutExtension(FullPath);

        /// <summary>
        /// Path to a file
        /// </summary>
        public string FullPath => Data.Path;

        /// <summary>
        /// Current state of the item
        /// </summary>
        public ResultItemState State { get; set; } = ResultItemState.None;

        /// <summary>
        /// Image representation of the file
        /// </summary>
        public Image Thumbnail { get; }

        public IEntity Data { get; set; }

        public ResultItemView(IEntity data, Image thumbnail)
        {
            Data = data;
            Thumbnail = thumbnail;
        }

        public void Dispose()
        {
            Thumbnail?.Dispose();
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

    public interface IImagesView : IWindowView
    {
        event MouseEventHandler HandleMouseDown;
        event MouseEventHandler HandleMouseUp;
        event MouseEventHandler HandleMouseMove;
        event EventHandler Resize;
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

        Size ItemSize { get; set; }
        Size ItemPadding { get; set; }

        /// <summary>
        /// List of items to show 
        /// </summary>
        IList<ResultItemView> Items { get; set; }

        /// <summary>
        /// Update gird size
        /// </summary>
        void UpdateSize();

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
        /// Draw rectangular selection area.
        /// </summary>
        /// <param name="bounds">Area of the selection</param>
        void ShowSelection(Rectangle bounds);

        /// <summary>
        /// Hide current rectengular selection.
        /// </summary>
        void HideSelection();

        /// <summary>
        /// Begin drag&amp;drop operation.
        /// </summary>
        /// <param name="data">Data to drag</param>
        /// <param name="effect">Drop effect (e.g. copy, move)</param>
        void BeginDragDrop(IDataObject data, DragDropEffects effect);

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
