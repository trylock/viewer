using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.UI.Attributes;
using Viewer.UI.Tasks;

namespace ViewerTest.UI.Attributes
{
    [TestClass]
    public class SaveQueueTest
    {
        private Mock<ITaskLoader> _taskLoader;
        private Mock<IProgressController> _progressController;
        private SaveQueue _queue;

        [TestInitialize]
        public void Setup()
        {
            _progressController = new Mock<IProgressController>();
            _taskLoader = new Mock<ITaskLoader>();
            _taskLoader
                .Setup(mock => mock.CreateLoader(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
                .Returns(_progressController.Object);

            _queue = new SaveQueue(_taskLoader.Object);
            
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        [TestMethod]
        public async Task SaveAsync_NoItems()
        {
            await _queue.SaveAsync(new IModifiedEntity[] {});
            
            _taskLoader.Verify(mock => mock.CreateLoader(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()), Times.Never);
        }

        [TestMethod]
        public async Task SaveAsync_OneItem()
        {
            var item = new Mock<IModifiedEntity>();
            await _queue.SaveAsync(new[] { item.Object });

            item.Verify(mock => mock.Save(), Times.Once);
            item.Verify(mock => mock.Return(), Times.Never);
            item.Verify(mock => mock.Revert(), Times.Never);

            _progressController.VerifySet(mock => mock.TotalTaskCount = 1, Times.Once);
            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        public async Task SaveAsync_MultipleBlocks()
        {
            var mocks = new Mock<IModifiedEntity>[100];
            for (var i = 0; i < mocks.Length; ++i)
            {
                mocks[i] = new Mock<IModifiedEntity>();
            }

            // save all blocks
            var tasks = new Task[mocks.Length];
            for (var i = 0; i < tasks.Length; ++i)
            {
                var mock = mocks[i];
                tasks[i] = _queue.SaveAsync(new []{ mock.Object });
            }

            await Task.WhenAll(tasks);

            foreach (var mock in mocks)
            {
                mock.Verify(it => it.Save(), Times.Once);
                mock.Verify(it => it.Return(), Times.Never);
                mock.Verify(it => it.Revert(), Times.Never);
            }
            
            _taskLoader.Verify(mock => mock.CreateLoader(It.IsAny<string>(), It.IsAny<CancellationTokenSource>()), Times.Once);
            // the progress controller mock is setup up so that is returns 0, thus each save operation
            // should set it to 1
            _progressController.VerifySet(mock => mock.TotalTaskCount = 1, Times.Exactly(mocks.Length));
            _progressController.Verify(mock => mock.Close(), Times.Once);
        }

        [TestMethod]
        public async Task SaveAsync_ReturnItemsToTheModifiedList()
        {
            var mocks = new Mock<IModifiedEntity>[10];
            for (var i = 0; i < mocks.Length; ++i)
            {
                mocks[i] = new Mock<IModifiedEntity>();
            }

            // the first mock will cancel the execution
            mocks[0]
                .Setup(mock => mock.Save())
                .Callback(() => _queue.Cancel());

            await _queue.SaveAsync(mocks.Select(item => item.Object).ToList());

            mocks[0].Verify(mock => mock.Save(), Times.Once);
            mocks[0].Verify(mock => mock.Return(), Times.Never);
            mocks[0].Verify(mock => mock.Revert(), Times.Never);
            for (var i = 1; i < mocks.Length; ++i)
            {
                var mock = mocks[i];
                mock.Verify(it => it.Save(), Times.Never);
                mock.Verify(it => it.Return(), Times.Once);
                mock.Verify(it => it.Revert(), Times.Never);
            }
        }

        [TestMethod]
        public async Task SaveAsync_ReturnEntityToTheModifiedListInCaseOfAnException()
        {
            var mocks = new Mock<IModifiedEntity>[10];
            for (var i = 0; i < mocks.Length; ++i)
            {
                mocks[i] = new Mock<IModifiedEntity>();
            }

            mocks[0]
                .Setup(mock => mock.Save())
                .Throws(new FileNotFoundException());
            mocks[1]
                .Setup(mock => mock.Save())
                .Throws(new IOException());
            mocks[2]
                .Setup(mock => mock.Save())
                .Throws(new UnauthorizedAccessException());
            mocks[3]
                .Setup(mock => mock.Save())
                .Throws(new SecurityException());

            await _queue.SaveAsync(mocks.Select(item => item.Object).ToList());

            for (var i = 0; i < 4; ++i)
            {
                mocks[i].Verify(mock => mock.Save(), Times.Once);
                mocks[i].Verify(mock => mock.Return(), Times.Once);
                mocks[i].Verify(mock => mock.Revert(), Times.Never);
            }

            for (var i = 4; i < mocks.Length; ++i)
            {
                mocks[i].Verify(mock => mock.Save(), Times.Once);
                mocks[i].Verify(mock => mock.Return(), Times.Never);
                mocks[i].Verify(mock => mock.Revert(), Times.Never);
            }
        }
    }
}
