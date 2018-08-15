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

        private IToolBarItem _openTool;
        private IToolBarItem _saveTool;
        private IToolBarItem _runTool;

        [ImportingConstructor]
        public QueryEditorComponent(
            IEditor editor, 
            IFileSystem fileSystem, 
            IQueryViewRepository queryViews, 
            IFileWatcherFactory fileWatcherFactory)
        {
            _editor = editor;
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddMenuItem(new []{ "View", "Query" }, () => _editor.OpenNew(DockState.Document), Resources.QueryComponentIcon.ToBitmap());

            _openTool = app.CreateToolBarItem("editor", "open", "Open Query", Resources.Open, OpenFileInEditor);
            _saveTool = app.CreateToolBarItem("editor", "save", "Save Query", Resources.Save, SaveCurrentEditor);
            _runTool = app.CreateToolBarItem("editor", "run", "Run Query", Resources.Start, RunCurrentEditor);
            
            app.AddLayoutDeserializeCallback(Deserialize);
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
                    return _editor.Open(path, DockState.Unknown).View;
                }
            }
            return null;
        }
    }
}
