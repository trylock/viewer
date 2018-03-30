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
        private IProgressView _progressView;

        public DirectoryTreePresenter(IDirectoryTreeView treeView, IProgressView progressView, IClipboardService clipboard)
        {
            _clipboard = clipboard;

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
        
        /// <summary>
        /// Get list of printable invalid file name characters in a string.
        /// </summary>
        /// <returns>String containing invalid file name characters separated by comma</returns>
        public string GetInvalidFileCharacters()
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in invalid)
            {
                if (char.IsControl(c) && !char.IsWhiteSpace(c))
                    continue;

                if (c == '\n')
                    sb.Append("\\n");
                else if (c == '\t')
                    sb.Append("\\t");
                else if (c == '\r')
                    sb.Append("\\r");
                else
                    sb.Append(c);
                sb.Append(", ");
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 3, 3); // remove the last separator
            }

            return sb.ToString();
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
                _treeView.UnauthorizedAccess(fullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _treeView.DirectoryNotFound(fullPath);
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
                e.Cancel();
                _treeView.InvalidFileName(e.NewName, GetInvalidFileCharacters());
                return;
            }

            try
            {
                DirectoryUtils.Rename(e.FullPath, e.NewName);
            }
            catch (UnauthorizedAccessException)
            {
                e.Cancel();
                _treeView.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                e.Cancel();
                _treeView.DirectoryNotFound(e.FullPath);
            }
        }
        
        private void View_DeleteDirectory(object sender, DirectoryEventArgs e)
        {
            if (!_treeView.ConfirmDelete(e.FullPath))
            {
                e.Cancel();
                return;
            }

            try
            {
                Directory.Delete(e.FullPath, true);
            }
            catch (DirectoryNotFoundException)
            {
                // we don't want to cancel the operation as the delete was technically successful
                _treeView.DirectoryNotFound(e.FullPath);
            }
            catch (UnauthorizedAccessException)
            {
                e.Cancel();
                _treeView.UnauthorizedAccess(e.FullPath);
            }
        }
        
        private void View_CreateDirectory(object sender, CreateDirectoryEventArgs e)
        {
            try
            {
                e.NewName = "New Folder";
                Directory.CreateDirectory(Path.Combine(e.FullPath, e.NewName));
            }
            catch (UnauthorizedAccessException)
            {
                e.Cancel();
                _treeView.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                e.Cancel();
                _treeView.DirectoryNotFound(e.FullPath);
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

            if (!PasteFiles(e.FullPath, files, e.Effect))
            {
                e.Cancel();
            }
        }

        private void View_PasteClipboardToDirectory(object sender, DirectoryEventArgs e)
        {
            PasteFiles(e.FullPath, _clipboard.GetFiles(), _clipboard.GetPreferredEffect());
        }

        private bool PasteFiles(string destinationDirectory, IEnumerable<string> files, DragDropEffects effect)
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
                _treeView.UnauthorizedAccess(destinationDirectory);
                return false;
            }

            // update subdirectories in given path
            _treeView.LoadDirectories(
                PathUtils.Split(destinationDirectory),
                GetValidSubdirectories(destinationDirectory));
            return true;
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
