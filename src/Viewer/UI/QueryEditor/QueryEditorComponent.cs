using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.IO;
using Viewer.Properties;
using Viewer.Query;
using Viewer.UI.Explorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.QueryEditor
{
    [Export(typeof(IComponent))]
    public class QueryEditorComponent : IComponent
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IEditor _editor;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IQueryViewManager _queryViewManager;

        [ImportingConstructor]
        public QueryEditorComponent(
            IEditor editor, 
            IFileSystemErrorView dialogView,
            IQueryViewManager queryViewManager)
        {
            _editor = editor;
            _dialogView = dialogView;
            _queryViewManager = queryViewManager;
        }

        public void OnStartup(IViewerApplication app)
        {
            // load all query views and watch query view directory for changes
            var path = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(Settings.Default.QueryViewDirectoryPath)
            );

            try
            {
                _queryViewManager.LoadDirectory(path);
            }
            catch (ArgumentException)
            {
                _dialogView.InvalidFileName(path);
            }
            catch (DirectoryNotFoundException)
            {
                _dialogView.DirectoryNotFound(path);
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

            // add application menus
            app.AddMenuItem(new []{ "View", "Query" }, () => _editor.OpenNew(DockState.Document), Resources.QueryComponentIcon.ToBitmap());
            app.AddLayoutDeserializeCallback(Deserialize);
        }
        
        private IWindowView Deserialize(string persistString)
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
