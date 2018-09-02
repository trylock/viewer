using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core.UI;

namespace Viewer.UI.Errors
{
    internal class RetryEventArgs : EventArgs
    {
        /// <summary>
        /// Entry to retry
        /// </summary>
        public ErrorListEntry Entry { get; }

        public RetryEventArgs(ErrorListEntry entry)
        {
            Entry = entry;
        }
    }

    internal interface IErrorListView : IWindowView
    {
        event EventHandler<RetryEventArgs> Retry;

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
