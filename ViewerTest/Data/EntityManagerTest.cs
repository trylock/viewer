using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.IO;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data
{
    [TestClass]
    public class EntityManagerTest
    {
        private Mock<IAttributeStorage> _storage;
        private EntityManager _entityManager;

        [TestInitialize]
        public void Setup()
        {
            _storage = new Mock<IAttributeStorage>();
            _entityManager = new EntityManager(_storage.Object);
        }

        [TestMethod]
        public void GetEntity_NonExistentEntity()
        {
            _storage.Setup(mock => mock.Load("test")).Returns((IEntity) null);

            var result = _entityManager.GetEntity("test");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetEntity_LoadEntity()
        {
            var entity = new FileEntity("test");
            _storage.Setup(mock => mock.Load("test")).Returns(entity);

            var result = _entityManager.GetEntity("test");
            Assert.IsTrue(ReferenceEquals(result, entity));

            result = _entityManager.GetEntity("test");
            Assert.IsTrue(ReferenceEquals(result, entity));

            _storage.Verify(mock => mock.Load("test"), Times.Exactly(2));
        }

        [TestMethod]
        public void MoveEntity_MovesEntityInTheStorage()
        {
            var entity = new FileEntity("test");

            _entityManager.SetEntity(entity, true);
            _entityManager.MoveEntity("test", "test2");

            _storage.Verify(mock => mock.Move("test", "test2"), Times.Once);
        }

        [TestMethod]
        public void MoveEntity_DoNotMoveEntityIfTheMoveOperationInStorageFails()
        {
            _entityManager.SetEntity(new FileEntity("test"), true);

            _storage
                .Setup(mock => mock.Move("test", "test2"))
                .Throws(new UnauthorizedAccessException());

            var throws = false;
            try
            {
                _entityManager.MoveEntity("test", "test2");
            }
            catch (UnauthorizedAccessException)
            {
                throws = true;
            }
            Assert.IsTrue(throws);

            var modified = _entityManager.GetModified().ToArray();
            Assert.AreEqual(1, modified.Length);
            Assert.AreEqual("test", modified[0].Path);
        }

        [TestMethod]
        public void SetEntity_ModifyTheSameEntityTwice()
        {
            var entityA = new FileEntity("test");
            var entityB = new FileEntity("test").SetAttribute(new Attribute("a", new IntValue(1)));
            _entityManager.SetEntity(entityA, true);
            _entityManager.SetEntity(entityB, true);

            var modified = _entityManager.GetModified();
            Assert.AreEqual(1, modified.Count);
            Assert.AreEqual(1, (modified[0].GetAttribute("a")?.Value as IntValue).Value);
        }
    }
}
