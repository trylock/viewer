using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.UI.Log
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

    public class LogEntry
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
    }

    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Log entry
        /// </summary>
        public LogEntry Entry { get; }

        public LogEventArgs(LogEntry entry)
        {
            Entry = entry;
        }
    }

    public interface ILogger
    {
        /// <summary>
        /// Add a log entry to the log collection
        /// </summary>
        /// <param name="entry"></param>
        void Add(LogEntry entry);
    }

    public interface ILog : ILogger, IEnumerable<LogEntry>
    {
        /// <summary>
        /// Event called when a new entry has been added
        /// </summary>
        event EventHandler<LogEventArgs> EntryAdded;

        /// <summary>
        /// Remove a log entry from the collection
        /// </summary>
        /// <param name="entry"></param>
        void Remove(LogEntry entry);

        /// <summary>
        /// Remove all log entries from the collection
        /// </summary>
        void Clear();
    }

    public class Log : ILog
    {
        private List<LogEntry> _entries = new List<LogEntry>();
        public event EventHandler<LogEventArgs> EntryAdded;

        public void Add(LogEntry entry)
        {
            lock (_entries)
            {
                _entries.Add(entry);
            }
            EntryAdded?.Invoke(this, new LogEventArgs(entry));
        }

        public void Remove(LogEntry entry)
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
        }

        public IEnumerator<LogEntry> GetEnumerator()
        {
            LogEntry[] buffer;
            lock (_entries)
            {
                buffer = new LogEntry[_entries.Count];
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
