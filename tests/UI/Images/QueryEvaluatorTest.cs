using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.IO;
using Viewer.Query;
using Viewer.UI.Images;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class QueryEvaluatorTest
    {
        private Mock<IFileWatcher> _fileWatcher;
        private Mock<IErrorListener> _errorListener;
        private Mock<ILazyThumbnailFactory> _thumbnailFactory;
        private Mock<IEntityManager> _entities;
        private Mock<IQuery> _query;
        private QueryEvaluator _evaluator;
        
        [TestInitialize]
        public void Setup()
        {
            _thumbnailFactory = new Mock<ILazyThumbnailFactory>();
            _fileWatcher = new Mock<IFileWatcher>();
            _errorListener = new Mock<IErrorListener>();
            _entities = new Mock<IEntityManager>();
            _query = new Mock<IQuery>();
            _query.Setup(mock => mock.Comparer).Returns(EntityComparer.Default);

            var factory = new Mock<IFileWatcherFactory>();
            factory.Setup(mock => mock.Create()).Returns(_fileWatcher.Object);
            _evaluator = new QueryEvaluator(
                factory.Object,
                _thumbnailFactory.Object, 
                _errorListener.Object, 
                _entities.Object, 
                _query.Object);
        }

        [TestMethod]
        public void Update_NoRunningQuery()
        {
            var result = _evaluator.Update();
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Update_RunQuery()
        {
            var entity1 = new FileEntity("test1");
            var entity2 = new FileEntity("test2");

            var thumbnail1 = new Mock<ILazyThumbnail>();
            var thumbnail2 = new Mock<ILazyThumbnail>();
            _thumbnailFactory
                .SetupSequence(mock => mock.Create(It.IsAny<IEntity>(), It.IsAny<CancellationToken>()))
                .Returns(thumbnail1.Object)
                .Returns(thumbnail2.Object);

            _query.Setup(mock => mock.Execute(
                It.IsAny<IProgress<QueryProgressReport>>(), 
                It.IsAny<CancellationToken>()))
                .Returns(new[]{ entity1, entity2 });

            _evaluator.Run();

            var items = _evaluator.Update();
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(entity1, items[0].Data);
            Assert.AreEqual(entity2, items[1].Data);
        }

        [TestMethod]
        public void Update_DeleteViewsOnFileDeleted()
        {
            var entities = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
                new FileEntity("test3"),
            };
            var thumbnail1 = new Mock<ILazyThumbnail>();
            var thumbnail2 = new Mock<ILazyThumbnail>();
            var thumbnail3 = new Mock<ILazyThumbnail>();
            
            _query.Setup(mock => mock.Execute(
                    It.IsAny<IProgress<QueryProgressReport>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(entities);
            _thumbnailFactory
                .SetupSequence(mock => mock.Create(It.IsAny<IEntity>(), It.IsAny<CancellationToken>()))
                .Returns(thumbnail1.Object)
                .Returns(thumbnail2.Object)
                .Returns(thumbnail3.Object);
            _evaluator.Run();

            var items = _evaluator.Update();
            Assert.AreEqual(3, items.Count);

            _fileWatcher.Raise(mock => mock.Deleted += null, 
                new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(entities[2].Path), Path.GetFileName(entities[2].Path)));
            _fileWatcher.Raise(mock => mock.Deleted += null,
                new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(entities[0].Path), Path.GetFileName(entities[0].Path)));

            items = _evaluator.Update();
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(entities[1], items[0].Data);
            Assert.AreEqual(thumbnail2.Object, items[0].Thumbnail);

            thumbnail1.Verify(mock => mock.Dispose(), Times.Once);
            thumbnail2.Verify(mock => mock.Dispose(), Times.Never);
            thumbnail3.Verify(mock => mock.Dispose(), Times.Once);
        }

        [TestMethod]
        public void Update_ResortModifiedEntities()
        {
            var entities = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
                new FileEntity("test3"),
            };
            var thumbnail1 = new Mock<ILazyThumbnail>();
            var thumbnail2 = new Mock<ILazyThumbnail>();
            var thumbnail3 = new Mock<ILazyThumbnail>();
            _query.Setup(mock => mock.Execute(
                    It.IsAny<IProgress<QueryProgressReport>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(entities);
            _thumbnailFactory
                .SetupSequence(mock => mock.Create(It.IsAny<IEntity>(), It.IsAny<CancellationToken>()))
                .Returns(thumbnail1.Object)
                .Returns(thumbnail2.Object)
                .Returns(thumbnail3.Object);

            _evaluator.Run();
            var items = _evaluator.Update().Select(view => view.Data).ToArray();
            CollectionAssert.AreEqual(entities, items);

            entities[2].ChangePath("test0");
            _entities.Raise(mock => mock.Moved += null, new EntityMovedEventArgs("test3", entities[2]));

            items = _evaluator.Update().Select(view => view.Data).ToArray();
            Assert.AreEqual(entities[2], items[0]);
            Assert.AreEqual(entities[0], items[1]);
            Assert.AreEqual(entities[1], items[2]);
        }

        [TestMethod]
        public void Update_RenameEntityMovedInFileSystem()
        {
            var entities = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var thumbnail1 = new Mock<ILazyThumbnail>();
            var thumbnail2 = new Mock<ILazyThumbnail>();
            _query.Setup(mock => mock.Execute(
                    It.IsAny<IProgress<QueryProgressReport>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(entities);
            _thumbnailFactory
                .SetupSequence(mock => mock.Create(It.IsAny<IEntity>(), It.IsAny<CancellationToken>()))
                .Returns(thumbnail1.Object)
                .Returns(thumbnail2.Object);

            _evaluator.Run();
            var items = _evaluator.Update().Select(view => view.Data).ToArray();
            CollectionAssert.AreEqual(entities, items);
            
            _fileWatcher.Raise(mock => mock.Renamed += null, new RenamedEventArgs(
                WatcherChangeTypes.Renamed, 
                Path.GetDirectoryName(entities[1].Path),
                "test0", 
                Path.GetFileName(entities[1].Path)));

            items = _evaluator.Update().Select(view => view.Data).ToArray();
            Assert.AreEqual(entities[1], items[0]);
            Assert.AreEqual(entities[0], items[1]);
            Assert.AreEqual(PathUtils.NormalizePath("test0"), entities[1].Path);
        }
    }
}
