using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;

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
                entitiesA, 
                Comparer<IEntity>.Default,
                null,
                new CancellationTokenSource());
            var result = query.Union(entitiesB).ToArray();

            CollectionAssert.AreEqual(new[]
            {
                entitiesA[0],
                entitiesA[1],
                entitiesB[0]
            }, result);
        }
    }
}
