using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Viewer.IO;
using Viewer.UI.Explorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Query
{
    public interface IEditor
    {
        /// <summary>
        /// Open given file in the editor.
        /// If the file is opened already, the editor will just make its window visible to user.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <param name="dockState">Unknown value won't show the window</param>
        /// <returns>Task completed when the editor window opens</returns>
        Task<IQueryView> OpenAsync(string path, DockState dockState);

        /// <summary>
        /// Open given file in the editor.
        /// If the file is opened already, the editor will just make its window visible to user.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <param name="dockState">Unknown value won't show the window</param>
        /// <returns></returns>
        IQueryView Open(string path, DockState dockState);

        /// <summary>
        /// Open new empty editor window
        /// </summary>
        /// <param name="dockState">Unknown value won't show the window</param>
        /// <returns>Opened dock content</returns>
        IQueryView OpenNew(DockState dockState);

        /// <summary>
        /// Close all editor windows
        /// </summary>
        void CloseAll();
    }

    [Export(typeof(IEditor))]
    public class Editor :  IEditor
    {
        private readonly IFileSystemErrorView _dialogView;
        private readonly IFileSystem _fileSystem;
        private readonly ExportFactory<QueryPresenter> _editorFactory;

        [ImportingConstructor]
        public Editor(
            ExportFactory<QueryPresenter> editorFactory, 
            IFileSystem fileSystem,
            IFileSystemErrorView dialogView)
        {
            _dialogView = dialogView;
            _fileSystem = fileSystem;
            _editorFactory = editorFactory;
        }

        /// <summary>
        /// Opened editor windows
        /// </summary>
        private readonly List<ExportLifetimeContext<QueryPresenter>> _windows = new List<ExportLifetimeContext<QueryPresenter>>();

        public async Task<IQueryView> OpenAsync(string path, DockState dockState)
        {
            // don't open a new window, if an window with this file is opened already
            var window = FindWindow(path);
            if (window != null)
            {
                return window;
            }

            // otherwise load the file and show it in a new window
            try
            {
                var data = await _fileSystem.ReadToEndAsync(path);
                return OpenWindow(path, data, dockState);
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

        public IQueryView Open(string path, DockState dockState)
        {
            var window = FindWindow(path);
            if (window != null)
            {
                return window;
            }

            try
            {
                var data = Encoding.UTF8.GetString(_fileSystem.ReadAllBytes(path));
                return OpenWindow(path, data, dockState);
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

        public IQueryView OpenNew(DockState dockState)
        {
            return OpenWindow(dockState).View;
        }

        public void CloseAll()
        {
            foreach (var editor in _windows)
            {
                editor.Value.View.Close();
            }
        }

        private IQueryView FindWindow(string path)
        {
            var window = _windows.Find(editor =>
                StringComparer.CurrentCultureIgnoreCase.Compare(editor.Value.View.FullPath, path) == 0);
            if (window != null)
            {
                window.Value.View.EnsureVisible();
                return window.Value.View;
            }

            return null;
        }

        private IQueryView OpenWindow(string path, string content, DockState dockState)
        {
            var editor = OpenWindow(dockState);
            editor.SetContent(path, content);
            return editor.View;
        }
        
        private QueryPresenter OpenWindow(DockState dockState)
        {
            var editor = _editorFactory.CreateExport();
            if (dockState != DockState.Unknown)
            {
                editor.Value.ShowView("Query", dockState);
            }

            editor.Value.View.CloseView += (sender, args) =>
            {
                _windows.Remove(editor);
                editor.Dispose();
            };
            _windows.Add(editor);
            return editor.Value;
        }
    }
}
