using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Query;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Query
{
    [TestClass]
    public class QueryTest
    {
        [TestMethod]
        public void Union_DontReturnTheSameEntityTwice()
        {
            var entitiesA = new[]
            {
                new FileEntity("test"),
                new FileEntity("test1"),
            };
            var entitiesB = new[]
            {
                new FileEntity("test2"),
                new FileEntity("test1"), 
            };
            var query = new Viewer.Query.Query(
                new MemoryQuery(entitiesA), 
                Comparer<IEntity>.Default,
                null);
            var result = query.Union(new MemoryQuery(entitiesB)).Evaluate(new NullQueryProgress(), CancellationToken.None).ToArray();

            CollectionAssert.AreEqual(new[]
            {
                entitiesA[0],
                entitiesA[1],
                entitiesB[0]
            }, result);
        }

        [TestMethod]
        public void WithText_ChangeQueryText()
        {
            var query = new Viewer.Query.Query(EmptyQuery.Default, EntityComparer.Default, "test1");
            var modified = query.WithText("test2");
            Assert.AreEqual("test1", query.Text);
            Assert.AreEqual("test2", modified.Text);
        }

        [TestMethod]
        public void Match_SelectQuery()
        {
            var entities = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var query = new Viewer.Query.Query(new MemoryQuery(entities), EntityComparer.Default, "test");
            Assert.IsTrue(query.Match(entities[0]));
            Assert.IsTrue(query.Match(entities[1]));
            Assert.IsFalse(query.Match(new FileEntity("test1")));
        }

        [TestMethod]
        public void Match_WhereQuery()
        {
            var entities = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2").SetAttribute(new Attribute("attr", new IntValue(4))),
            };
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entities), EntityComparer.Default, "test");
            query = query.Where(entity => entity.GetValue<IntValue>("attr") != null);
            Assert.IsFalse(query.Match(entities[0]));
            Assert.IsTrue(query.Match(entities[1]));
            Assert.IsFalse(query.Match(new FileEntity("test1").SetAttribute(new Attribute("attr", new IntValue(4)))));
        }

        [TestMethod]
        public void Match_UnionQuery()
        {
            var entitiesA = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var entitiesB = new[]
            {
                new FileEntity("test3"),
                new FileEntity("test4"),
            };
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA), EntityComparer.Default, "test");
            query = query.Union(new MemoryQuery(entitiesB));
            Assert.IsTrue(query.Match(entitiesA[0]));
            Assert.IsTrue(query.Match(entitiesA[1]));
            Assert.IsTrue(query.Match(entitiesB[0]));
            Assert.IsTrue(query.Match(entitiesB[1]));
            Assert.IsFalse(query.Match(new FileEntity("test1")));
            Assert.IsFalse(query.Match(new FileEntity("test2")));
        }

        [TestMethod]
        public void Match_ExceptQuery()
        {
            var entitiesA = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var entitiesB = new[]
            {
                entitiesA[1],
                new FileEntity("test2"),
            };
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA), EntityComparer.Default, "test");
            query = query.Except(new MemoryQuery(entitiesB));
            Assert.IsTrue(query.Match(entitiesA[0]));
            Assert.IsFalse(query.Match(entitiesA[1]));
            Assert.IsFalse(query.Match(entitiesB[0]));
            Assert.IsFalse(query.Match(entitiesB[1]));
            Assert.IsFalse(query.Match(new FileEntity("test1")));
            Assert.IsFalse(query.Match(new FileEntity("test2")));
        }

        [TestMethod]
        public void Match_IntersectQuery()
        {
            var entitiesA = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var entitiesB = new[]
            {
                entitiesA[1],
                new FileEntity("test2"),
            };
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA), EntityComparer.Default, "test");
            query = query.Intersect(new MemoryQuery(entitiesB));
            Assert.IsFalse(query.Match(entitiesA[0]));
            Assert.IsTrue(query.Match(entitiesA[1]));
            Assert.IsTrue(query.Match(entitiesB[0]));
            Assert.IsFalse(query.Match(entitiesB[1]));
            Assert.IsFalse(query.Match(new FileEntity("test1")));
            Assert.IsFalse(query.Match(new FileEntity("test2")));
        }
    }
}
