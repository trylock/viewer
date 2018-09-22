using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Query;
using Viewer.UI;

namespace ViewerTest.UI
{
    [TestClass]
    public class QueryHistoryTest
    {
        [TestMethod]
        public void Current_EmptyHistory()
        {
            var queryEvents = new QueryHistory();
            
            Assert.IsNull(queryEvents.Current);
            Assert.IsNull(queryEvents.Current);
        }

        [TestMethod]
        public void Forward_EmptyHistory()
        {
            var executed = false;
            var queryEvents = new QueryHistory();
            queryEvents.QueryExecuted += (sender, args) => executed = true;

            Assert.IsFalse(executed);
            Assert.IsNull(queryEvents.Current);

            queryEvents.Forward();

            Assert.IsFalse(executed);
            Assert.IsNull(queryEvents.Current);
        }

        [TestMethod]
        public void Back_EmptyHistory()
        {
            var executed = false;
            var queryEvents = new QueryHistory();
            queryEvents.QueryExecuted += (sender, args) => executed = true;

            Assert.IsFalse(executed);
            Assert.IsNull(queryEvents.Current);

            queryEvents.Back();

            Assert.IsFalse(executed);
            Assert.IsNull(queryEvents.Current);
        }

        [TestMethod]
        public void ExecuteQuery_DontAddTheSameQueryTwice()
        {
            var executionCount = 0;

            var query1 = new Mock<IQuery>();
            query1.Setup(mock => mock.Text).Returns("1");
            var query2 = new Mock<IQuery>();
            query2.Setup(mock => mock.Text).Returns("1");

            var queryEvents = new QueryHistory();
            queryEvents.QueryExecuted += (sender, args) => ++executionCount;

            Assert.AreEqual(0, executionCount);
            Assert.IsNull(queryEvents.Current);
            Assert.AreNotEqual(query1.Object, query2.Object);

            queryEvents.ExecuteQuery(query1.Object);
            Assert.AreEqual(1, executionCount);
            Assert.AreEqual(query1.Object, queryEvents.Current);

            queryEvents.ExecuteQuery(query2.Object);
            Assert.AreEqual(2, executionCount);
            Assert.AreEqual(query1.Object, queryEvents.Current);
        }

        [TestMethod]
        public void ExecuteQuery_EmptyHistory()
        {
            var query = new Mock<IQuery>();
            IExecutableQuery executed = null;

            var queryEvents = new QueryHistory();
            queryEvents.QueryExecuted += (sender, args) => executed = args.Query;
            
            Assert.IsNull(queryEvents.Current);
            Assert.IsNull(executed);

            queryEvents.ExecuteQuery(query.Object);

            Assert.AreEqual(query.Object, queryEvents.Current);
            Assert.AreEqual(query.Object, executed);
        }

        [TestMethod]
        public void Back_MoveInHistory()
        {
            var query = new[]
            {
                new Mock<IQuery>(),
                new Mock<IQuery>(),
                new Mock<IQuery>(),
                new Mock<IQuery>(),
                new Mock<IQuery>(),
            };

            for (var i = 0; i < query.Length; ++i)
            {
                query[i].Setup(mock => mock.Text).Returns(i.ToString());
            }

            IExecutableQuery current = null;
            var queryEvents = new QueryHistory();
            queryEvents.QueryExecuted += (sender, args) => current = args.Query;

            Assert.IsNull(queryEvents.Current);

            queryEvents.ExecuteQuery(query[0].Object);
            Assert.AreEqual(query[0].Object, queryEvents.Current);
            Assert.AreEqual(query[0].Object, current);

            queryEvents.ExecuteQuery(query[1].Object);
            Assert.AreEqual(query[1].Object, queryEvents.Current);
            Assert.AreEqual(query[1].Object, current);

            queryEvents.Back();
            Assert.AreEqual(query[0].Object, queryEvents.Current);
            Assert.AreEqual(query[0].Object, current);

            queryEvents.ExecuteQuery(query[2].Object);
            Assert.AreEqual(query[2].Object, queryEvents.Current);
            Assert.AreEqual(query[2].Object, current);

            queryEvents.ExecuteQuery(query[3].Object);
            Assert.AreEqual(query[3].Object, queryEvents.Current);
            Assert.AreEqual(query[3].Object, current);

            queryEvents.Back();
            Assert.AreEqual(query[2].Object, queryEvents.Current);
            Assert.AreEqual(query[2].Object, current);

            queryEvents.Back();
            Assert.AreEqual(query[0].Object, queryEvents.Current);
            Assert.AreEqual(query[0].Object, current);

            queryEvents.Back();
            Assert.AreEqual(query[0].Object, queryEvents.Current);
            Assert.AreEqual(query[0].Object, current);

            queryEvents.ExecuteQuery(query[4].Object);
            Assert.AreEqual(query[4].Object, queryEvents.Current);
            Assert.AreEqual(query[4].Object, current);
        }

        [TestMethod]
        public void Forward_MoveInHistory()
        {
            var query = new[]
            {
                new Mock<IQuery>(),
                new Mock<IQuery>(),
                new Mock<IQuery>(),
                new Mock<IQuery>(),
            };

            for (var i = 0; i < query.Length; ++i)
            {
                query[i].Setup(mock => mock.Text).Returns(i.ToString());
            }

            IExecutableQuery current = null;
            var queryEvents = new QueryHistory();
            queryEvents.QueryExecuted += (sender, args) => current = args.Query;

            Assert.IsNull(queryEvents.Current);

            queryEvents.ExecuteQuery(query[0].Object);
            Assert.AreEqual(query[0].Object, queryEvents.Current);
            Assert.AreEqual(query[0].Object, current);

            queryEvents.ExecuteQuery(query[1].Object);
            Assert.AreEqual(query[1].Object, queryEvents.Current);
            Assert.AreEqual(query[1].Object, current);

            queryEvents.ExecuteQuery(query[2].Object);
            Assert.AreEqual(query[2].Object, queryEvents.Current);
            Assert.AreEqual(query[2].Object, current);

            queryEvents.Forward();
            Assert.AreEqual(query[2].Object, queryEvents.Current);
            Assert.AreEqual(query[2].Object, current);

            queryEvents.Back();
            queryEvents.ExecuteQuery(query[3].Object);
            Assert.AreEqual(query[3].Object, queryEvents.Current);
            Assert.AreEqual(query[3].Object, current);

            queryEvents.Back();
            queryEvents.Back();
            Assert.AreEqual(query[0].Object, queryEvents.Current);
            Assert.AreEqual(query[0].Object, current);

            queryEvents.Forward();
            Assert.AreEqual(query[1].Object, queryEvents.Current);
            Assert.AreEqual(query[1].Object, current);

            queryEvents.Forward();
            Assert.AreEqual(query[3].Object, queryEvents.Current);
            Assert.AreEqual(query[3].Object, current);
        }

        [TestMethod]
        public void GetEnumerator_EnumerateHistoryItemsInCorrectOrder()
        {
            var queries = new[]
            {
                new Mock<IQuery>(),
                new Mock<IQuery>(),
                new Mock<IQuery>(),
            };

            queries[0].Setup(mock => mock.Text).Returns("query0");
            queries[1].Setup(mock => mock.Text).Returns("query1");
            queries[2].Setup(mock => mock.Text).Returns("query2");

            var history = new QueryHistory();
            history.ExecuteQuery(queries[0].Object);
            history.ExecuteQuery(queries[1].Object);
            history.ExecuteQuery(queries[2].Object);

            var actualItems = history.ToArray();
            var expectedItems = queries.Select(mock => mock.Object).Reverse().ToArray();
            CollectionAssert.AreEqual(expectedItems, actualItems);
        }

        [TestMethod]
        public void Indexer_GetQueriesInCorrectOrder()
        {
            var queries = new[]
            {
                new Mock<IQuery>(),
                new Mock<IQuery>(),
                new Mock<IQuery>(),
            };

            queries[0].Setup(mock => mock.Text).Returns("query0");
            queries[1].Setup(mock => mock.Text).Returns("query1");
            queries[2].Setup(mock => mock.Text).Returns("query2");

            var history = new QueryHistory();
            history.ExecuteQuery(queries[0].Object);
            history.ExecuteQuery(queries[1].Object);
            history.ExecuteQuery(queries[2].Object);
            
            Assert.AreEqual(3, history.Count);
            Assert.AreEqual(queries[2].Object, history[0]);
            Assert.AreEqual(queries[1].Object, history[1]);
            Assert.AreEqual(queries[0].Object, history[2]);
        }
    }
}
