using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.IO;
using Viewer.UI.Explorer;
using Viewer.UI.Tasks;

namespace ViewerTest.UI.Explorer
{
    [TestClass]
    public class ExplorerTest
    {
        private Mock<IFileSystem> _fileSystem;
        private Mock<IFileSystemErrorView> _dialogView;
        private Mock<ITaskLoader> _taskLoader;
        private Mock<IProgressController> _progressController;
        private Viewer.UI.Explorer.Explorer _explorer;

        [TestInitialize]
        public void Setup()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            _fileSystem = new Mock<IFileSystem>();
            _dialogView = new Mock<IFileSystemErrorView>();
            _progressController = new Mock<IProgressController>();
            _taskLoader = new Mock<ITaskLoader>();
            _taskLoader
                .Setup(mock => mock.CreateLoader(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(_progressController.Object);
            _explorer = new Viewer.UI.Explorer.Explorer(_fileSystem.Object, _dialogView.Object, _taskLoader.Object);
        }

        /// <summary>
        /// Use this in Verify to compare paths. It will normalize directory separators.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string CheckPath(string path)
        {
            path = PathUtils.NormalizePath(path);
            return It.Is<string>(str => PathUtils.NormalizePath(str) == path);
        }

        [TestMethod]
        public async Task CopyFileAsync_CopySingleFile()
        {
            _fileSystem
                .Setup(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/b.txt");
                });

            await _explorer.CopyFilesAsync("dest/dir", new[] { "src/b.txt" });

            _fileSystem.Verify(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/b.txt"), 
                CheckPath("dest/dir/b.txt")), Times.Once);
            _fileSystem.VerifyNoOtherCalls();

            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        public async Task CopyFileAsync_CopySingleEmptyDirectory()
        {
            _fileSystem
                .Setup(mock => mock.Search("src", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnDirectory("src");
                });

            await _explorer.CopyFilesAsync("dest/dir", new[] { "src" });

            _fileSystem.Verify(mock => mock.Search("src", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.CreateDirectory(CheckPath("dest/dir/src")), Times.Once);
            _fileSystem.VerifyNoOtherCalls();

            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        public async Task CopyFileAsync_CopyDirectoryToItself()
        {
            _fileSystem
                .Setup(mock => mock.Search("dest", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    var result = listener.OnDirectory("dest");
                    if (result == SearchControl.Visit)
                    {
                        listener.OnFile("dest/a.txt");
                    }
                });

            await _explorer.CopyFilesAsync("dest", new[] {"dest"});

            _fileSystem.Verify(mock => mock.Search("dest", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.CreateDirectory(CheckPath("dest")), Times.Never);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("dest/a.txt"),
                CheckPath("dest/a.txt")), Times.Never);
            _fileSystem.VerifyNoOtherCalls();

            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        public async Task CopyFileAsync_UnauthorizedAccessToAFile()
        {
            _fileSystem
                .Setup(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/a.txt");
                });
            _fileSystem
                .Setup(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/b.txt");
                });
            _fileSystem
                .Setup(mock => mock.CopyFile("src/a.txt", It.IsAny<string>()))
                .Throws(new UnauthorizedAccessException());

            await _explorer.CopyFilesAsync("dest", new[] { "src/a.txt", "src/b.txt" });

            _fileSystem.Verify(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/a.txt"), 
                CheckPath("dest/a.txt")), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/b.txt"),
                CheckPath("dest/b.txt")), Times.Once);
            _fileSystem.VerifyNoOtherCalls();

            _dialogView.Verify(mock => mock.UnauthorizedAccess(CheckPath("src/a.txt")), Times.Once);
            _dialogView.VerifyNoOtherCalls();

            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        public async Task CopyFileAsync_DirectoryNotFound()
        {
            _fileSystem
                .Setup(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/a.txt");
                });
            _fileSystem
                .Setup(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/b.txt");
                });
            _fileSystem
                .Setup(mock => mock.CopyFile("src/a.txt", It.IsAny<string>()))
                .Throws(new DirectoryNotFoundException());

            await _explorer.CopyFilesAsync("dest", new[] { "src/a.txt", "src/b.txt" });

            _fileSystem.Verify(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/a.txt"),
                CheckPath("dest/a.txt")), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/b.txt"),
                CheckPath("dest/b.txt")), Times.Once);
            _fileSystem.VerifyNoOtherCalls();
            
            _dialogView.VerifyNoOtherCalls();

            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        public async Task CopyFileAsync_FileNotFound()
        {
            _fileSystem
                .Setup(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/a.txt");
                });
            _fileSystem
                .Setup(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/b.txt");
                });
            _fileSystem
                .Setup(mock => mock.CopyFile("src/a.txt", It.IsAny<string>()))
                .Throws(new FileNotFoundException());

            await _explorer.CopyFilesAsync("dest", new[] { "src/a.txt", "src/b.txt" });

            _fileSystem.Verify(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/a.txt"),
                CheckPath("dest/a.txt")), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/b.txt"),
                CheckPath("dest/b.txt")), Times.Once);
            _fileSystem.VerifyNoOtherCalls();

            _dialogView.VerifyNoOtherCalls();

            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        public async Task CopyFileAsync_ReportProgress()
        {
            _fileSystem
                .Setup(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/a.txt");
                });
            _fileSystem
                .Setup(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/b.txt");
                });

            await _explorer.CopyFilesAsync("dest", new[] { "src/a.txt", "src/b.txt" });

            _fileSystem.Verify(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/a.txt"),
                CheckPath("dest/a.txt")), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/b.txt"),
                CheckPath("dest/b.txt")), Times.Once);
            _fileSystem.VerifyNoOtherCalls();

            _dialogView.VerifyNoOtherCalls();

            _progressController.VerifySet(mock => mock.TotalTaskCount = 2, Times.Once);
            _progressController.Verify(mock => mock.Report(It.Is<ILoadingProgress>(progress => 
                progress.Message == "src/a.txt")), Times.Once);
            _progressController.Verify(mock => mock.Report(It.Is<ILoadingProgress>(progress =>
                progress.Message == "src/b.txt")), Times.Once);
            _progressController.Verify(mock => mock.Close(), Times.Once);
            _progressController.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task CopyFileAsync_ReplaceAnExistingFile()
        {
            _fileSystem
                .Setup(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/a.txt");
                });
            _fileSystem
                .SetupSequence(mock => mock.CopyFile("src/a.txt", It.IsAny<string>()))
                .Throws(new IOException())
                .Pass();
            _dialogView
                .Setup(mock => mock.ConfirmReplace(CheckPath("dest/a.txt")))
                .Returns(DialogResult.Yes);

            await _explorer.CopyFilesAsync("dest", new[] { "src/a.txt" });

            _fileSystem.Verify(mock => mock.DeleteFile(CheckPath("dest/a.txt")), Times.Once);
            _fileSystem.Verify(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/a.txt"),
                CheckPath("dest/a.txt")), Times.Exactly(2));
            _fileSystem.VerifyNoOtherCalls();

            _dialogView.Verify(mock => mock.ConfirmReplace(CheckPath("dest/a.txt")), Times.Once);
            _dialogView.VerifyNoOtherCalls();

            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        public async Task CopyFileAsync_SkipAnExistingFile()
        {
            _fileSystem
                .Setup(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/a.txt");
                });
            _fileSystem
                .Setup(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/b.txt");
                });
            _fileSystem
                .Setup(mock => mock.CopyFile("src/a.txt", It.IsAny<string>()))
                .Throws(new IOException());
            _dialogView
                .Setup(mock => mock.ConfirmReplace(CheckPath("dest/a.txt")))
                .Returns(DialogResult.No);

            await _explorer.CopyFilesAsync("dest", new[] { "src/a.txt", "src/b.txt" });

            _fileSystem.Verify(mock => mock.DeleteFile(It.IsAny<string>()), Times.Never);
            _fileSystem.Verify(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/a.txt"),
                CheckPath("dest/a.txt")), Times.Once);
            _fileSystem.Verify(mock => mock.CopyFile(
                CheckPath("src/b.txt"),
                CheckPath("dest/b.txt")), Times.Once);
            _fileSystem.VerifyNoOtherCalls();

            _dialogView.Verify(mock => mock.ConfirmReplace(CheckPath("dest/a.txt")), Times.Once);
            _dialogView.VerifyNoOtherCalls();

            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task CopyFileAsync_CancelOperation()
        {
            _fileSystem
                .Setup(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/a.txt");
                });
            _fileSystem
                .Setup(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()))
                .Callback<string, ISearchListener>((dest, listener) =>
                {
                    listener.OnFile("src/b.txt");
                });
            _fileSystem
                .Setup(mock => mock.CopyFile("src/a.txt", It.IsAny<string>()))
                .Throws(new IOException());
            _dialogView
                .Setup(mock => mock.ConfirmReplace(CheckPath("dest/a.txt")))
                .Returns(DialogResult.Cancel);

            try
            {
                await _explorer.CopyFilesAsync("dest", new[] {"src/a.txt", "src/b.txt"});
            }
            finally
            {
                _fileSystem.Verify(mock => mock.DeleteFile(It.IsAny<string>()), Times.Never);
                _fileSystem.Verify(mock => mock.Search("src/a.txt", It.IsAny<ISearchListener>()), Times.Once);
                _fileSystem.Verify(mock => mock.Search("src/b.txt", It.IsAny<ISearchListener>()), Times.Once);
                _fileSystem.Verify(mock => mock.CopyFile(
                    CheckPath("src/a.txt"),
                    CheckPath("dest/a.txt")), Times.Once);
                _fileSystem.Verify(mock => mock.CopyFile(
                    CheckPath("src/b.txt"),
                    CheckPath("dest/b.txt")), Times.Never);
                _fileSystem.VerifyNoOtherCalls();

                _dialogView.Verify(mock => mock.ConfirmReplace(CheckPath("dest/a.txt")), Times.Once);
                _dialogView.VerifyNoOtherCalls();

                _progressController.Verify(mock => mock.Close(), Times.Once);
            }
        }
    }
}
