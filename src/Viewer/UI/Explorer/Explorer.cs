using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.IO;
using Viewer.Properties;
using Viewer.UI.Tasks;

namespace Viewer.UI.Explorer
{
    /// <summary>
    /// Explorer is component which handles long running I/O operations and provides a simple to
    /// use interface. It creates and manages all the necessary UI to show progress and errors.
    /// </summary>
    public interface IExplorer
    {
        /// <summary>
        /// Copy all files in the <paramref name="files"/> list to <paramref name="destinationDirectory"/>.
        /// User can cancel this operation. In that case, the returned task will throw
        /// <see cref="OperationCanceledException"/>.
        /// </summary>
        /// <param name="destinationDirectory">Directory where <paramref name="files"/> will be copied.</param>
        /// <param name="files">Copied files.</param>
        /// <returns>Task completed after the copy operation finishes.</returns>
        Task CopyFilesAsync(string destinationDirectory, IEnumerable<string> files);

        /// <summary>
        /// Move all files in the <paramref name="files"/> list to <paramref name="destinationDirectory"/>.
        /// User can cancel this operation. In that case, the returned task will throw
        /// <see cref="OperationCanceledException"/>.
        /// </summary>
        /// <param name="destinationDirectory">Directory where <paramref name="files"/> will be moved.</param>
        /// <param name="files">Moved files.</param>
        /// <returns>Task completed after the move operation finishes.</returns>
        Task MoveFilesAsync(string destinationDirectory, IEnumerable<string> files);
    }

    [Export(typeof(IExplorer))]
    public class Explorer : IExplorer
    {
        private readonly IFileSystem _fileSystem;
        private readonly IFileSystemErrorView _dialogView;
        private readonly ITaskLoader _taskLoader;

        [ImportingConstructor]
        public Explorer(IFileSystem fileSystem, IFileSystemErrorView dialogView, ITaskLoader taskLoader)
        {
            _fileSystem = fileSystem;
            _dialogView = dialogView;
            _taskLoader = taskLoader;
        }

        public Task CopyFilesAsync(string destinationDirectory, IEnumerable<string> files)
        {
            return CopyMoveFilesAsync(destinationDirectory, files, DragDropEffects.Copy);
        }

        public Task MoveFilesAsync(string destinationDirectory, IEnumerable<string> files)
        {
            return CopyMoveFilesAsync(destinationDirectory, files, DragDropEffects.Move);
        }
        
        private class CopyHandle
        {
            private readonly IFileSystem _fileSystem;
            private readonly IProgress<LoadingProgress> _progress;
            private readonly IFileSystemErrorView _dialogView;
            private readonly CancellationToken _cancellationToken;
            private readonly string _baseDir;
            private readonly string _destDir;

            public CopyHandle(
                IFileSystem fileSystem,
                string baseDir,
                string desDir,
                IProgress<LoadingProgress> progress,
                CancellationToken cancellationToken,
                IFileSystemErrorView dialogView)
            {
                _fileSystem = fileSystem;
                _baseDir = baseDir;
                _destDir = desDir;
                _dialogView = dialogView;
                _progress = progress;
                _cancellationToken = cancellationToken;
            }

            private string GetDestinationPath(string path)
            {
                var partialPath = path.Substring(_baseDir.Length + 1);
                return Path.Combine(_destDir, partialPath);
            }

            public SearchControl CreateDirectory(string path)
            {
                if (PathUtils.UnifyPath(_destDir) == PathUtils.UnifyPath(path))
                {
                    _dialogView.FailedToMove(path, _destDir);
                    return SearchControl.None;
                }
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
                _cancellationToken.ThrowIfCancellationRequested();

                var destPath = GetDestinationPath(path);
                _progress.Report(new LoadingProgress(path));
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

                return SearchControl.None;
            }
        }

        private async Task CopyMoveFilesAsync(string destinationDirectory, IEnumerable<string> files, DragDropEffects effect)
        {
            // copy files
            var fileCount = (int)_fileSystem.CountFiles(files, true);
            var cancellation = new CancellationTokenSource();
            var progress = _taskLoader.CreateLoader(Resources.CopyingFiles_Label, fileCount, cancellation);
            try
            {
                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        var baseDir = PathUtils.GetDirectoryPath(file);
                        var copy = new CopyHandle(
                            _fileSystem,
                            baseDir,
                            destinationDirectory,
                            progress,
                            cancellation.Token,
                            _dialogView);
                        if ((effect & DragDropEffects.Move) != 0)
                            _fileSystem.Search(file, copy.CreateDirectory, copy.MoveFile);
                        else
                            _fileSystem.Search(file, copy.CreateDirectory, copy.CopyFile);
                    }
                }, cancellation.Token);
            }
            finally
            {
                progress.Close();
            }
        }
    }
}
