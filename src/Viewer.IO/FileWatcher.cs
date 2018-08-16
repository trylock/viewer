using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.IO
{
    /// <inheritdoc />
    /// <summary>
    /// File system watcher watches file and directory changes within specified directory.
    /// Use the <see cref="M:Viewer.IO.IFileWatcher.Watch(System.String)" /> method to start watching a directory with this watcher.
    /// </summary>
    /// <see cref="FileSystemWatcher"/>
    public interface IFileWatcher : IDisposable
    {
        /// <summary>
        /// Event occurs when a file or directory in a <see cref="Watch"/>ed path is changed.
        /// </summary>
        event FileSystemEventHandler Changed;

        /// <summary>
        /// Event occurs when a file or directory in a <see cref="Watch"/>ed path is renamed.
        /// </summary>
        event RenamedEventHandler Renamed;

        /// <summary>
        /// Event occurs when a file or directory in a <see cref="Watch"/>ed path is deleted.
        /// </summary>
        event FileSystemEventHandler Deleted;

        /// <summary>
        /// Event occurs when a file or directory in a <see cref="Watch"/>ed path is created.
        /// </summary>
        event FileSystemEventHandler Created;

        /// <summary>
        /// Start watching changes in a directory <paramref name="path"/>.
        /// This method can be used concurrently from multiple thread.
        /// It must not be used concurrently with the Dispose method.
        /// </summary>
        /// <param name="path">Path to a directory</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> could not be found or it is invalid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> is too long.</exception>
        void Watch(string path);
    }

    public class FileWatcher : IFileWatcher
    {
        private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new ConcurrentDictionary<string, FileSystemWatcher>();

        public event FileSystemEventHandler Changed;
        public event RenamedEventHandler Renamed;
        public event FileSystemEventHandler Deleted;
        public event FileSystemEventHandler Created;

        public void Watch(string path)
        {
            var fullPath = PathUtils.UnifyPath(path);
            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.LastAccess |
                               NotifyFilters.LastWrite |
                               NotifyFilters.FileName |
                               NotifyFilters.DirectoryName
            };

            if (!_watchers.TryAdd(fullPath, watcher))
            {
                // this directory is being watched already
                watcher.Dispose();
            }
            else
            {
                // this call has added the watcher => initialize it
                watcher.Changed += WatcherOnChanged;
                watcher.Created += WatcherOnCreated;
                watcher.Deleted += WatcherOnDeleted;
                watcher.Renamed += WatcherOnRenamed;
                watcher.EnableRaisingEvents = true;
            }
        }

        private void WatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            Renamed?.Invoke(sender, e);
        }

        private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
        {
            Deleted?.Invoke(sender, e);
        }

        private void WatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            Created?.Invoke(sender, e);
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            Changed?.Invoke(sender, e);
        }

        public void Dispose()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Value.Changed -= WatcherOnChanged;
                watcher.Value.Created -= WatcherOnCreated;
                watcher.Value.Deleted -= WatcherOnDeleted;
                watcher.Value.Renamed -= WatcherOnRenamed;
                watcher.Value.EnableRaisingEvents = false;
                watcher.Value.Dispose();
            }
            _watchers.Clear();
        }
    }

    public interface IFileWatcherFactory
    {
        /// <summary>
        /// Create a new file watcher which does not watch any directory.
        /// </summary>
        /// <returns>New file watcher</returns>
        IFileWatcher Create();
    }

    [Export(typeof(IFileWatcherFactory))]
    public class FileWatcherFactory : IFileWatcherFactory
    {
        public IFileWatcher Create()
        {
            return new FileWatcher();
        }
    }
}
