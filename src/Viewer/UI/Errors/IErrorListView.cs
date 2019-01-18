using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core.UI;

namespace Viewer.UI.Errors
{
    internal class ErrorListEntryEventArgs : EventArgs
    {
        /// <summary>
        /// Entry to retry
        /// </summary>
        public ErrorListEntry Entry { get; }

        public ErrorListEntryEventArgs(ErrorListEntry entry)
        {
            Entry = entry;
        }
    }

    internal interface IErrorListView : IWindowView
    {
        /// <summary>
        /// Event occurs when user clicks on the retry button of an entry
        /// </summary>
        event EventHandler<ErrorListEntryEventArgs> Retry;

        /// <summary>
        /// Event occurs when user double clicks on an entry
        /// </summary>
        event EventHandler<ErrorListEntryEventArgs> ActivateEntry;

        /// <summary>
        /// Entries in the log
        /// </summary>
        IEnumerable<ErrorListEntry> Entries { get; set; }

        /// <summary>
        /// Update all log entries in the view
        /// </summary>
        void UpdateEntries();
    }
}
