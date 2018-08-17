﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.IO;
using Viewer.Properties;
using Viewer.Query;
using Viewer.Query.Properties;
using Viewer.UI.Explorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.QueryEditor
{
    [Export(typeof(IComponent))]
    public class QueryEditorComponent : IComponent
    {
        private readonly IEditor _editor;
        private readonly IFileSystem _fileSystem;
        private readonly IFileWatcher _fileWatcher;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IQueryViewRepository _queryViews;

        private IToolBarItem _openTool;
        private IToolBarItem _saveTool;
        private IToolBarItem _runTool;
        private IToolBarDropDown _viewsDropDown;
        
        /// <summary>
        /// When a file system watcher detects a change in the query views directory, it fires an event.
        /// Since the file has been changed, it is possible that a process has the file locked. This is
        /// the time in milliseconds after which the event handler will try to open the changed file and
        /// apply changes to query views loaded in this application. 
        /// </summary>
        public static int UpdateDelay = 200;

        [ImportingConstructor]
        public QueryEditorComponent(
            IEditor editor, 
            IFileSystem fileSystem, 
            IQueryViewRepository queryViews, 
            IFileWatcherFactory fileWatcherFactory,
            IFileSystemErrorView dialogView)
        {
            _editor = editor;
            _fileSystem = fileSystem;
            _fileWatcher = fileWatcherFactory.Create();
            _queryViews = queryViews;
            _dialogView = dialogView;
        }

        public void OnStartup(IViewerApplication app)
        {
            // load all query views and watch query view directory for changes
            var queryViewsDirectory = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(Settings.Default.QueryViewDirectoryPath)
            );
            LoadQueryViews(queryViewsDirectory);
            WatchQueryViews(queryViewsDirectory);

            // add application menus
            app.AddMenuItem(new []{ "View", "Query" }, () => _editor.OpenNew(DockState.Document), Resources.QueryComponentIcon.ToBitmap());
            app.AddMenuItem(new []{ "File", "Open Query" }, OpenFileInEditor, Resources.Open);

            _openTool = app.CreateToolBarItem("editor", "open", "Open Query", Resources.Open, OpenFileInEditor);
            _viewsDropDown = app.CreateToolBarSelect("editor", "views", "Open Query View", Resources.View);
            _saveTool = app.CreateToolBarItem("editor", "save", "Save Query", Resources.Save, SaveCurrentEditor);
            _runTool = app.CreateToolBarItem("editor", "run", "Run Query", Resources.Start, RunCurrentEditor);
            _viewsDropDown.Items = GetQueryViewNames();
            _viewsDropDown.ItemSelected += ViewsDropDownOnItemSelected;

            app.AddLayoutDeserializeCallback(Deserialize);

            // register event handlers
            var context = SynchronizationContext.Current;
            _queryViews.Changed += (sender, args) => context.Post(state =>
            {
                _viewsDropDown.Items = GetQueryViewNames();
            }, null);
        }

        private ICollection<string> GetQueryViewNames()
        {
            var names = _queryViews.Select(item => item.Name).ToArray();
            Array.Sort(names);
            return names;
        }

        private static bool IsQueryFile(string path)
        {
            return Path.GetExtension(path)?.ToLowerInvariant() == ".vql";
        }

        private void ViewsDropDownOnItemSelected(object sender, SelectedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value))
            {
                return;
            }

            var view = _queryViews.Find(e.Value);
            if (view == null)
            {
                return;
            }

            _editor.OpenAsync(view.Path, DockState.Document);
        }

        private void WatchQueryViews(string path)
        {
            try
            {
                _fileWatcher.Watch(path);
            }
            catch (ArgumentException)
            {
                _dialogView.DirectoryNotFound(path);
            }
            catch (PathTooLongException)
            {
                _dialogView.PathTooLong(path);
            }
            _fileWatcher.Deleted += FileWatcherOnDeleted;
            _fileWatcher.Renamed += FileWatcherOnRenamed;
            _fileWatcher.Changed += FileWatcherOnChanged;
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
                var name = Path.GetFileNameWithoutExtension(e.FullPath);
                var query = _fileSystem.ReadAllText(e.FullPath);
                _queryViews.Add(new QueryView(name, query, e.FullPath));
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

            var name = Path.GetFileNameWithoutExtension(e.FullPath);
            try
            {
                var content = _fileSystem.ReadAllText(e.FullPath);
                _queryViews.Remove(name);
                _queryViews.Add(new QueryView(name, content, e.FullPath));
            }
            catch (SystemException)
            {
                // ignore I/O errors 
            }
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
            catch (ArgumentException)
            {
                _dialogView.InvalidFileName(path);
            }
            catch (DirectoryNotFoundException)
            {
                _fileSystem.CreateDirectory(path);
            }
            catch (PathTooLongException)
            {
                _dialogView.PathTooLong(path);
            }
            catch (IOException) // path is a file name
            {
                _dialogView.DirectoryNotFound(path);
            }
            catch (Exception e) when (e.GetType() == typeof(SecurityException) ||
                                      e.GetType() == typeof(UnauthorizedAccessException))
            {
                _dialogView.UnauthorizedAccess(path);
            }
        }

        /// <summary>
        /// Run query in current editor.
        /// This is nop if no query editor has focus.
        /// </summary>
        private void RunCurrentEditor()
        {
            _editor.Active?.RunAsync();
        }
        
        /// <summary>
        /// Save query to its file in current query editor.
        /// This is nop if no query editor has focus.
        /// </summary>
        private void SaveCurrentEditor()
        {
            _editor.Active?.SaveAsync();
        }

        /// <summary>
        /// Open a file dialog which lets user select a file to open in query editor.
        /// </summary>
        private void OpenFileInEditor()
        {
            using (var selector = new OpenFileDialog())
            {
                if (selector.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                _editor.OpenAsync(selector.FileName, DockState.Document);
            }
        }

        private IDockContent Deserialize(string persistString)
        {
            if (persistString.StartsWith(typeof(QueryEditorView).FullName))
            {
                var parts = persistString.Split(';');
                var content = parts.Length >= 2 ? parts[1] : "";
                var path = parts.Length >= 3 ? parts[2] : "";

                if (content.Length ==  0 && path.Length == 0)
                {
                    return _editor.OpenNew(DockState.Unknown).View;
                }
                else if (path.Length == 0)
                {
                    var window = _editor.OpenNew(DockState.Unknown).View;
                    window.Query = content;
                    return window;
                }
                else 
                {
                    return _editor.Open(path, DockState.Unknown)?.View;
                }
            }
            return null;
        }
    }
}
