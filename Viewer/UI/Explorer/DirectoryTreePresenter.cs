using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.IO;
using Viewer.Properties;
using Viewer.UI.Log;
using Viewer.UI.Tasks;

namespace Viewer.UI.Explorer
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class DirectoryTreePresenter : Presenter<IDirectoryTreeView>
    {
        public const string LogGroupName = "FileSystem";

        /// <summary>
        /// Directory with at least one of these flags will be hidden.
        /// </summary>
        public FileAttributes HideFlags { get; set; } = FileAttributes.Hidden;

        private readonly IApplicationState _state;
        private readonly IQueryEngine _queryEngine;
        private readonly IFileSystem _fileSystem;
        private readonly IClipboardService _clipboard;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IProgressViewFactory _progressViewFactory;

        protected override ExportLifetimeContext<IDirectoryTreeView> ViewLifetime { get; }

        [ImportingConstructor]
        public DirectoryTreePresenter(
            ExportFactory<IDirectoryTreeView> viewFactory,
            IApplicationState state,
            IQueryEngine queryEngine,
            IProgressViewFactory progressViewFactory,
            IFileSystemErrorView dialogView,
            IFileSystem fileSystem,
            IClipboardService clipboard)
        {
            _state = state;
            _queryEngine = queryEngine;
            _fileSystem = fileSystem;
            _clipboard = clipboard;
            _dialogView = dialogView;
            _progressViewFactory = progressViewFactory;
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
                View.LoadDirectories(PathUtils.Split(e.FullPath), directories);
            }
            finally
            {
                View.EndLoading();
            }
        }

        private void View_OpenDirectory(object sender, DirectoryEventArgs e)
        {
            _state.ExecuteQuery(_queryEngine.CreateQuery().Select(e.FullPath));
        }
        
        private void View_RenameDirectory(object sender, RenameDirectoryEventArgs e)
        {
            if (!PathUtils.IsValidFileName(e.NewName))
            {
                _dialogView.InvalidFileName(e.NewName, PathUtils.GetInvalidFileCharacters());
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

        private class CopyProgress
        {
            public string Path { get; }
            public bool IsFinished { get; }

            public CopyProgress(string path, bool isFinished)
            {
                Path = path;
                IsFinished = isFinished;
            }
        }

        private class CopyHandle
        {
            private readonly IFileSystem _fileSystem;
            private readonly IProgressView<CopyProgress> _progressView;
            private readonly IFileSystemErrorView _dialogView;
            private readonly string _baseDir;
            private readonly string _destDir;

            public CopyHandle(
                IFileSystem fileSystem, 
                string baseDir, 
                string desDir, 
                IProgressView<CopyProgress> progressView,
                IFileSystemErrorView dialogView)
            {
                _fileSystem = fileSystem;
                _baseDir = baseDir;
                _destDir = desDir;
                _dialogView = dialogView;
                _progressView = progressView;
            }
            
            private string GetDestinationPath(string path)
            {
                var partialPath = path.Substring(_baseDir.Length + 1);
                return Path.Combine(_destDir, partialPath);
            }
            
            public SearchControl CreateDirectory(string path)
            {
                var destDir = GetDestinationPath(path);
                _fileSystem.CreateDirectory(destDir);
                return SearchControl.Visit;
            }

            public SearchControl CopyFile(string path)
            {
                return Operation(path, (src, dest) => _fileSystem.CopyFile(src, dest));
            }

            public SearchControl MoveFile(string path)
            {
                return Operation(path, (src, dest) => _fileSystem.MoveFile(src, dest));
            }

            private SearchControl Operation(string path, Action<string, string> operation)
            {
                var destPath = GetDestinationPath(path);
                _progressView.Progress.Report(new CopyProgress(path, false));
                try
                {
                    operation(path, destPath);
                }
                catch (DirectoryNotFoundException)
                {
                    _dialogView.DirectoryNotFound(path);
                }
                catch (Exception e) when (e.GetType() == typeof(UnauthorizedAccessException) ||
                                          e.GetType() == typeof(SecurityException))
                {
                    _dialogView.UnauthorizedAccess(path);
                }
                finally
                {
                    _progressView.Progress.Report(new CopyProgress(path, true));
                }

                return SearchControl.None;
            }
        }

        private void PasteFiles(string destinationDirectory, IEnumerable<string> files, DragDropEffects effect)
        {
            // copy files
            var fileCount = (int)_fileSystem.CountFiles(files, true);
            _progressViewFactory
                .Create<CopyProgress>(copy => copy.IsFinished, copy => copy.Path)
                .WithTitle(Resources.CopyingFiles_Label)
                .WithWork(fileCount)
                .Show(view =>
                {
                    Task.Run(() =>
                    {
                        foreach (var file in files)
                        {
                            var baseDir = PathUtils.GetDirectoryPath(file);
                            var copy = new CopyHandle(_fileSystem, baseDir, destinationDirectory, view, _dialogView);
                            if ((effect & DragDropEffects.Move) != 0)
                                _fileSystem.Search(file, copy.CreateDirectory, copy.MoveFile);
                            else
                                _fileSystem.Search(file, copy.CreateDirectory, copy.CopyFile);
                        }
                    }, view.CancellationToken).ContinueWith(task =>
                    {
                        // update subdirectories in given path
                        View.LoadDirectories(
                            PathUtils.Split(destinationDirectory),
                            GetValidSubdirectories(destinationDirectory));
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                });
        }
    }
}
