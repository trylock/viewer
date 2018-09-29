using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.IO;
using Viewer.Query;
using Viewer.Query.Expressions;
using Viewer.Query.QueryExpression;
using Viewer.Query.Search;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Query.QueryExpression
{
    [TestClass]
    public class WhereQueryTest
    {
        private WhereQuery CreateQuery(IExecutableQuery source, ValueExpression expression)
        {
            var runtime = new Mock<IRuntime>();
            var priorityComparerFactory = new Mock<IPriorityComparerFactory>();
            return new WhereQuery(runtime.Object, priorityComparerFactory.Object, source, expression);
        }

        [TestMethod]
        public void Text_WhenTheSourceIsASelect()
        {
            var select = new SelectQuery(
                new Mock<IFileSystem>().Object,
                new Mock<IEntityManager>().Object,
                "pattern",
                FileAttributes.System);
            var where = CreateQuery(select, new AttributeAccessExpression(0, 0, "attr"));
            Assert.IsTrue(string.Equals(
                "select \"pattern\"" + Environment.NewLine +
                "where attr",
                where.Text, StringComparison.CurrentCultureIgnoreCase));
        }

        [TestMethod]
        public void Text_WhenTheSourceIsAQueryView()
        {
            var select = new SelectQuery(
                new Mock<IFileSystem>().Object,
                new Mock<IEntityManager>().Object,
                "pattern",
                FileAttributes.System);
            var queryView = new QueryViewQuery(select, "viewName");
            var where = CreateQuery(queryView, new AttributeAccessExpression(0, 0, "attr"));
            Assert.IsTrue(string.Equals(
                "select viewName" + Environment.NewLine +
                "where attr",
                where.Text, StringComparison.CurrentCultureIgnoreCase));
        }

        [TestMethod]
        public void Text_WhenTheSourceIsAComplexQuery()
        {
            var selectA = new SelectQuery(
                new Mock<IFileSystem>().Object,
                new Mock<IEntityManager>().Object,
                "a",
                FileAttributes.System);
            var selectB = new SelectQuery(
                new Mock<IFileSystem>().Object,
                new Mock<IEntityManager>().Object,
                "b",
                FileAttributes.System);
            var union = new UnionQuery(selectA, selectB);
            var where = CreateQuery(union, new AttributeAccessExpression(0, 0, "attr"));
            Assert.IsTrue(string.Equals(
                "select (" + Environment.NewLine +
                "select \"a\"" + Environment.NewLine +
                "union" + Environment.NewLine +
                "select \"b\"" + Environment.NewLine +
                ")" + Environment.NewLine +
                "where attr",
                where.Text, StringComparison.CurrentCultureIgnoreCase));
        }
        
        [TestMethod]
        public void Match_EntityPredicate()
        {
            var entities = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2")
                    .SetAttribute(new Attribute("attr", new IntValue(4), AttributeSource.Custom)),
            };
            var query = CreateQuery(new MemoryQuery(entities), new AttributeAccessExpression(0, 0, "attr"));
            Assert.IsFalse(query.Match(entities[0]));
            Assert.IsTrue(query.Match(entities[1]));
            Assert.IsFalse(query.Match(
                new FileEntity("test1")
                    .SetAttribute(new Attribute("attr", new IntValue(4), AttributeSource.Custom))
            ));
        }
    }
}
