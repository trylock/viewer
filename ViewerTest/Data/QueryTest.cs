using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.UI.Log;

namespace ViewerTest.Data
{
    [TestClass]
    public class QueryTest
    {
        private Mock<IEntityManager> _entityManager;
        private Mock<IFileSystem> _fileSystem;
        private Mock<ILogger> _log;
        private IQuery _query;

        [TestInitialize]
        public void Startup()
        {
            _entityManager = new Mock<IEntityManager>();
            _fileSystem = new Mock<IFileSystem>();
            _log = new Mock<ILogger>();
            _query = new Query(_entityManager.Object, _fileSystem.Object, _log.Object);
        }

        [TestMethod]
        public void Evaluate_EmptyQuery()
        {
            var entities = _query.ToArray();
            Assert.AreEqual(0, entities.Length);
        }

        [TestMethod]
        public void Evaluate_NoWhereCondition()
        {
            var entity1 = new Entity("pattern/a.jpg");
            var entity2 = new Entity("pattern/b.jpg");
            
            _fileSystem.Setup(mock => mock.DirectoryExists("pattern\\")).Returns(true);
            _fileSystem.Setup(mock => mock.EnumerateFiles("pattern\\")).Returns(new[] {entity1.Path, entity2.Path});

            _entityManager.Setup(mock => mock.GetEntity(entity1.Path)).Returns(entity1);
            _entityManager.Setup(mock => mock.GetEntity(entity2.Path)).Returns(entity2);
            
            _query = _query.Select("pattern");
            var entities = _query.ToArray();
            
            CollectionAssert.AreEqual(new IEntity[]{ entity1, entity2 }, entities);
        }

        [TestMethod]
        public void Evaluate_MultipleWhereConditions()
        {
            var entity1 = new Entity("pattern/a.jpg");
            var entity2 = new Entity("pattern/b.jpg");
            
            _fileSystem.Setup(mock => mock.DirectoryExists("pattern\\")).Returns(true);
            _fileSystem.Setup(mock => mock.EnumerateFiles("pattern\\")).Returns(new[] { entity1.Path, entity2.Path });

            _entityManager.Setup(mock => mock.GetEntity(entity1.Path)).Returns(entity1);
            _entityManager.Setup(mock => mock.GetEntity(entity2.Path)).Returns(entity2);

            _query = _query.Select("pattern");
            var firstQuery = _query.Where(entity => entity.Path.StartsWith("pattern"));
            var secondQuery = firstQuery.Where(entity => entity.Path == "pattern/b.jpg");
            var thirdQuery = firstQuery.Where(entity => false);

            // execute the queries
            var firstResult = firstQuery.ToArray();
            var secondResult = secondQuery.ToArray();
            var thirdResult = thirdQuery.ToArray();

            // make sure we have loaded the entities from the storage
            _entityManager.Verify(mock => mock.GetEntity("pattern/a.jpg"));
            _entityManager.Verify(mock => mock.GetEntity("pattern/b.jpg"));

            CollectionAssert.AreEqual(new IEntity[] { entity1, entity2 }, firstResult);
            CollectionAssert.AreEqual(new IEntity[] { entity2 }, secondResult);
            CollectionAssert.AreEqual(new IEntity[] { }, thirdResult);
        }
    }
}
