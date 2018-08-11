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
    public class QueryEventsTest
    {
        [TestMethod]
        public void Current_EmptyHistory()
        {
            var queryEvents = new QueryEvents();
            
            Assert.IsNull(queryEvents.Current);
            Assert.IsNull(queryEvents.Current);
        }

        [TestMethod]
        public void Forward_EmptyHistory()
        {
            var executed = false;
            var queryEvents = new QueryEvents();
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
            var queryEvents = new QueryEvents();
            queryEvents.QueryExecuted += (sender, args) => executed = true;

            Assert.IsFalse(executed);
            Assert.IsNull(queryEvents.Current);

            queryEvents.Back();

            Assert.IsFalse(executed);
            Assert.IsNull(queryEvents.Current);
        }

        [TestMethod]
        public void ExecuteQuery_EmptyHistory()
        {
            var query = new Mock<IQuery>();
            IQuery executed = null;

            var queryEvents = new QueryEvents();
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
                new Mock<IQuery>().Object,
                new Mock<IQuery>().Object,
                new Mock<IQuery>().Object,
                new Mock<IQuery>().Object,
                new Mock<IQuery>().Object,
            };

            IQuery current = null;
            var queryEvents = new QueryEvents();
            queryEvents.QueryExecuted += (sender, args) => current = args.Query;

            Assert.IsNull(queryEvents.Current);

            queryEvents.ExecuteQuery(query[0]);
            Assert.AreEqual(query[0], queryEvents.Current);
            Assert.AreEqual(query[0], current);

            queryEvents.ExecuteQuery(query[1]);
            Assert.AreEqual(query[1], queryEvents.Current);
            Assert.AreEqual(query[1], current);

            queryEvents.Back();
            Assert.AreEqual(query[0], queryEvents.Current);
            Assert.AreEqual(query[0], current);

            queryEvents.ExecuteQuery(query[2]);
            Assert.AreEqual(query[2], queryEvents.Current);
            Assert.AreEqual(query[2], current);

            queryEvents.ExecuteQuery(query[3]);
            Assert.AreEqual(query[3], queryEvents.Current);
            Assert.AreEqual(query[3], current);

            queryEvents.Back();
            Assert.AreEqual(query[2], queryEvents.Current);
            Assert.AreEqual(query[2], current);

            queryEvents.Back();
            Assert.AreEqual(query[0], queryEvents.Current);
            Assert.AreEqual(query[0], current);

            queryEvents.Back();
            Assert.AreEqual(query[0], queryEvents.Current);
            Assert.AreEqual(query[0], current);

            queryEvents.ExecuteQuery(query[4]);
            Assert.AreEqual(query[4], queryEvents.Current);
            Assert.AreEqual(query[4], current);
        }

        [TestMethod]
        public void Forward_MoveInHistory()
        {
            var query = new[]
            {
                new Mock<IQuery>().Object,
                new Mock<IQuery>().Object,
                new Mock<IQuery>().Object,
                new Mock<IQuery>().Object,
            };

            IQuery current = null;
            var queryEvents = new QueryEvents();
            queryEvents.QueryExecuted += (sender, args) => current = args.Query;

            Assert.IsNull(queryEvents.Current);

            queryEvents.ExecuteQuery(query[0]);
            Assert.AreEqual(query[0], queryEvents.Current);
            Assert.AreEqual(query[0], current);

            queryEvents.ExecuteQuery(query[1]);
            Assert.AreEqual(query[1], queryEvents.Current);
            Assert.AreEqual(query[1], current);

            queryEvents.ExecuteQuery(query[2]);
            Assert.AreEqual(query[2], queryEvents.Current);
            Assert.AreEqual(query[2], current);

            queryEvents.Forward();
            Assert.AreEqual(query[2], queryEvents.Current);
            Assert.AreEqual(query[2], current);

            queryEvents.Back();
            queryEvents.ExecuteQuery(query[3]);
            Assert.AreEqual(query[3], queryEvents.Current);
            Assert.AreEqual(query[3], current);

            queryEvents.Back();
            queryEvents.Back();
            Assert.AreEqual(query[0], queryEvents.Current);
            Assert.AreEqual(query[0], current);

            queryEvents.Forward();
            Assert.AreEqual(query[1], queryEvents.Current);
            Assert.AreEqual(query[1], current);

            queryEvents.Forward();
            Assert.AreEqual(query[3], queryEvents.Current);
            Assert.AreEqual(query[3], current);
        }
    }
}
