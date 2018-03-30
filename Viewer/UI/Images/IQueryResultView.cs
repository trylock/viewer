using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;

namespace Viewer.UI.Images
{
    public class ResultItemView : IDisposable
    {
        /// <summary>
        /// Name of the file which should be shown to the user
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Path to a file
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Image representation of the file
        /// </summary>
        public Image Thumbnail { get; }

        public ResultItemView(string name, string path, Image thumbnail)
        {
            Name = name;
            Path = path;
            Thumbnail = thumbnail;
        }

        public void Dispose()
        {
            Thumbnail?.Dispose();
        }
    }

    public class FileMoveEventArgs : EventArgs
    {
        /// <summary>
        /// Full path to files to move
        /// </summary>
        public IEnumerable<string> FilePaths { get; }

        public FileMoveEventArgs(IEnumerable<string> filePaths)
        {
            FilePaths = filePaths;
        }
    }

    public class SelectionEventArgs : EventArgs
    {
        /// <summary>
        /// Set of items in selection
        /// </summary>
        public IEnumerable<int> Selection { get; }
        
        public SelectionEventArgs(IEnumerable<int> selection)
        {
            Selection = selection;
        }
    }

    public class ItemEventArgs : EventArgs
    {
        /// <summary>
        /// Index of items currently in selection
        /// </summary>
        public int Index { get; }

        public ItemEventArgs(int item)
        {
            Index = item;
        }
    }

    public class RenameItemEventArgs : ItemEventArgs
    {
        /// <summary>
        /// New name of the item
        /// </summary>
        public string NewName { get; }

        public RenameItemEventArgs(int index, string newName) : base(index)
        {
            NewName = newName;
        }
    }
    
    public interface IQueryResultView : IWindowView
    {
        /// <summary>
        /// Event called when a new selection starts.
        /// Selection is a list of items currently in selection.
        /// </summary>
        event EventHandler<SelectionEventArgs> SelectionStart;

        /// <summary>
        /// Event called when user modifies current selection.
        /// Selection is a list of items in the new selection.
        /// </summary>
        event EventHandler<SelectionEventArgs> SelectionChanged;

        /// <summary>
        /// Event called when user presses down a key
        /// </summary>
        event KeyEventHandler HandleKeyDown;

        /// <summary>
        /// Event called when user releases a key
        /// </summary>
        event KeyEventHandler HandleKeyUp;

        /// <summary>
        /// Event called when user requests to open an item in the result.
        /// </summary>
        event EventHandler<ItemEventArgs> OpenItem;

        /// <summary>
        /// Event called when user requests to rename item.
        /// </summary>
        event EventHandler<RenameItemEventArgs> RenameItem;

        /// <summary>
        /// Event called when user requests to delete items in the result.
        /// </summary>
        event EventHandler<SelectionEventArgs> DeleteItems;

        /// <summary>
        /// Show new items.
        /// </summary>
        /// <param name="items">List of items to show</param>
        void LoadItems(IEnumerable<ResultItemView> items);

        /// <summary>
        /// Set new item size
        /// </summary>
        /// <param name="itemSize">New item size</param>
        void SetItemSize(Size itemSize);

        /// <summary>
        /// Manually change user selection.
        /// Items currently in the selection will be removed from the selection.
        /// This won't trigger the SelectionChanged event
        /// </summary>
        /// <param name="items">Indicies of items in new selection</param>
        void SetItemsInSelection(IEnumerable<int> items);
    }
}
