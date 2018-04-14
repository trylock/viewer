using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Log
{
    public class RetryEventArgs : EventArgs
    {
        /// <summary>
        /// Entry to retry
        /// </summary>
        public LogEntry Entry { get; }

        public RetryEventArgs(LogEntry entry)
        {
            Entry = entry;
        }
    }

    public interface ILogView : IWindowView
    {
        event EventHandler<RetryEventArgs> Retry;

        /// <summary>
        /// Entries in the log
        /// </summary>
        IEnumerable<LogEntry> Entries { get; set; }

        /// <summary>
        /// Update all log entries in the view
        /// </summary>
        void UpdateEntries();
    }
}
