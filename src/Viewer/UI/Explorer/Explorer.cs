using System;
using System.Collections;
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
using FileNotFoundException = System.IO.FileNotFoundException;

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
        private readonly ITaskLoader _taskLoader;
        private readonly IFileSystem _fileSystem;
        private readonly IFileSystemErrorView _dialogView;

        [ImportingConstructor]
        public Explorer(IFileSystem fileSystem, IFileSystemErrorView dialogView, ITaskLoader taskLoader)
        {
            _fileSystem = fileSystem;
            _dialogView = dialogView;
            _taskLoader = taskLoader;
        }

        public Task CopyFilesAsync(string destinationDirectory, IEnumerable<string> files)
        {
            return CopyMoveFilesAsync(destinationDirectory, files, false);
        }

        public Task MoveFilesAsync(string destinationDirectory, IEnumerable<string> files)
        {
            return CopyMoveFilesAsync(destinationDirectory, files, true);
        }

        public struct FileOperation
        {
            public string SourcePath { get; }
            public string DestinationPath { get; }
            public bool IsDirectory { get; }

            public FileOperation(string sourcePath, string destinationPath, bool isDirectory)
            {
                SourcePath = sourcePath;
                DestinationPath = destinationPath;
                IsDirectory = isDirectory;
            }
        }
        
        /// <summary>
        /// Find files to copy/move
        /// </summary>
        private class FileSearchListener : ISearchListener, IReadOnlyCollection<FileOperation>
        {
            private readonly string _destDir;
            private readonly CancellationToken _cancellationToken;
            private readonly Queue<FileOperation> _queue = new Queue<FileOperation>();

            public int Count => _queue.Count;

            public string BaseDirectory { get; set; }

            public FileSearchListener(string destDir, CancellationToken cancellation)
            {
                _destDir = destDir ?? throw new ArgumentNullException(nameof(destDir));
                _cancellationToken = cancellation;
            }

            public SearchControl OnDirectory(string path)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (string.Equals(
                    PathUtils.NormalizePath(_destDir),
                    PathUtils.NormalizePath(path),
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    return SearchControl.None;
                }

                var destDir = GetDestinationPath(path);
                _queue.Enqueue(new FileOperation(path, destDir, true));
                return SearchControl.Visit;
            }

            public SearchControl OnFile(string path)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var destPath = GetDestinationPath(path);
                _queue.Enqueue(new FileOperation(path, destPath, false));
                return SearchControl.Visit;
            }

            public IEnumerator<FileOperation> GetEnumerator()
            {
                return _queue.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private string GetDestinationPath(string path)
            {
                var partialPath = path
                    .Substring(BaseDirectory.Length)
                    .TrimStart(PathUtils.PathSeparators);
                return Path.Combine(_destDir, partialPath);
            }
        }

        private async Task CopyMoveFilesAsync(
            string destinationDirectory, 
            IEnumerable<string> files, 
            bool isMove)
        {
            // copy/move files
            var uiContext = SynchronizationContext.Current;
            if (uiContext == null)
            {
                throw new InvalidOperationException(
                    "Missing syncrhonization context. Call this function from the UI thread.");
            }

            var cancellation = new CancellationTokenSource();
            var progress = _taskLoader.CreateLoader(Resources.CopyingFiles_Label, cancellation);
            try
            {
                await Task.Run(() =>
                {
                    // find files to copy/move
                    var listener = new FileSearchListener(destinationDirectory, cancellation.Token);
                    foreach (var file in files)
                    {
                        listener.BaseDirectory = PathUtils.GetDirectoryPath(file);
                        _fileSystem.Search(file, listener);
                    }

                    // update total number of tasks
                    progress.TotalTaskCount = listener.Count;

                    // copy/move found files
                    foreach (var file in listener)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();

                        if (file.IsDirectory)
                        {
                            CreateDirectory(file.DestinationPath);
                        }
                        else
                        {
                            progress.Report(new LoadingProgress(file.SourcePath));
                            Process(file, isMove, uiContext, cancellation);
                        }
                    }
                }, cancellation.Token);
            }
            finally
            {
                progress.Close();
                cancellation.Dispose();
            }
        }

        private void CreateDirectory(string directory)
        {
            _fileSystem.CreateDirectory(directory);
        }

        private void Process(
            FileOperation file, 
            bool isMove, 
            SynchronizationContext uiContext, 
            CancellationTokenSource cancellation)
        {
            var retry = true;
            while (retry)
            {
                retry = false;
                try
                {
                    if (isMove)
                    {
                        _fileSystem.MoveFile(file.SourcePath, file.DestinationPath);
                    }
                    else
                    {
                        _fileSystem.CopyFile(file.SourcePath, file.DestinationPath);
                    }
                }
                catch (FileNotFoundException)
                {
                    // silently ignore this error, just don't copy/move the file
                }
                catch (DirectoryNotFoundException)
                {
                    // silently ignore this error, just don't copy/move the file
                }
                catch (IOException)
                {
                    var result = DialogResult.No;
                    uiContext.Send(_ =>
                    {
                        result = _dialogView.ConfirmReplace(file.DestinationPath);
                    }, null);

                    if (result == DialogResult.Yes)
                    {
                        retry = true;
                        _fileSystem.DeleteFile(file.DestinationPath);
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        cancellation.Cancel();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    uiContext.Send(_ => _dialogView.UnauthorizedAccess(file.SourcePath), null);
                }
                catch (SecurityException)
                {
                    uiContext.Send(_ => _dialogView.UnauthorizedAccess(file.SourcePath), null);
                }
            }
        }
    }
}
