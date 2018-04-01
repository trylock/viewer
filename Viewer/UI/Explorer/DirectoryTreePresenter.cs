using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.IO;
using Viewer.Properties;

namespace Viewer.UI.Explorer
{
    public class DirectoryTreePresenter
    {
        /// <summary>
        /// Directory with at least one of these flags will be hidden.
        /// </summary>
        public FileAttributes HideFlags { get; set; } = FileAttributes.Hidden;

        private IClipboardService _clipboard;
        private IDirectoryTreeView _treeView;
        private IFileSystemErrorView _errorView;
        private IProgressView _progressView;

        public DirectoryTreePresenter(
            IDirectoryTreeView treeView, 
            IProgressView progressView, 
            IFileSystemErrorView errorView,
            IClipboardService clipboard)
        {
            _clipboard = clipboard;

            _errorView = errorView;
            _progressView = progressView;
            _treeView = treeView;
            _treeView.ExpandDirectory += View_ExpandDirectory;
            _treeView.RenameDirectory += View_RenameDirectory;
            _treeView.DeleteDirectory += View_DeleteDirectory;
            _treeView.CreateDirectory += View_CreateDirectory;
            _treeView.OpenInExplorer += View_OpenInExplorer;
            _treeView.CopyDirectory += View_CopyDirectory;
            _treeView.PasteToDirectory += View_PasteToDirectory;
            _treeView.PasteClipboardToDirectory += View_PasteClipboardToDirectory;
        }

        public void UpdateRootDirectories()
        {
            _treeView.LoadDirectories(new string[] { }, GetRoots());
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
                result.AddRange(
                    from item in di.EnumerateDirectories()
                    where (item.Attributes & HideFlags) == 0
                    select new DirectoryView
                    {
                        UserName = item.Name,
                        FileName = item.Name,
                        HasChildren = item.EnumerateDirectories().Any()
                    });
            }
            catch (UnauthorizedAccessException)
            {
                _errorView.UnauthorizedAccess(fullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _errorView.DirectoryNotFound(fullPath);
            }

            return result;
        }

        private void View_ExpandDirectory(object sender, DirectoryEventArgs e)
        {
            _treeView.LoadDirectories(
                PathUtils.Split(e.FullPath), 
                GetValidSubdirectories(e.FullPath));
        }
        
        private void View_RenameDirectory(object sender, RenameDirectoryEventArgs e)
        {
            if (!PathUtils.IsValidFileName(e.NewName))
            {
                _errorView.InvalidFileName(e.NewName, PathUtils.GetInvalidFileCharacters());
                return;
            }

            try
            {
                DirectoryUtils.Rename(e.FullPath, e.NewName);
                _treeView.SetDirectory(PathUtils.Split(e.FullPath), new DirectoryView
                {
                    FileName = e.NewName,
                    UserName = e.NewName,
                });
            }
            catch (UnauthorizedAccessException)
            {
                _errorView.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _errorView.DirectoryNotFound(e.FullPath);
            }
        }
        
        private void View_DeleteDirectory(object sender, DirectoryEventArgs e)
        {
            if (!_errorView.ConfirmDelete(e.FullPath))
            {
                return;
            }

            try
            {
                Directory.Delete(e.FullPath, true);
                _treeView.RemoveDirectory(PathUtils.Split(e.FullPath));
            }
            catch (DirectoryNotFoundException)
            {
                _errorView.DirectoryNotFound(e.FullPath);
                // we stil want to remove the directory from the view
                _treeView.RemoveDirectory(PathUtils.Split(e.FullPath));
            }
            catch (UnauthorizedAccessException)
            {
                _errorView.UnauthorizedAccess(e.FullPath);
            }
        }
        
        private void View_CreateDirectory(object sender, CreateDirectoryEventArgs e)
        {
            try
            {
                e.NewName = "New Folder";
                var directoryPath = Path.Combine(e.FullPath, e.NewName);
                Directory.CreateDirectory(directoryPath);
                _treeView.AddDirectory(PathUtils.Split(e.FullPath), new DirectoryView
                {
                    UserName = e.NewName,
                    FileName = e.NewName
                });
                _treeView.BeginEditDirectory(PathUtils.Split(directoryPath));
            }
            catch (UnauthorizedAccessException)
            {
                _errorView.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _errorView.DirectoryNotFound(e.FullPath);
            }
        }

        private void View_OpenInExplorer(object sender, DirectoryEventArgs e)
        {
            Process.Start(
                Resources.ExplorerProcessName,
                string.Format(Resources.ExplorerOpenFolderArguments, e.FullPath));
        }

        private void View_CopyDirectory(object sender, DirectoryEventArgs e)
        {
            _clipboard.SetFiles(new[] { e.FullPath });
            _clipboard.SetPreferredEffect(DragDropEffects.Copy);
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
            PasteFiles(e.FullPath, _clipboard.GetFiles(), _clipboard.GetPreferredEffect());
        }

        private void PasteFiles(string destinationDirectory, IEnumerable<string> files, DragDropEffects effect)
        {
            try
            {
                // copy/move all files in the clipboard
                foreach (var source in files)
                {
                    var target = Path.Combine(destinationDirectory, PathUtils.GetLastPart(source));

                    if ((effect & DragDropEffects.Move) != 0)
                    {
                        if (File.Exists(source))
                        {
                            File.Move(source, target);
                        }
                        else
                        {
                            Directory.Move(source, target);
                        }
                    }
                    else if ((effect & DragDropEffects.Copy) != 0)
                    {
                        if (File.Exists(source))
                        {
                            File.Copy(source, target);
                        }
                        else
                        {
                            CopyDirectory(source, target);
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                // a directory in the data was deleted
                // ignore the event 
            }
            catch (FileNotFoundException)
            {
                // a file in the data was deleted
                // ignore the event 
            }
            catch (UnauthorizedAccessException)
            {
                _errorView.UnauthorizedAccess(destinationDirectory);
            }

            // update subdirectories in given path
            _treeView.LoadDirectories(
                PathUtils.Split(destinationDirectory),
                GetValidSubdirectories(destinationDirectory));
        }
        
        private void CopyDirectory(string source, string target)
        {
            var filesCount = (int)DirectoryUtils.CountFiles(source, true);
            var isCanceled = false;
            _progressView.CancelProgress += (o, args) => isCanceled = true;
            _progressView.Show(Resources.CopyingFiles_Label, filesCount, () => {
                DirectoryUtils.Copy(source, target, true,
                    file =>
                    {
                        _progressView.StartWork(file);
                        return !isCanceled;
                    });
                if (isCanceled)
                {
                    _progressView.Hide();
                }
                else
                {
                    _progressView.Finish();
                }
            });

            if (isCanceled)
            {
                Directory.Delete(target, true);
            }
        }
    }
}
