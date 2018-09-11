using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Viewer.Query;

namespace Viewer.UI.Images
{
    internal class QueryHistoryItem
    {
        /// <summary>
        /// Query of this view
        /// </summary>
        public IQuery Query { get; }

        /// <summary>
        /// Textual representation of <see cref="Query"/> shown to the user.
        /// </summary>
        public string Text { get; }

        public QueryHistoryItem(IQuery query)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            Text = Regex.Replace(Query.Text, @"\r\n?|\n", " ");
        }

        public override string ToString()
        {
            return Text;
        }
    }

    internal class HistoryItemEventArgs : EventArgs
    {
        /// <summary>
        /// Text of the executed query
        /// </summary>
        public string Text { get; }

        public HistoryItemEventArgs(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }

    internal interface IHistoryView
    {
        /// <summary>
        /// ture iff user can go forward in query history
        /// </summary>
        bool CanGoForwardInHistory { get; set; }

        /// <summary>
        /// true iff user can go back in query history
        /// </summary>
        bool CanGoBackInHistory { get; set; }

        /// <summary>
        /// Event occurs when user wants to go back in query history.
        /// </summary>
        event EventHandler GoBackInHistory;

        /// <summary>
        /// Event occurs when user wantc to go forward in query history.
        /// </summary>
        event EventHandler GoForwardInHistory;

        /// <summary>
        /// Event occurs whenever user selects an item
        /// </summary>
        event EventHandler UserSelectedItem;

        /// <summary>
        /// Event occurs when user adds a new history item
        /// </summary>
        event EventHandler<HistoryItemEventArgs> ItemAdded;

        /// <summary>
        /// List of history items ordered from the oldest to the newest item.
        /// </summary>
        IReadOnlyList<QueryHistoryItem> Items { get; set; }

        /// <summary>
        /// Get or set currently selected item. null means no item is selected.
        /// </summary>
        QueryHistoryItem SelectedItem { get; set; }
    }
}
