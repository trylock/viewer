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
    public class ResultItem : IDisposable
    {
        /// <summary>
        /// Name of the file which should be shown to the user
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Image representation of the file
        /// </summary>
        public Image Thumbnail { get; }

        public ResultItem(string name, Image thumbnail)
        {
            Name = name;
            Thumbnail = thumbnail;
        }

        public void Dispose()
        {
            Thumbnail?.Dispose();
        }
    }

    public interface IQueryResultView : IView
    {
        /// <summary>
        /// Event called when selection has changed.
        /// New selection is available in the SelectionItems property.
        /// </summary>
        event EventHandler SelectionChanged;

        /// <summary>
        /// Event called when a user pressed a key.
        /// </summary>
        event EventHandler<KeyEventArgs> HandleShortcuts;

        /// <summary>
        /// Result items
        /// </summary>
        IReadOnlyList<ResultItem> Items { get; set; }
        
        /// <summary>
        /// Size of each item
        /// </summary>
        Size ItemSize { get; set; }

        /// <summary>
        /// List of indicies of selected items
        /// </summary>
        IEnumerable<int> SelectedItems { get; }

        /// <summary>
        /// Remove all items from selection
        /// </summary>
        void ClearSelection();

        /// <summary>
        /// Add given items to selection.
        /// Previous values will stay selected unless you call the ClearSelection method.
        /// </summary>
        /// <param name="items">List of items to add to selection</param>
        void AddToSelection(IEnumerable<int> items);
    }
}
