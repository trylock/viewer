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
            _storage.Setup(mock => mock.Load("test")).Returns<IEntity>(null);

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
            _entityManager.MoveEntity(entity, "test2");

            _storage.Verify(mock => mock.Move(entity, "test2"), Times.Once);
        }

        [TestMethod]
        public void MoveEntity_DoNotMoveEntityIfTheMoveOperationInStorageFails()
        {
            var entity = new FileEntity("test");
            var oldPath = entity.Path;
            _entityManager.SetEntity(entity, true);

            _storage
                .Setup(mock => mock.Move(entity, "test2"))
                .Throws(new UnauthorizedAccessException());

            var throws = false;
            try
            {
                _entityManager.MoveEntity(entity, "test2");
            }
            catch (UnauthorizedAccessException)
            {
                throws = true;
            }
            Assert.IsTrue(throws);

            var modified = _entityManager.GetModified().ToArray();
            Assert.AreEqual(1, modified.Length);
            Assert.AreEqual(oldPath, modified[0].Path);
            Assert.AreEqual(oldPath, entity.Path);
        }

        [TestMethod]
        public void SetEntity_ModifyTheSameEntityTwice()
        {
            var entityA = new FileEntity("test");
            var entityB = new FileEntity("test").SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom));
            _entityManager.SetEntity(entityA, true);
            _entityManager.SetEntity(entityB, true);

            var modified = _entityManager.GetModified();
            Assert.AreEqual(1, modified.Count);
            Assert.AreEqual(1, (modified[0].GetAttribute("a")?.Value as IntValue).Value);
        }

        [TestMethod]
        public void SetEntity_ModifiedListIsAListOfCopies()
        {
            var entity = new FileEntity("test");
            _entityManager.SetEntity(entity, true);

            entity.SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom));
            Assert.IsNotNull(entity.GetAttribute("test"));

            var modified = _entityManager.GetModified();
            Assert.AreEqual(1, modified.Count);
            Assert.AreEqual(entity.Path, modified[0].Path);
            Assert.IsNull(modified[0].GetAttribute("test"));
        }

        [TestMethod]
        public void SetEntity_NonMarkedEntityWithReplaceFalse()
        {
            var entity = new FileEntity("test");
            _entityManager.SetEntity(entity, false);

            entity.SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom));
            Assert.IsNotNull(entity.GetAttribute("test"));
            
            var modified = _entityManager.GetModified();
            Assert.AreEqual(1, modified.Count);
            Assert.AreEqual(entity.Path, modified[0].Path);
            Assert.IsNull(modified[0].GetAttribute("test"));
        }

        [TestMethod]
        public void SetEntity_MarkedEntityWithReplaceFalse()
        {
            var entity1 = new FileEntity("test1")
                .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom));
            var entity2 = new FileEntity("test1")
                .SetAttribute(new Attribute("a", new IntValue(2), AttributeSource.Custom));
            _entityManager.SetEntity(entity1, true);
            _entityManager.SetEntity(entity2, false);

            entity1.SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom));
            Assert.IsNotNull(entity1.GetAttribute("test"));

            entity2.SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom));
            Assert.IsNotNull(entity2.GetAttribute("test"));

            var modified = _entityManager.GetModified();
            Assert.AreEqual(1, modified.Count);
            Assert.AreEqual(entity1.Path, modified[0].Path);
            Assert.IsNull(modified[0].GetAttribute("test"));
            Assert.AreEqual(1, modified[0].GetValue<IntValue>("a").Value);
        }

        [TestMethod]
        public void MoveEntity_ChangesModifiedEntityPath()
        {
            var entity = new FileEntity("test");

            _entityManager.SetEntity(entity, true);
            _entityManager.MoveEntity(entity, "test1");

            var modified = _entityManager.GetModified();
            Assert.AreEqual(1, modified.Count);
            Assert.AreEqual(PathUtils.NormalizePath("test1"), modified[0].Path);
        }

        [TestMethod]
        public void GetEntity_LoadEntityFromTheModifiedList()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom));

            _entityManager.SetEntity(entity, true);

            var loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded);
        }

        [TestMethod]
        public void GetEntity_LoadEntityFromTheSavingList()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom));

            _entityManager.SetEntity(entity, true);

            var snapshot = _entityManager.GetModified();
            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(PathUtils.NormalizePath("test"), snapshot[0].Path);

            var loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(loaded, entity);
        }

        [TestMethod]
        public void GetEntity_LoadTheMostRecentEntityFromTheSavingList()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom));
            
            // add entity to the modified list
            _entityManager.SetEntity(entity, true);

            var loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded);

            // move entity to the saving list
            var snapshot = _entityManager.GetModified();
            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(entity.Path, snapshot[0].Path);
            Assert.AreEqual(1, snapshot[0].GetValue<IntValue>("a").Value);

            loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded);

            // modify entity
            entity = entity.SetAttribute(new Attribute("a", new IntValue(2), AttributeSource.Custom));

            // add modified entity to the modified list
            _entityManager.SetEntity(entity, false);

            loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded);

            // move modified entity to the saving list
            snapshot = _entityManager.GetModified();
            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(entity.Path, snapshot[0].Path);
            Assert.AreEqual(2, snapshot[0].GetValue<IntValue>("a").Value);
            
            loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded);
        }

        [TestMethod]
        public void GetModified_SaveRemovesEntityFromTheSavingList()
        {
            var entity = new FileEntity("test");

            _entityManager.SetEntity(entity, true);
            var snapshot = _entityManager.GetModified();
            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(entity.Path, snapshot[0].Path);
            Assert.IsFalse(snapshot[0].Any());

            var loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded);

            snapshot[0].Save();

            _storage.Verify(mock => mock.Store(It.Is<IEntity>(storedEntity =>
                storedEntity.Path == entity.Path &&
                !storedEntity.Any())), Times.Once);

            loaded = _entityManager.GetEntity("test");
            Assert.IsNull(loaded);
        }

        [TestMethod]
        public void GetModified_ReventRemovesEntityFromTheSavingList()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom));

            // add entity to the modified list
            _entityManager.SetEntity(entity, true);

            // move it to the saving list
            var snapshot = _entityManager.GetModified();
            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(entity.Path, snapshot[0].Path);

            var loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded);

            // remove attribute "a"
            entity.RemoveAttribute("a");
            Assert.IsFalse(entity.Any());

            // revert the entity to its initial state
            snapshot[0].Revert();
            Assert.IsTrue(entity.Any());
            Assert.AreEqual(1, entity.GetValue<IntValue>("a").Value);
            
            loaded = _entityManager.GetEntity("test");
            Assert.IsNull(loaded);
        }

        [TestMethod]
        public void GetModified_ReturnRemovesEntityFromTheSavingList()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom));

            // add entity to the modified list
            _entityManager.SetEntity(entity, true);

            // move it to the saving list
            var snapshot = _entityManager.GetModified();
            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(entity.Path, snapshot[0].Path);

            var loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded);

            // remove attribute "a"
            entity.RemoveAttribute("a");
            Assert.IsFalse(entity.Any());

            // return the entity to the modified list
            snapshot[0].Return();
            Assert.IsFalse(entity.Any());

            // It returns the modified entity. This is ok, Return should not change the entity state
            loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded); 

            // move it to the saving list again 
            snapshot = _entityManager.GetModified();
            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(entity.Path, snapshot[0].Path);
            Assert.AreEqual(1, snapshot[0].GetValue<IntValue>("a").Value);

            loaded = _entityManager.GetEntity("test");
            Assert.AreEqual(entity, loaded);
        }
    }
}
