using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Viewer.IO;
using Viewer.Query;

namespace Viewer.UI.QueryEditor
{
    public interface IQueryViewManager
    {
        /// <summary>
        /// Load all query views from directory at <paramref name="path"/> and watch this directory
        /// for any changes in query files.
        /// </summary>
        /// <param name="path"></param>
        void LoadDirectory(string path);
    }

    [Export(typeof(IQueryViewManager))]
    public class QueryViewManager : IQueryViewManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IQueryViewRepository _queryViews;
        private readonly IFileWatcher _fileWatcher;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// When a file system watcher detects a change in the query views directory, it fires an event.
        /// Since the file has been changed, it is possible that a process has the file locked. This is
        /// the time in milliseconds after which the event handler will try to open the changed file and
        /// apply changes to query views loaded in this application. 
        /// </summary>
        public static int UpdateDelay = 200;

        [ImportingConstructor]
        public QueryViewManager(
            IQueryViewRepository queryViews, 
            IFileSystem fileSystem, 
            IFileWatcherFactory fileWatcherFactory)
        {
            _queryViews = queryViews;
            _fileSystem = fileSystem;
            _fileWatcher = fileWatcherFactory.Create();
            _fileWatcher.Deleted += FileWatcherOnDeleted;
            _fileWatcher.Renamed += FileWatcherOnRenamed;
            _fileWatcher.Changed += FileWatcherOnChanged;
        }
        
        public void LoadDirectory(string path)
        {
            LoadQueryViews(path);
            _fileWatcher.Watch(path);
        }
        
        /// <summary>
        /// Try to load all query views from the query view directory (specified in user settings).
        /// If this operation fails, an error message is shown to the user.
        /// </summary>
        /// <param name="path">Path to the query views directory</param>
        private void LoadQueryViews(string path)
        {
            try
            {
                var files = _fileSystem.EnumerateFiles(path, "*.vql");
                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var content = _fileSystem.ReadAllText(file);
                    _queryViews.Add(new QueryView(name, content, file));
                }
            }
            catch (DirectoryNotFoundException)
            {
                _fileSystem.CreateDirectory(path);
            }
        }

        private static bool IsQueryFile(string path)
        {
            return Path.GetExtension(path)?.ToLowerInvariant() == ".vql";
        }

        private void FileWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            // remove old query view
            if (IsQueryFile(e.OldFullPath))
            {
                var name = Path.GetFileNameWithoutExtension(e.OldFullPath);
                _queryViews.Remove(name);
            }

            // add a new query view
            if (IsQueryFile(e.FullPath))
            {
                UpdateQueryView(e.FullPath);
            }
        }

        private void FileWatcherOnDeleted(object sender, FileSystemEventArgs e)
        {
            if (!IsQueryFile(e.FullPath))
            {
                return;
            }

            var name = Path.GetFileNameWithoutExtension(e.FullPath);
            _queryViews.Remove(name);
        }

        private async void FileWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (!IsQueryFile(e.FullPath))
            {
                return;
            }

            // This is necessary. The process which caused this event probably has the file opened.
            // Delaying the event handler will increase probability that we will successfully read the file.
            await Task.Delay(UpdateDelay);

            UpdateQueryView(e.FullPath);
        }

        private void UpdateQueryView(string filePath)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            try
            {
                var content = _fileSystem.ReadAllText(filePath);
                _queryViews.Remove(name);
                _queryViews.Add(new QueryView(name, content, filePath));
            }
            catch (SystemException ex)
            {
                // ignore I/O errors 
                Logger.Error(ex, "Failed to read Query View file.");
            }
        }
    }
}
