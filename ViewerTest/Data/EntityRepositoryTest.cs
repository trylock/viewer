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
    public class EntityRepositoryTest
    {
        [TestMethod]
        public void Add_ReplaceEntity()
        {
            var entity1 = new Entity("test");
            var entity2 = new Entity("test");
            var entity3 = new Entity("test2");
            var rep = new EntityRepository();
            rep.Add(entity1);
            rep.Add(entity2);
            rep.Add(entity3);

            var data = rep.GetSnapshot().OrderBy(entity => entity.Path).ToList();
            CollectionAssert.AreEqual(new[]{ entity2, entity3 }, data);
        }

        [TestMethod]
        public void TryAdd_DontReplaceEntity()
        {
            var entity1 = new Entity("test");
            var entity2 = new Entity("test");
            var entity3 = new Entity("test2");
            var rep = new EntityRepository();
            Assert.IsTrue(rep.TryAdd(entity1));
            Assert.IsFalse(rep.TryAdd(entity2));
            Assert.IsTrue(rep.TryAdd(entity3));

            var data = rep.GetSnapshot().OrderBy(entity => entity.Path).ToList();
            CollectionAssert.AreEqual(new[] { entity1, entity3 }, data);
        }

        [TestMethod]
        public void Remove_NonExistentEntity()
        {
            var entity1 = new Entity("test");
            var rep = new EntityRepository();
            rep.Add(entity1);
            rep.Remove("test2");

            var data = rep.GetSnapshot().OrderBy(entity => entity.Path).ToList();
            CollectionAssert.AreEqual(new[] { entity1 }, data);
        }

        [TestMethod]
        public void Move_NonExistentEntity()
        {
            var entity1 = new Entity("test");
            var rep = new EntityRepository();
            rep.Add(entity1);
            rep.Move("test2", "test");

            var data = rep.GetSnapshot().OrderBy(entity => entity.Path).ToList();
            CollectionAssert.AreEqual(new[] { entity1 }, data);
        }

        [TestMethod]
        public void Move_ExistingEntity()
        {
            var entity1 = new Entity("test");
            var rep = new EntityRepository();
            rep.Add(entity1);
            rep.Move("test", "test2");

            var data = rep.GetSnapshot();
            Assert.AreEqual(1, data.Count);
            Assert.AreEqual("test2", data[0].Path);
        }

        [TestMethod]
        public void Move_ReplaceNewPath()
        {
            var entity1 = new Entity("test");
            var entity2 = new Entity("test2");
            var rep = new EntityRepository();
            rep.Add(entity1);
            rep.Add(entity2);
            rep.Move("test", "test2");

            var data = rep.GetSnapshot();
            Assert.AreEqual(1, data.Count);
            Assert.AreEqual("test2", data[0].Path);
            Assert.AreNotEqual(entity2, data[0]);
        }
    }
}
