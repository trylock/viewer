using System;
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
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class DirectoryTreePresenter : Presenter<IDirectoryTreeView>
    {
        /// <summary>
        /// Directory with at least one of these flags will be hidden.
        /// </summary>
        public FileAttributes HideFlags { get; set; } = FileAttributes.Hidden;

        private readonly IQueryEvents _state;
        private readonly IQueryFactory _queryFactory;
        private readonly IFileSystem _fileSystem;
        private readonly ISystemExplorer _systemExplorer;
        private readonly IClipboardService _clipboard;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IExplorer _explorer;

        protected override ExportLifetimeContext<IDirectoryTreeView> ViewLifetime { get; }

        [ImportingConstructor]
        public DirectoryTreePresenter(
            ExportFactory<IDirectoryTreeView> viewFactory,
            IQueryEvents state,
            IQueryFactory queryFactory,
            ITaskLoader taskLoader,
            IFileSystemErrorView dialogView,
            IFileSystem fileSystem,
            ISystemExplorer systemExplorer,
            IExplorer explorer,
            IClipboardService clipboard)
        {
            _state = state;
            _queryFactory = queryFactory;
            _fileSystem = fileSystem;
            _systemExplorer = systemExplorer;
            _clipboard = clipboard;
            _dialogView = dialogView;
            _explorer = explorer;
            ViewLifetime = viewFactory.CreateExport();

            SubscribeTo(View, "View");
        }

        public void UpdateRootDirectories()
        {
            View.LoadDirectories(new string[] { }, GetRoots());
        }
        
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

        private IEnumerable<DirectoryView> GetValidSubdirectories(string fullPath)
        {
            var result = new List<DirectoryView>();
            try
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

                    result.Add(new DirectoryView
                    {
                        UserName = item.Name,
                        FileName = item.Name,
                        HasChildren = hasChildren
                    });
                }
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

            return result;
        }

        private async void View_ExpandDirectory(object sender, DirectoryEventArgs e)
        {
            View.BeginLoading();
            try
            {
                var directories = await Task.Run(() => GetValidSubdirectories(e.FullPath));
                var pathParts = PathUtils.Split(e.FullPath);
                View.LoadDirectories(pathParts, directories);
            }
            finally
            {
                View.EndLoading();
            }
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
                var newPath = Path.Combine(Path.GetDirectoryName(e.FullPath), e.NewName);
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
            _systemExplorer.OpenFile(e.FullPath);
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

            // update subdirectories in given path
            View.LoadDirectories(
                PathUtils.Split(destDir),
                GetValidSubdirectories(destDir));
        }
    }
}
