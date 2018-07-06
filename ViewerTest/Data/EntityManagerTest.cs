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
        public void GetEntity_LoadEntityOnlyOnce()
        {
            var entity = new Entity("test");
            _storage.Setup(mock => mock.Load("test")).Returns(entity);

            var result = _entityManager.GetEntity("test");
            Assert.IsTrue(ReferenceEquals(result, entity));

            result = _entityManager.GetEntity("test");
            Assert.IsTrue(ReferenceEquals(result, entity));

            _storage.Verify(mock => mock.Load("test"), Times.Once);
        }

        [TestMethod]
        public void MoveEntity_MovesEntityInTheStorage()
        {
            var entity = new Entity("test");

            _entityManager.SetEntity(entity);
            _entityManager.MoveEntity("test", "test2");

            _storage.Verify(mock => mock.Move("test", "test2"), Times.Once);

            Assert.AreEqual("test2", entity.Path);
        }

        [TestMethod]
        public void MoveEntity_DoNotMoveEntityIfTheMoveOperationInStorageFails()
        {
            _entityManager.SetEntity(new Entity("test"));

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
    }
}
