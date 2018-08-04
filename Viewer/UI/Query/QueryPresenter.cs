using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Query;
using Viewer.UI.Explorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Query
{
    [Export]
    public class QueryPresenter : Presenter<IQueryView>
    {
        private readonly IApplicationState _appEvents;
        private readonly IFileSystemErrorView _dialogErrorView;
        private readonly IQueryCompiler _queryCompiler;
        private readonly IErrorListener _queryErrorListener;
        private readonly IEditor _editor;

        protected override ExportLifetimeContext<IQueryView> ViewLifetime { get; }

        private bool _isUnsaved = false;
        
        [ImportingConstructor]
        public QueryPresenter(
            ExportFactory<IQueryView> viewFactory, 
            IApplicationState appEvents, 
            IFileSystemErrorView dialogErrorView, 
            IQueryCompiler queryCompiler, 
            IErrorListener queryErrorListener,
            IEditor editor)
        {
            ViewLifetime = viewFactory.CreateExport();
            _dialogErrorView = dialogErrorView;
            _queryCompiler = queryCompiler;
            _queryErrorListener = queryErrorListener;
            _appEvents = appEvents;
            _editor = editor;

            SubscribeTo(View, "View");
        }

        /// <summary>
        /// Set content of this editor window
        /// </summary>
        /// <param name="path">Location of a file which contains the query or null</param>
        /// <param name="content">Query</param>
        public void SetContent(string path, string content)
        {
            View.FullPath = path;
            if (View.FullPath != null)
            {
                View.Text = Path.GetFileName(View.FullPath);
            }

            View.Query = content;
            MarkSaved();
        }

        /// <summary>
        /// Mark this file unsaved
        /// </summary>
        private void MarkUnsaved()
        {
            _isUnsaved = true;
            if (!View.Text.EndsWith("*"))
            {
                View.Text += '*';
            }
        }

        /// <summary>
        /// Inverse operation to MarkUnsaved
        /// </summary>
        private void MarkSaved()
        {
            _isUnsaved = false;
            if (View.Text.EndsWith("*"))
            {
                View.Text = View.Text.Substring(0, View.Text.Length - 1);
            }
        }
        
        private async void View_SaveQuery(object sender, EventArgs e)
        {
            if (!_isUnsaved)
            {
                return;
            }

            // this query is not saved in a file
            if (View.FullPath == null)
            {
                View.FullPath = View.PickFileForWrite();
                if (View.FullPath == null)
                {
                    // user has not picked a file
                    return;
                }
            }

            // save the query to its file
            try
            {
                using (var stream = new FileStream(View.FullPath, FileMode.Create, FileAccess.Write))
                {
                    var data = Encoding.UTF8.GetBytes(View.Query);
                    await stream.WriteAsync(data, 0, data.Length);
                }

                MarkSaved();
            }
            catch (UnauthorizedAccessException)
            {
                _dialogErrorView.UnauthorizedAccess(View.FullPath);
            }
            catch (SecurityException)
            {
                _dialogErrorView.UnauthorizedAccess(View.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _dialogErrorView.FileNotFound(View.FullPath);
            }
        }

        private void View_OpenQuery(object sender, OpenQueryEventArgs e)
        {
            _editor.OpenAsync(e.FullPath, DockState.Document);
        }

        private async void View_RunQuery(object sender, EventArgs e)
        {
            var input = View.Query;
            var query = await Task.Run(() => _queryCompiler.Compile(new StringReader(input), _queryErrorListener));
            if (query != null)
            {
                _appEvents.ExecuteQuery(query);
            }
        }

        private void View_QueryChanged(object sender, EventArgs e)
        {
            MarkUnsaved();
        }

        private async void View_OnDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLowerInvariant() == ".vql")
                {
                    await _editor.OpenAsync(file, DockState.Document);
                }
            }
        }
    }
}
