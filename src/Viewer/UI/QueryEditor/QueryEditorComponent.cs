using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.IO;
using Viewer.Properties;
using Viewer.Query;
using Viewer.Query.Properties;
using Viewer.UI.Explorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.QueryEditor
{
    [Export(typeof(IComponent))]
    public class QueryEditorComponent : IComponent, IDisposable
    {
        private readonly IEditor _editor;
        private readonly IFileSystem _fileSystem;
        private readonly IFileWatcher _fileWatcher;
        private readonly IQueryViewRepository _queryViews;

        [ImportingConstructor]
        public QueryEditorComponent(
            IEditor editor, 
            IFileSystem fileSystem, 
            IQueryViewRepository queryViews, 
            IFileWatcherFactory fileWatcherFactory)
        {
            _editor = editor;
            _fileSystem = fileSystem;
            _queryViews = queryViews;
            _fileWatcher = fileWatcherFactory.Create();
            _fileWatcher.Created += FileWatcherOnCreated;
            _fileWatcher.Changed += FileWatcherOnChanged;
            _fileWatcher.Renamed += FileWatcherOnRenamed;
        }

        private void FileWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            LoadQueryViews();
        }

        private void FileWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            LoadQueryViews();
        }

        private void FileWatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            LoadQueryViews();
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddMenuItem(new []{ "View", "Query" }, () => _editor.OpenNew(DockState.Document), Resources.QueryComponentIcon.ToBitmap());
            app.AddLayoutDeserializeCallback(Deserialize);

            // load views and watch for 
            LoadQueryViews();

            try
            {
                _fileWatcher.Watch(Settings.Default.QueryViewDirectoryPath);
            }
            catch (ArgumentException)
            {
                // the directory does not exists 
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
                    return _editor.OpenNew(DockState.Unknown);
                }
                else if (path.Length == 0)
                {
                    var window = _editor.OpenNew(DockState.Unknown);
                    window.Query = content;
                    return window;
                }
                else 
                {
                    return _editor.Open(path, DockState.Unknown);
                }
            }
            return null;
        }

        public void Dispose()
        {
            _fileWatcher?.Dispose();
        }

        private void LoadQueryViews()
        {
            LoadQueryViews(Settings.Default.QueryViewDirectoryPath);
        }

        private void LoadQueryViews(string directoryPath)
        {
            try
            {
                var fullDirectoryPath = Path.GetFullPath(directoryPath);
                var views = _fileSystem
                    .EnumerateFiles(fullDirectoryPath, "*.vql")
                    .Select(LoadQueryView)
                    .Where(view => view != null);
                _queryViews.Replace(views);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        private QueryView LoadQueryView(string filePath)
        {
            try
            {
                var contents = _fileSystem.ReadAllText(filePath);
                var name = Path.GetFileNameWithoutExtension(filePath);
                return new QueryView(name, contents, filePath);
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (FileNotFoundException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (IOException)
            {
            }

            return null;
        }
    }
}
