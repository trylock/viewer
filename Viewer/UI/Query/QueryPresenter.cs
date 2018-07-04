using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Viewer.Query;
using Viewer.UI.Explorer;

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
        private string _path;

        /// <summary>
        /// Full path to a file which contains this query or
        /// null if there is no such file.
        /// </summary>
        public string FullPath
        {
            get => _path;
            set
            {
                _path = value;
                if (_path != null)
                {
                    View.Text = Path.GetFileName(_path);
                }
            }
        }

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
            if (!_isUnsaved || FullPath == null)
            {
                return;
            }

            var path = FullPath;
            try
            {
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    var data = Encoding.UTF8.GetBytes(View.Query);
                    await stream.WriteAsync(data, 0, data.Length);
                }

                MarkSaved();
            }
            catch (UnauthorizedAccessException)
            {
                _dialogErrorView.UnauthorizedAccess(path);
            }
            catch (SecurityException)
            {
                _dialogErrorView.UnauthorizedAccess(path);
            }
            catch (DirectoryNotFoundException)
            {
                _dialogErrorView.FileNotFound(path);
            }
        }

        private void View_OpenQuery(object sender, OpenQueryEventArgs e)
        {
            _editor.OpenAsync(e.FullPath);
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
    }
}
