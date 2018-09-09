using System;
using System.Collections.Generic;
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
using Viewer.Query.QueryExpression;
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
            var query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
            var result = query.Union(new MemoryQuery(entitiesB)).Execute(new NullQueryProgress(), CancellationToken.None).ToArray();

            CollectionAssert.AreEqual(new[]
            {
                entitiesA[0],
                entitiesA[1],
                entitiesB[0]
            }, result);
        }

        [TestMethod]
        public void Match_SelectQuery()
        {
            var entities = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var query = new Viewer.Query.Query(new MemoryQuery(entities));
            Assert.IsTrue(query.Match(entities[0]));
            Assert.IsTrue(query.Match(entities[1]));
            Assert.IsFalse(query.Match(new FileEntity("test1")));
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
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
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
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
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
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
            query = query.Intersect(new MemoryQuery(entitiesB));
            Assert.IsFalse(query.Match(entitiesA[0]));
            Assert.IsTrue(query.Match(entitiesA[1]));
            Assert.IsTrue(query.Match(entitiesB[0]));
            Assert.IsFalse(query.Match(entitiesB[1]));
            Assert.IsFalse(query.Match(new FileEntity("test1")));
            Assert.IsFalse(query.Match(new FileEntity("test2")));
        }

        [TestMethod]
        public void Except_SameEntities()
        {
            var entitiesA = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var entitiesB = new[]
            {
                entitiesA[1],
                entitiesA[0], 
            };
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
            query = query.Except(new MemoryQuery(entitiesB));

            var result = query.Execute(new NullQueryProgress(), CancellationToken.None).ToArray();
            CollectionAssert.AreEqual(new IEntity[]{}, result);
        }

        [TestMethod]
        public void Except_NonEmptyIntersection()
        {
            var entitiesA = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var entitiesB = new[]
            {
                entitiesA[1],
            };
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
            query = query.Except(new MemoryQuery(entitiesB));

            var result = query.Execute(new NullQueryProgress(), CancellationToken.None).ToArray();
            CollectionAssert.AreEqual(new[] { entitiesA[0] }, result);
        }

        [TestMethod]
        public void Except_EmptyIntersection()
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
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
            query = query.Except(new MemoryQuery(entitiesB));

            var result = query.Execute(new NullQueryProgress(), CancellationToken.None).ToArray();
            CollectionAssert.AreEqual(new[] { entitiesA[0], entitiesA[1] }, result);
        }

        [TestMethod]
        public void Union_IsCaseInsensitive()
        {
            var entitiesA = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var entitiesB = new[]
            {
                new FileEntity("TEST3"),
                new FileEntity("TesT2"),
            };
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
            query = query.Union(new MemoryQuery(entitiesB));

            var result = query.Execute(new NullQueryProgress(), CancellationToken.None).ToArray();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(entitiesA[0].Path, result[0].Path);
            Assert.AreEqual(entitiesA[1].Path, result[1].Path);
            Assert.AreEqual(entitiesB[0].Path, result[2].Path);
        }

        [TestMethod]
        public void Intersect_NonEmptyIntersection()
        {
            var entitiesA = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };
            var entitiesB = new[]
            {
                new FileEntity("test3"),
                entitiesA[1],
            };
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
            query = query.Intersect(new MemoryQuery(entitiesB));

            var result = query.Execute(new NullQueryProgress(), CancellationToken.None).ToArray();
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(entitiesA[1].Path, result[0].Path);
        }

        [TestMethod]
        public void Intersect_EmptyIntersection()
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
            IQuery query = new Viewer.Query.Query(new MemoryQuery(entitiesA));
            query = query.Intersect(new MemoryQuery(entitiesB));

            var result = query.Execute(new NullQueryProgress(), CancellationToken.None).ToArray();
            Assert.AreEqual(0, result.Length);
        }
    }
}
