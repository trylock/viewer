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

namespace ViewerTest.Data
{
    [TestClass]
    public class QueryTest
    {
        [TestMethod]
        public void Evaluate_EmptyQuery()
        {
            var storage = new Mock<IAttributeStorage>();
            var fileSystem = new Mock<IFileSystem>();
            var query = new Query(storage.Object, fileSystem.Object);
            var entities = query.ToArray();
            Assert.AreEqual(0, entities.Length);
        }

        [TestMethod]
        public void Evaluate_NoWhereCondition()
        {
            var entity1 = new Entity("pattern/a.jpg");
            var entity2 = new Entity("pattern/b.jpg");

            var storage = new Mock<IAttributeStorage>();
            var fileSystem = new Mock<IFileSystem>();

            fileSystem.Setup(mock => mock.DirectoryExists("pattern\\")).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateFiles("pattern\\")).Returns(new[] {entity1.Path, entity2.Path});

            storage.Setup(mock => mock.Load(entity1.Path)).Returns(entity1);
            storage.Setup(mock => mock.Load(entity2.Path)).Returns(entity2);

            IQuery query = new Query(storage.Object, fileSystem.Object);
            query = query.Select("pattern");
            var entities = query.ToArray();
            
            CollectionAssert.AreEqual(new IEntity[]{ entity1, entity2 }, entities);
        }

        [TestMethod]
        public void Evaluate_MultipleWhereConditions()
        {
            var entity1 = new Entity("pattern/a.jpg");
            var entity2 = new Entity("pattern/b.jpg");

            var storage = new Mock<IAttributeStorage>();
            var fileSystem = new Mock<IFileSystem>();

            fileSystem.Setup(mock => mock.DirectoryExists("pattern\\")).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateFiles("pattern\\")).Returns(new[] { entity1.Path, entity2.Path });

            storage.Setup(mock => mock.Load(entity1.Path)).Returns(entity1);
            storage.Setup(mock => mock.Load(entity2.Path)).Returns(entity2);

            IQuery query = new Query(storage.Object, fileSystem.Object);
            query = query.Select("pattern");
            var firstQuery = query.Where(entity => entity.Path.StartsWith("pattern"));
            var secondQuery = firstQuery.Where(entity => entity.Path == "pattern/b.jpg");
            var thirdQuery = firstQuery.Where(entity => false);

            // execute the queries
            var firstResult = firstQuery.ToArray();
            var secondResult = secondQuery.ToArray();
            var thirdResult = thirdQuery.ToArray();
            
            // make sure we have loaded the entities from the storage
            storage.Verify(mock => mock.Load("pattern/a.jpg"));
            storage.Verify(mock => mock.Load("pattern/b.jpg"));

            CollectionAssert.AreEqual(new IEntity[] { entity1, entity2 }, firstResult);
            CollectionAssert.AreEqual(new IEntity[] { entity2 }, secondResult);
            CollectionAssert.AreEqual(new IEntity[] { }, thirdResult);
        }
    }
}
