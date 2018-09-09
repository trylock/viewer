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
using Viewer.Query.QueryExpression;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Query.QueryExpression
{
    [TestClass]
    public class WhereQueryTest
    {
        [TestMethod]
        public void Text_SourceDoesNotHaveATextualRepresentation()
        {
            var source = new Mock<IExecutableQuery>();
            source
                .Setup(mock => mock.Text)
                .Returns<string>(null);
            var where = new WhereQuery(source.Object, _ => true, "true");
            Assert.IsNull(where.Text);
        }

        [TestMethod]
        public void Text_WhenTheSourceIsASelect()
        {
            var select = new SelectQuery(
                new Mock<IFileSystem>().Object,
                new Mock<IEntityManager>().Object,
                "pattern",
                FileAttributes.System);
            var where = new WhereQuery(select, entity => entity.GetValue<IntValue>("attr") != null, "int(attr)");
            Assert.IsTrue(string.Equals(
                "select \"pattern\"" + Environment.NewLine +
                "where int(attr)",
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
            var where = new WhereQuery(queryView, entity => entity.GetValue<IntValue>("attr") != null, "int(attr)");
            Assert.IsTrue(string.Equals(
                "select viewName" + Environment.NewLine +
                "where int(attr)",
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
            var where = new WhereQuery(union, entity => entity.GetValue<IntValue>("attr") != null, "int(attr)");
            Assert.IsTrue(string.Equals(
                "select (" + Environment.NewLine +
                "select \"a\"" + Environment.NewLine +
                "union" + Environment.NewLine +
                "select \"b\"" + Environment.NewLine +
                ")" + Environment.NewLine +
                "where int(attr)",
                where.Text, StringComparison.CurrentCultureIgnoreCase));
        }
        
        [TestMethod]
        public void Match_EntityPredicate()
        {
            var entities = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2").SetAttribute(new Attribute("attr", new IntValue(4), AttributeSource.Custom)),
            };
            var query = new WhereQuery(new MemoryQuery(entities), entity => entity.GetValue<IntValue>("attr") != null, "attr");
            Assert.IsFalse(query.Match(entities[0]));
            Assert.IsTrue(query.Match(entities[1]));
            Assert.IsFalse(query.Match(new FileEntity("test1").SetAttribute(new Attribute("attr", new IntValue(4), AttributeSource.Custom))));
        }
    }
}
