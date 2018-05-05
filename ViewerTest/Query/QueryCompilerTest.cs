using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Query;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Query
{
    [TestClass]
    public class QueryCompilerTest
    {
        private Mock<IQueryFactory> _factory;
        private Mock<IQuery> _query;
        private Mock<IRuntime> _runtime;
        private QueryCompiler _compiler;

        [TestInitialize]
        public void Setup()
        {
            _factory = new Mock<IQueryFactory>();

            _query = new Mock<IQuery>();
            _query
                .Setup(mock => mock.Where(It.IsAny<Func<IEntity, bool>>()))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.Path(It.IsAny<string>()))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.SetComparer(It.IsAny<IComparer<IEntity>>()))
                .Returns(_query.Object);
            _factory.Setup(mock => mock.CreateQuery()).Returns(_query.Object);

            _runtime = new Mock<IRuntime>();
            _compiler = new QueryCompiler(_factory.Object, _runtime.Object);
        }

        [TestMethod]
        public void Compile_EmptyQuery()
        {
            var emptyQuery = new Viewer.Data.Query(null, null);
            _factory.Setup(mock => mock.CreateQuery()).Returns(emptyQuery);

            var query = _compiler.Compile(new StringReader(""));
            Assert.AreEqual(emptyQuery, query);
        }

        [TestMethod]
        public void Compile_SelectOnly()
        {
            _compiler.Compile(new StringReader("SELECT \"a/b/**/cd/e*/f?\""));

            _query.Verify(mock => mock.Path("a/b/**/cd/e*/f?"), Times.Once);
        }

        [TestMethod]
        public void Compile_WhereConstantExpressionAlwaysTrue()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(1), new IntValue(1)))
                .Returns(new IntValue(1));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE 1 = 1"));

            _query.Verify(mock => mock.Path("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate => predicate(null) && predicate(new Entity("test"))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_WhereConstantExpressionAlwaysFalse()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(1), new IntValue(2)))
                .Returns(new IntValue(null));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE 1 = 2"));

            _query.Verify(mock => mock.Path("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate => !(predicate(null) && predicate(new Entity("test")))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_WhereComparisonWithAttribute()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(4), new IntValue(4)))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(null), new IntValue(4)))
                .Returns(new IntValue(null));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE test = 4"));

            _query.Verify(mock => mock.Path("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate => 
                    !predicate(new Entity("test")) && 
                    predicate(new Entity("test").SetAttribute(new Attribute("test", new IntValue(4), AttributeFlags.None)))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_ComparisonBetweenAttributes()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(null), new IntValue(null)))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(1), new IntValue(null)))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(1), new IntValue(4)))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(4), new IntValue(4)))
                .Returns(new IntValue(1));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE test1 = test2"));

            _query.Verify(mock => mock.Path("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate =>
                    !predicate(new Entity("test")) &&
                    !predicate(new Entity("test").SetAttribute(new Attribute("test1", new IntValue(1), AttributeFlags.None))) &&
                    !predicate(new Entity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(1), AttributeFlags.None))
                        .SetAttribute(new Attribute("test2", new IntValue(4), AttributeFlags.None))) &&
                    predicate(new Entity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(4), AttributeFlags.None))
                        .SetAttribute(new Attribute("test2", new IntValue(4), AttributeFlags.None)))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_OrderBySimpleKey()
        {
            _compiler.Compile(new StringReader("SELECT \"pattern\" ORDER BY test"));

            _query.Verify(mock => mock.SetComparer(It.Is<IComparer<IEntity>>(comparer => 
                comparer.Compare(
                    new Entity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeFlags.None)),
                    new Entity("test").SetAttribute(new Attribute("test", new IntValue(2), AttributeFlags.None))
                ) < 0 &&
                comparer.Compare(
                    new Entity("test"),
                    new Entity("test").SetAttribute(new Attribute("test", new IntValue(2), AttributeFlags.None))
                ) > 0
            )));
        }

        [TestMethod]
        public void Compile_OrderByComplexExpression()
        {
            _compiler.Compile(new StringReader("SELECT \"pattern\" ORDER BY test / 3 DESC"));

            _runtime
                .Setup(mock => mock.FindAndCall("/", new IntValue(1), new IntValue(3)))
                .Returns(new IntValue(1 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", new IntValue(2), new IntValue(3)))
                .Returns(new IntValue(2 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", new IntValue(3), new IntValue(3)))
                .Returns(new IntValue(3 / 3));

            _query.Verify(mock => mock.SetComparer(It.Is<IComparer<IEntity>>(comparer =>
                comparer.Compare(
                    new Entity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeFlags.None)),
                    new Entity("test").SetAttribute(new Attribute("test", new IntValue(2), AttributeFlags.None))
                ) == 0 &&
                comparer.Compare(
                    new Entity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeFlags.None)),
                    new Entity("test").SetAttribute(new Attribute("test", new IntValue(3), AttributeFlags.None))
                ) > 0
            )));
        }

        [TestMethod]
        public void Compile_OrderByMultipleExpressions()
        {
            _compiler.Compile(new StringReader("SELECT \"pattern\" ORDER BY test1 / 3 DESC, test2"));

            _runtime
                .Setup(mock => mock.FindAndCall("/", new IntValue(1), new IntValue(3)))
                .Returns(new IntValue(1 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", new IntValue(2), new IntValue(3)))
                .Returns(new IntValue(2 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", new IntValue(3), new IntValue(3)))
                .Returns(new IntValue(3 / 3));

            _query.Verify(mock => mock.SetComparer(It.Is<IComparer<IEntity>>(comparer =>
                comparer.Compare(
                    new Entity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(1), AttributeFlags.None))
                        .SetAttribute(new Attribute("test2", new RealValue(3.14), AttributeFlags.None)),
                    new Entity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(2), AttributeFlags.None))
                        .SetAttribute(new Attribute("test2", new RealValue(2.20), AttributeFlags.None))
                ) > 0 
            )));
        }

        [TestMethod]
        public void Compile_CustomFunctionInvocation()
        {
            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE test(1, \"value\")"));

            _runtime
                .Setup(mock => mock.FindAndCall("test", new IntValue(1), new StringValue("value")))
                .Returns(new RealValue(3.14));

            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate => 
                predicate(new Entity("test"))
            )));
        }

        [TestMethod]
        public void Compile_Subquery()
        {
            _compiler.Compile(
                new StringReader("SELECT (SELECT \"pattern\" WHERE test = 1 ORDER BY test DESC) WHERE test2 = 2"));

            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(1), new IntValue(1)))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(2), new IntValue(2)))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(null), new IntValue(1)))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(null), new IntValue(2)))
                .Returns(new IntValue(null));

            _query.Verify(mock => mock.Path("pattern"));
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate => 
                !predicate(new Entity("test").SetAttribute(new Attribute("test2", new IntValue(2), AttributeFlags.None))) &&
                predicate(new Entity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeFlags.None)))
            )));
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate =>
                predicate(new Entity("test").SetAttribute(new Attribute("test2", new IntValue(2), AttributeFlags.None))) &&
                !predicate(new Entity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeFlags.None)))
            )));
        }
    }
}
