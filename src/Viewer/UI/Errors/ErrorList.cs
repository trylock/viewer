using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.UI.Errors
{
    public enum LogType
    {
        Error = 0,
        Warning,
        Message
    }

    /// <summary>
    /// Delegate called when user wants to retry a failed operation
    /// </summary>
    public delegate void Retry();

    public class ErrorListEntry
    {
        /// <summary>
        /// Type of the log entry
        /// </summary>
        public LogType Type { get; set; }

        /// <summary>
        /// Group name of the log entry
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Message of the log entry
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Retry operation of this log entry.
        /// It can be null.
        /// </summary>
        public Retry RetryOperation { get; set; }

        /// <summary>
        /// Line in an input where the error occurs
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Column in an input where the error occurs
        /// </summary>
        public int Column { get; set; }
    }

    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Log entry
        /// </summary>
        public ErrorListEntry Entry { get; }

        public LogEventArgs(ErrorListEntry entry)
        {
            Entry = entry;
        }
    }
    
    public interface IErrorList : IEnumerable<ErrorListEntry>
    {
        /// <summary>
        /// Event called when a new entry has been added
        /// </summary>
        event EventHandler<LogEventArgs> EntryAdded;

        /// <summary>
        /// Event called when the log is cleared
        /// </summary>
        event EventHandler EntriesRemoved;

        /// <summary>
        /// Add a log entry to the log collection
        /// </summary>
        /// <param name="entry"></param>
        void Add(ErrorListEntry entry);

        /// <summary>
        /// Remove a log entry from the collection
        /// </summary>
        /// <param name="entry"></param>
        void Remove(ErrorListEntry entry);

        /// <summary>
        /// Remove all log entries from the collection
        /// </summary>
        void Clear();
    }

    [Export(typeof(IErrorList))]
    public class ErrorList : IErrorList
    {
        private readonly List<ErrorListEntry> _entries = new List<ErrorListEntry>();

        public event EventHandler<LogEventArgs> EntryAdded;
        public event EventHandler EntriesRemoved;

        public void Add(ErrorListEntry entry)
        {
            lock (_entries)
            {
                _entries.Add(entry);
            }
            EntryAdded?.Invoke(this, new LogEventArgs(entry));
        }

        public void Remove(ErrorListEntry entry)
        {
            lock (_entries)
            {
                _entries.Remove(entry);
            }
        }

        public void Clear()
        {
            lock (_entries)
            {
                _entries.Clear();
            }
            EntriesRemoved?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerator<ErrorListEntry> GetEnumerator()
        {
            ErrorListEntry[] buffer;
            lock (_entries)
            {
                buffer = new ErrorListEntry[_entries.Count];
                _entries.CopyTo(buffer, 0);
            }

            foreach (var entry in buffer)
            {
                yield return entry;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
