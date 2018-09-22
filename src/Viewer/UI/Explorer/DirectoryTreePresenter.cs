using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.IO;
using Viewer.Properties;
using Viewer.Query;
using Viewer.UI.Tasks;

namespace Viewer.UI.Explorer
{
    internal class DirectoryTreePresenter : Presenter<IDirectoryTreeView>
    {
        /// <summary>
        /// Directory with at least one of these flags will be hidden.
        /// </summary>
        public FileAttributes HideFlags { get; set; } = FileAttributes.Hidden;

        private readonly IQueryHistory _state;
        private readonly IQueryFactory _queryFactory;
        private readonly IFileSystem _fileSystem;
        private readonly IClipboardService _clipboard;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IExplorer _explorer;
        
        public DirectoryTreePresenter(
            IDirectoryTreeView view,
            IQueryHistory state,
            IQueryFactory queryFactory,
            IFileSystemErrorView dialogView,
            IFileSystem fileSystem,
            IExplorer explorer,
            IClipboardService clipboard)
        {
            View = view;
            _state = state;
            _queryFactory = queryFactory;
            _fileSystem = fileSystem;
            _clipboard = clipboard;
            _dialogView = dialogView;
            _explorer = explorer;

            // update the view
            UpdateRootDirectories();
            SetCurrentQuery(_state.Current);

            // subscribe to events
            SubscribeTo(View, "View");
            _state.QueryExecuted += StateOnQueryExecuted;
        }

        private bool _isDisposed = false;

        public override void Dispose()
        {
            _isDisposed = true;
            _state.QueryExecuted -= StateOnQueryExecuted;
            base.Dispose();
        }
        
        private void StateOnQueryExecuted(object sender, QueryEventArgs e)
        {
            SetCurrentQuery(e.Query);
        }

        private async void SetCurrentQuery(IExecutableQuery query)
        {
            Reset();

            if (query == null)
            {
                return;
            }

            // highlight base directories of each pattern
            foreach (var pattern in query.Patterns)
            {
                var basePath = pattern.GetBasePath();
                if (basePath == null)
                    continue;

                await OpenAsync(basePath);

                if (_isDisposed)
                {
                    return;
                }

                View.HighlightDirectory(PathUtils.Split(basePath));
            }
        }

        #region Public Interface

        public void UpdateRootDirectories()
        {
            View.LoadDirectories(new string[] { }, GetRoots(), false);
        }
        
        public void Reset()
        {
            View.ResetHighlight();
        }

        private class Folder
        {
            /// <summary>
            /// Full path to the folder
            /// </summary>
            public string Path { get; }

            /// <summary>
            /// Subfolders
            /// </summary>
            public List<DirectoryView> Children { get; }

            public Folder(string path, List<DirectoryView> children)
            {
                Path = path;
                Children = children;
            }
        }

        public async Task OpenAsync(string path)
        {
            path = Path.GetFullPath(path);
            var parts = PathUtils.Split(path).ToArray();
            
            // find all subdirectories on the path
            var subdirectories = await Task.Run(() =>
            {
                var folders = new List<Folder>();
                var prefixPath = "";
                foreach (var part in parts)
                {
                    // Make sure there will be a directory separator after the drive letter colon.
                    // Otherwise, Path.Combine will produce a relative path "DriveLetter:File" which
                    // will most probably be incorrect.
                    prefixPath = Path.Combine(prefixPath, part) + Path.DirectorySeparatorChar;
                    try
                    {
                        var children = EnumerateValidSubdirectories(prefixPath).ToList();
                        var folder = new Folder(prefixPath, children);
                        folders.Add(folder);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        break;
                    }
                    catch (SecurityException)
                    {
                        break;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        break;
                    }
                }

                return folders;
            });

            if (_isDisposed)
            {
                return;
            }

            // update the view
            for (var i = 0; i < subdirectories.Count; ++i)
            {
                var folder = subdirectories[i];
                var pathParts = PathUtils.Split(folder.Path);
                var isNotLast = i + 1 < subdirectories.Count;
                View.LoadDirectories(pathParts, folder.Children, isNotLast);
            }
        }
        
        #endregion

        private IEnumerable<DirectoryView> GetRoots()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                    continue;

                var name = PathUtils.GetLastPart(drive.Name);

                if (!string.IsNullOrEmpty(drive.VolumeLabel))
                {
                    yield return new DirectoryView
                    {
                        UserName = drive.VolumeLabel + " (" + name + ")",
                        FileName = name,
                        HasChildren = true
                    };
                }
                else
                {
                    yield return new DirectoryView
                    {
                        UserName = name,
                        FileName = name,
                        HasChildren = true
                    };
                }
            }
        }

        private IEnumerable<DirectoryView> EnumerateValidSubdirectories(string fullPath)
        {
            var di = new DirectoryInfo(fullPath);
            foreach (var item in di.EnumerateDirectories())
            {
                if ((item.Attributes & HideFlags) != 0)
                    continue;

                // Find out if the subdirectory has any children.
                // If user is not authorized to read this directory, don't add 
                // the option to expand it in the view.
                var hasChildren = false;
                try
                {
                    hasChildren = item.EnumerateDirectories().Any();
                }
                catch (SecurityException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }

                yield return new DirectoryView
                {
                    UserName = item.Name,
                    FileName = item.Name,
                    HasChildren = hasChildren
                };
            }
        }

        private IEnumerable<DirectoryView> GetValidSubdirectories(string fullPath)
        {
            try
            {
                return EnumerateValidSubdirectories(fullPath);
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(fullPath);
            }
            catch (SecurityException)
            {
                _dialogView.UnauthorizedAccess(fullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _dialogView.DirectoryNotFound(fullPath);
            }

            return Enumerable.Empty<DirectoryView>();
        }

        private async void View_ExpandDirectory(object sender, DirectoryEventArgs e)
        {
            var directories = await Task.Run(() => GetValidSubdirectories(e.FullPath));
            if (_isDisposed)
            {
                return;
            }

            var pathParts = PathUtils.Split(e.FullPath);
            View.LoadDirectories(pathParts, directories, false);
        }

        private void View_OpenDirectory(object sender, DirectoryEventArgs e)
        {
            _state.ExecuteQuery(_queryFactory.CreateQuery(e.FullPath));
            View.SelectDirectory(PathUtils.Split(e.FullPath));
            View.EnsureVisible();
        }
        
        private void View_RenameDirectory(object sender, RenameDirectoryEventArgs e)
        {
            if (!PathUtils.IsValidFileName(e.NewName))
            {
                _dialogView.InvalidFileName(e.NewName);
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(e.FullPath);
                if (directory == null) // trying to rename root directory
                {
                    return;
                }
                var newPath = Path.Combine(directory, e.NewName);
                _fileSystem.MoveDirectory(e.FullPath, newPath);
                View.SetDirectory(PathUtils.Split(e.FullPath), new DirectoryView
                {
                    FileName = e.NewName,
                    UserName = e.NewName,
                });
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _dialogView.DirectoryNotFound(e.FullPath);
            }
        }
        
        private void View_DeleteDirectory(object sender, DirectoryEventArgs e)
        {
            if (!_dialogView.ConfirmDelete(e.FullPath))
            {
                return;
            }

            try
            {
                _fileSystem.DeleteDirectory(e.FullPath, true);
                View.RemoveDirectory(PathUtils.Split(e.FullPath));
            }
            catch (DirectoryNotFoundException)
            {
                _dialogView.DirectoryNotFound(e.FullPath);
                // we stil want to remove the directory from the view
                View.RemoveDirectory(PathUtils.Split(e.FullPath));
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(e.FullPath);
            }
            catch (IOException)
            {
                _dialogView.FileInUse(e.FullPath);
            }
        }
        
        private void View_CreateDirectory(object sender, DirectoryEventArgs e)
        {
            try
            {
                var newName = "New Folder";
                var directoryPath = Path.Combine(e.FullPath, newName);
                _fileSystem.CreateDirectory(directoryPath);
                View.AddDirectory(PathUtils.Split(e.FullPath), new DirectoryView
                {
                    UserName = newName,
                    FileName = newName
                });
                View.BeginEditDirectory(PathUtils.Split(directoryPath));
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _dialogView.DirectoryNotFound(e.FullPath);
            }
        }

        private void View_OpenInExplorer(object sender, DirectoryEventArgs e)
        {
            var fullPath = Path.GetFullPath(e.FullPath);
            Process.Start(
                Resources.ExplorerProcessName,
                string.Format(Resources.ExplorerOpenFolderArguments, fullPath)
            );
        }

        private void View_CopyDirectory(object sender, DirectoryEventArgs e)
        {
            try
            {
                _clipboard.SetFiles(new ClipboardFileDrop(new[] {e.FullPath}, DragDropEffects.Copy));
            }
            catch (ExternalException ex)
            {
                _dialogView.ClipboardIsBusy(ex.Message);
            }
        }
        
        private void View_PasteToDirectory(object sender, PasteEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (files == null)
            {
                return;
            }

            PasteFiles(e.FullPath, files, e.Effect);
        }

        private void View_PasteClipboardToDirectory(object sender, DirectoryEventArgs e)
        {
            try
            {
                var files = _clipboard.GetFiles();
                PasteFiles(e.FullPath, files, files.Effect);
            }
            catch (ExternalException ex)
            {
                _dialogView.ClipboardIsBusy(ex.Message);
            }
        }

        private async void PasteFiles(string destDir, IEnumerable<string> files, DragDropEffects effect)
        {
            try
            {
                if ((effect & DragDropEffects.Move) != 0)
                {
                    await _explorer.MoveFilesAsync(destDir, files);
                }
                else if ((effect & DragDropEffects.Copy) != 0)
                {
                    await _explorer.CopyFilesAsync(destDir, files);
                }
            }
            catch (OperationCanceledException)
            {
            }

            if (_isDisposed)
            {
                return;
            }

            // update subdirectories in given path
            View_ExpandDirectory(this, new DirectoryEventArgs(destDir));
        }
    }
}
