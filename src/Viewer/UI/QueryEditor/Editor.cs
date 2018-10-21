using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.IO;
using Viewer.Properties;
using Viewer.Query;
using Viewer.Query.Suggestions;
using Viewer.UI.Explorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.QueryEditor
{
    public interface IEditor
    {
        /// <summary>
        /// Open given file in the editor.
        /// If the file is opened already, the editor will just make its window visible to user.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <returns>Task completed when the editor window opens</returns>
        Task<IWindowView> OpenAsync(string path);

        /// <summary>
        /// Open given file in the editor.
        /// If the file is opened already, the editor will just make its window visible to user.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <returns></returns>
        IWindowView Open(string path);

        /// <summary>
        /// Open new empty editor window
        /// </summary>
        /// <returns>Opened dock content</returns>
        IWindowView OpenNew();

        /// <summary>
        /// Open new empty editor window
        /// </summary>
        /// <param name="content">Content of the opened window</param>
        /// <returns>Opened dock content</returns>
        IWindowView OpenNew(string content);

        /// <summary>
        /// Save <paramref name="query"/> to <paramref name="path"/>.
        /// If the file exists already, it will be rewritten.
        /// </summary>
        /// <param name="path">Path to a file where to save given query</param>
        /// <param name="query">Query to save</param>
        /// <returns>Task finished when <paramref name="query"/> is written to <paramref name="path"/></returns>
        /// <exception cref="IOException">An IO error occurred</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid</exception>
        /// <exception cref="UnauthorizedAccessException">The access is not permitted</exception>
        Task SaveAsync(string path, string query);

        /// <summary>
        /// Close all editor windows
        /// </summary>
        void CloseAll();
    }

    [Export(typeof(IEditor))]
    public class Editor :  IEditor
    {
        private readonly IQueryHistory _queryHistory;
        private readonly IQueryCompiler _queryCompiler;
        private readonly IQueryErrorListener _queryErrorListener;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IQuerySuggestions _querySuggestions;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Opened editor windows
        /// </summary>
        private readonly List<QueryEditorPresenter> _windows = new List<QueryEditorPresenter>();
        
        [ImportingConstructor]
        public Editor(
            IFileSystem fileSystem,
            IFileSystemErrorView dialogView,
            IQueryHistory queryHistory,
            IQueryCompiler queryCompiler,
            IQueryErrorListener queryErrorListener,
            IQuerySuggestions querySuggestions)
        {
            _fileSystem = fileSystem;
            _dialogView = dialogView;
            _queryHistory = queryHistory;
            _queryCompiler = queryCompiler;
            _queryErrorListener = queryErrorListener;
            _querySuggestions = querySuggestions;
        }

        public async Task<IWindowView> OpenAsync(string path)
        {
            // don't open a new window, if a window with this file is opened already
            var window = FindWindow(path);
            if (window != null)
            {
                return window;
            }

            // otherwise load the file and show it in a new window
            try
            {
                var data = await _fileSystem.ReadToEndAsync(path);
                return OpenWindow(path, data);
            }
            catch (DirectoryNotFoundException e)
            {
                _dialogView.DirectoryNotFound(e.Message);
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
            catch (SecurityException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
            catch (FileNotFoundException)
            {
                _dialogView.FileNotFound(path);
            }

            return null;
        }

        public IWindowView Open(string path)
        {
            var window = FindWindow(path);
            if (window != null)
            {
                return window;
            }

            try
            {
                var data = Encoding.UTF8.GetString(_fileSystem.ReadAllBytes(path));
                return OpenWindow(path, data);
            }
            catch (DirectoryNotFoundException)
            {
                _dialogView.DirectoryNotFound(Path.GetDirectoryName(path));
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
            catch (SecurityException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
            catch (FileNotFoundException)
            {
                _dialogView.FileNotFound(path);
            }

            return null;
        }

        public IWindowView OpenNew()
        {
            return OpenWindow().View;
        }

        public IWindowView OpenNew(string content)
        {
            var window = OpenWindow();
            window.SetContent(null, content);
            return window.View;
        }

        public async Task SaveAsync(string path, string query)
        {
            var fullPath = Path.GetFullPath(path);
            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                var data = Encoding.UTF8.GetBytes(query);
                await stream.WriteAsync(data, 0, data.Length);
            }
        }

        public void CloseAll()
        {
            foreach (var editor in _windows)
            {
                editor.View.Close();
            }
        }

        private IWindowView FindWindow(string path)
        {
            var window = _windows.Find(editor =>
                StringComparer.CurrentCultureIgnoreCase.Compare(editor.View.FullPath, path) == 0);
            if (window != null)
            {
                window.View.EnsureVisible();
                return window.View;
            }

            return null;
        }

        private IWindowView OpenWindow(string path, string content)
        {
            var editor = OpenWindow();
            editor.SetContent(path, content);
            return editor.View;
        }
        
        private QueryEditorPresenter OpenWindow()
        {
            var editor = new QueryEditorPresenter(
                new QueryEditorView(), 
                _dialogView, 
                _queryHistory, 
                _queryCompiler, 
                _queryErrorListener, 
                _querySuggestions, 
                this);

            editor.View.CloseView += (sender, args) =>
            {
                _windows.Remove(editor);
                editor.Dispose();
            };
            _windows.Add(editor);
            return editor;
        }
    }
}
