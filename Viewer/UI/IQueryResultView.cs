using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI
{
    public class ResultItem
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
    }

    public interface IQueryResultView
    {
        /// <summary>
        /// Result items
        /// </summary>
        IReadOnlyList<ResultItem> Items { get; set; }

        /// <summary>
        /// List of indicies of selected items
        /// </summary>
        IEnumerable<int> SelectedItems { get; }

        /// <summary>
        /// Size of each item
        /// </summary>
        Size ItemSize { get; set; }

        /// <summary>
        /// Event called when selection has been changed.
        /// New selection is available in the SelectionItems property.
        /// </summary>
        event EventHandler SelectionChanged;

        /// <summary>
        /// Remove all items from selection
        /// </summary>
        void ClearSelection();
    }
}
