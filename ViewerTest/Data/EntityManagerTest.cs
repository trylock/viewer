using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;

namespace ViewerTest.Data
{
    [TestClass]
    public class EntityManagerTest
    {
        [TestMethod]
        public void GetEntity_MissingEntity()
        {
            var storage = new MemoryAttributeStorage();
            var manager = new EntityManager(storage);
            Assert.IsNull(manager.GetEntity("test"));
        }

        [TestMethod]
        public void GetEntity_LoadEntityFromStorage()
        {
            var test = new Entity("test", DateTime.Now, DateTime.Now);
            var storage = new MemoryAttributeStorage();
            storage.Add(test);
            var manager = new EntityManager(storage);
            var entity = manager.GetEntity("test");
            Assert.AreEqual(test, entity);
        }

        [TestMethod]
        public void GetEntity_GetEntityFromMemory()
        {
            var test = new Entity("test", DateTime.Now, DateTime.Now);
            var storage = new MemoryAttributeStorage();
            storage.Add(test);
            var manager = new EntityManager(storage);
            manager.GetEntity("test");
            storage.Remove("test");
            var entity = manager.GetEntity("test");
            Assert.AreEqual(test, entity);
        }

        [TestMethod]
        public void AddEntity_NewEntity()
        {
            var storage = new MemoryAttributeStorage();
            var manager = new EntityManager(storage);
            var entity = new Entity("test", DateTime.Now, DateTime.Now);
            manager.SetEntity(entity);
            Assert.IsNull(storage.Load("test"));
            manager.Save(entity);
            Assert.AreEqual(entity, storage.Load("test"));
        }

        [TestMethod]
        public void AddEntity_ReplaceOldEntity()
        {
            var storage = new MemoryAttributeStorage();
            var entity = new Entity("test", DateTime.Now, DateTime.Now);
            storage.Add(entity);

            var manager = new EntityManager(storage);
            manager.GetEntity("test");
            var newEntity = new Entity("test", DateTime.Now, DateTime.Now);
            newEntity.SetAttribute(new IntAttribute("attr", 1));
            manager.SetEntity(newEntity);
            
            // changes were not written to a file yet
            Assert.AreEqual(entity, storage.Load("test"));
            Assert.AreEqual(newEntity, manager.GetEntity("test"));
            
            manager.Save(newEntity);

            Assert.AreEqual(newEntity, storage.Load("test"));
            Assert.AreEqual(newEntity, manager.GetEntity("test"));
        }

        [TestMethod]
        public void DeleteEntity_NonexistentEntity()
        {
            var storage = new MemoryAttributeStorage();
            var manager = new EntityManager(storage);
            manager.DeleteEntity("test");
        }

        [TestMethod]
        public void DeleteEntity_UnsavedEntity()
        {
            var storage = new MemoryAttributeStorage();
            var manager = new EntityManager(storage);
            var entity = new Entity("test", DateTime.Now, DateTime.Now);
            manager.SetEntity(entity);

            Assert.AreEqual(entity, manager.GetEntity("test"));
            Assert.IsNull(storage.Load("test"));

            manager.DeleteEntity("test");

            Assert.IsNull(manager.GetEntity("test"));
            Assert.IsNull(storage.Load("test"));
        }

        [TestMethod]
        public void DeleteEntity_SavedEntity()
        {
            var storage = new MemoryAttributeStorage();
            storage.Add(new Entity("test", DateTime.Now, DateTime.Now));
            var manager = new EntityManager(storage);
            var entity = manager.GetEntity("test");

            Assert.AreEqual(entity, manager.GetEntity("test"));
            Assert.AreEqual(entity, storage.Load("test"));

            manager.DeleteEntity("test");

            Assert.IsNull(manager.GetEntity("test"));
            Assert.IsNull(storage.Load("test"));
        }

        [TestMethod]
        public void MoveEntity_UnknownEntity()
        {
            var storage = new MemoryAttributeStorage();
            storage.Add(new Entity("test", DateTime.Now, DateTime.Now));
            var manager = new EntityManager(storage);
            
            Assert.IsNotNull(storage.Load("test"));
            Assert.IsNull(storage.Load("test2"));

            manager.MoveEntity("test", "test2");

            Assert.IsNull(storage.Load("test"));
            Assert.IsNotNull(storage.Load("test2"));
        }

        [TestMethod]
        public void MoveEntity_InMemoryAndInFile()
        {
            var storage = new MemoryAttributeStorage();
            storage.Add(new Entity("test", DateTime.Now, DateTime.Now));
            var manager = new EntityManager(storage);
            manager.GetEntity("test");
            
            Assert.IsNotNull(storage.Load("test"));
            Assert.IsNotNull(manager.GetEntity("test"));
            Assert.IsNull(storage.Load("test2"));
            Assert.IsNull(manager.GetEntity("test2"));

            manager.MoveEntity("test", "test2");

            Assert.IsNull(storage.Load("test"));
            Assert.IsNull(manager.GetEntity("test"));
            Assert.IsNotNull(storage.Load("test2"));
            Assert.IsNotNull(manager.GetEntity("test2"));
        }
    }
}
