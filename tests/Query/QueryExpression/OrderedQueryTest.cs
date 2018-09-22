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

namespace ViewerTest.Query.QueryExpression
{
    [TestClass]
    public class OrderedQueryTest
    {
        [TestMethod]
        public void Text_SourceDoesNotHaveATextualRepresentation()
        {
            var source = new Mock<QueryFragment>();
            source
                .Setup(mock => mock.Text)
                .Returns<string>(null);
            var orderBy = new OrderedQuery(source.Object, EntityComparer.Default, "attr desc");
            Assert.IsNull(orderBy.Text);
        }

        [TestMethod]
        public void Text_WhenTheSourceIsASelect()
        {
            var select = new SelectQuery(
                new Mock<IFileSystem>().Object,
                new Mock<IEntityManager>().Object,
                "a",
                FileAttributes.System);
            var orderBy = new OrderedQuery(select, EntityComparer.Default, "attr desc");
            Assert.IsTrue(string.Equals(
                "select \"a\"" + Environment.NewLine +
                "order by attr desc",
                orderBy.Text, StringComparison.CurrentCultureIgnoreCase));
        }

        [TestMethod]
        public void Text_WhenTheSourceIsAQueryView()
        {
            var select = new SelectQuery(
                new Mock<IFileSystem>().Object,
                new Mock<IEntityManager>().Object,
                "a",
                FileAttributes.System);
            var view = new QueryViewQuery(select, "viewName");
            var orderBy = new OrderedQuery(view, EntityComparer.Default, "attr desc");
            Assert.IsTrue(string.Equals(
                "select viewName" + Environment.NewLine +
                "order by attr desc",
                orderBy.Text, StringComparison.CurrentCultureIgnoreCase));
        }

        [TestMethod]
        public void Text_WhenTheSourceIsAWhere()
        {
            var select = new SelectQuery(
                new Mock<IFileSystem>().Object,
                new Mock<IEntityManager>().Object,
                "a",
                FileAttributes.System);
              
            var where = new WhereQuery(
                new Mock<IRuntime>().Object, 
                new Mock<IAttributeCache>().Object,  
                select,
                new AttributeAccessExpression(0, 0, "attr"));
            var orderBy = new OrderedQuery(where, EntityComparer.Default, "attr desc");
            Assert.IsTrue(string.Equals(
                "select \"a\"" + Environment.NewLine +
                "where attr" + Environment.NewLine +
                "order by attr desc",
                orderBy.Text, StringComparison.CurrentCultureIgnoreCase));
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
            var intersect = new IntersectQuery(selectA, selectB);
            var orderBy = new OrderedQuery(intersect, EntityComparer.Default, "attr1 desc, attr2 asc, attr3");
            Assert.IsTrue(string.Equals(
                "select (" + Environment.NewLine +
                "select \"a\"" + Environment.NewLine +
                "intersect" + Environment.NewLine +
                "select \"b\"" + Environment.NewLine +
                ")" + Environment.NewLine +
                "order by attr1 desc, attr2 asc, attr3",
                orderBy.Text, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
