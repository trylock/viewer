using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core.UI;
using Viewer.Query;
using Viewer.Query.Suggestions;
using Viewer.UI.Suggestions;

namespace Viewer.UI.QueryEditor
{
    internal class OpenQueryEventArgs : EventArgs
    {
        /// <summary>
        /// Full path to a file to open
        /// </summary>
        public string FullPath { get; }

        public OpenQueryEventArgs(string fullPath)
        {
            FullPath = fullPath;
        }
    }

    internal class QueryViewEventArgs : EventArgs
    {
        /// <summary>
        /// Query view argument.
        /// </summary>
        public QueryView View { get; }

        public QueryViewEventArgs(QueryView view)
        {
            View = view ?? throw new ArgumentNullException(nameof(view));
        }
    }

    internal interface IDropView
    {
        /// <summary>
        /// Event called when user drops something into the view
        /// </summary>
        event EventHandler<DragEventArgs> OnDrop;
    }

    internal interface IQuerySuggestionView
    {
        /// <summary>
        /// Event occurs whenever a suggestion from <see cref="Suggestions"/> is selected.
        /// </summary>
        event EventHandler<SuggestionEventArgs> SuggestionAccepted;

        /// <summary>
        /// User has requested to generate suggestions for current caret position (e.g. user
        /// has changed the caret position)
        /// </summary>
        event EventHandler SuggestionsRequested;

        /// <summary>
        /// Event occurs every k milliseconds
        /// </summary>
        event EventHandler Poll;

        /// <summary>
        /// Caret position in the editor
        /// </summary>
        int CaretPosition { get; set; }

        /// <summary>
        /// Suggestions shown at editor cursor
        /// </summary>
        IEnumerable<Suggestion> Suggestions { get; set; }
    }

    internal interface IQueryEditorView : IDropView, IQuerySuggestionView, IWindowView
    {
        /// <summary>
        /// Event called when user requests to run the query
        /// </summary>
        event EventHandler RunQuery;

        /// <summary>
        /// Event called when user changes the query
        /// </summary>
        event EventHandler QueryChanged;

        /// <summary>
        /// Event called when user tries to save the query to a file
        /// </summary>
        event EventHandler SaveQuery;

        /// <summary>
        /// Event called when user requests to open a new query
        /// </summary>
        event EventHandler<OpenQueryEventArgs> OpenQuery;

        /// <summary>
        /// Event occurs when user tries to open a query view.
        /// </summary>
        event EventHandler<QueryViewEventArgs> OpenQueryView;

        /// <summary>
        /// Available query views.
        /// </summary>
        ICollection<QueryView> Views { get; set; }

        /// <summary>
        /// Full path to a file which contains this query or
        /// null if there is no such file.
        /// </summary>
        string FullPath { get; set; }

        /// <summary>
        /// Input query
        /// </summary>
        string Query { get; set; }

        /// <summary>
        /// Prompts user to pick a file where the query should be saved
        /// </summary>
        /// <returns>Path to selected file or null if user did not select a file</returns>
        string PickFileForWrite();
    }
}
