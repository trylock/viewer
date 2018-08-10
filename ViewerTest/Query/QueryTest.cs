using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Query;

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
            var result = query.Union(new MemoryQuery(entitiesB)).Evaluate(CancellationToken.None).ToArray();

            CollectionAssert.AreEqual(new[]
            {
                entitiesA[0],
                entitiesA[1],
                entitiesB[0]
            }, result);
        }
    }
}
