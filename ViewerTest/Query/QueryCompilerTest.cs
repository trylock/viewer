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
        private Mock<IRuntime> _runtime;
        private QueryCompiler _compiler;

        [TestInitialize]
        public void Setup()
        {
            _factory = new Mock<IQueryFactory>();
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
            var queryMock = new Mock<IQuery>();
            queryMock
                .Setup(mock => mock.Where(It.IsAny<Func<IEntity, bool>>()))
                .Returns(queryMock.Object);
            queryMock
                .Setup(mock => mock.Path(It.IsAny<string>()))
                .Returns(queryMock.Object);
            _factory.Setup(mock => mock.CreateQuery()).Returns(queryMock.Object);

            _compiler.Compile(new StringReader("SELECT \"a/b/**/cd/e*/f?\""));

            queryMock.Verify(mock => mock.Path("a/b/**/cd/e*/f?"), Times.Once);
        }

        [TestMethod]
        public void Compile_WhereConstantExpressionAlwaysTrue()
        {
            var queryMock = new Mock<IQuery>();
            queryMock
                .Setup(mock => mock.Where(It.IsAny<Func<IEntity, bool>>()))
                .Returns(queryMock.Object);
            queryMock
                .Setup(mock => mock.Path(It.IsAny<string>()))
                .Returns(queryMock.Object);
            _factory.Setup(mock => mock.CreateQuery()).Returns(queryMock.Object);

            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(1), new IntValue(1)))
                .Returns(new IntValue(1));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE 1 = 1"));

            queryMock.Verify(mock => mock.Path("pattern"), Times.Once);
            queryMock.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate => predicate(null) && predicate(new Entity("test"))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_WhereConstantExpressionAlwaysFalse()
        {
            var queryMock = new Mock<IQuery>();
            queryMock
                .Setup(mock => mock.Where(It.IsAny<Func<IEntity, bool>>()))
                .Returns(queryMock.Object);
            queryMock
                .Setup(mock => mock.Path(It.IsAny<string>()))
                .Returns(queryMock.Object);
            _factory.Setup(mock => mock.CreateQuery()).Returns(queryMock.Object);

            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(1), new IntValue(2)))
                .Returns(new IntValue(null));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE 1 = 2"));
            
            queryMock.Verify(mock => mock.Path("pattern"), Times.Once);
            queryMock.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate => !(predicate(null) && predicate(new Entity("test")))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_WhereComparisonWithAttribute()
        {
            var queryMock = new Mock<IQuery>();
            queryMock
                .Setup(mock => mock.Where(It.IsAny<Func<IEntity, bool>>()))
                .Returns(queryMock.Object);
            queryMock
                .Setup(mock => mock.Path(It.IsAny<string>()))
                .Returns(queryMock.Object);
            _factory.Setup(mock => mock.CreateQuery()).Returns(queryMock.Object);

            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(4), new IntValue(4)))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("=", new IntValue(null), new IntValue(4)))
                .Returns(new IntValue(null));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE test = 4"));

            queryMock.Verify(mock => mock.Path("pattern"), Times.Once);
            queryMock.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate => 
                    !predicate(new Entity("test")) && 
                    predicate(new Entity("test").SetAttribute(new Attribute("test", new IntValue(4), AttributeFlags.None)))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_ComparisonBetweenAttributes()
        {
            var queryMock = new Mock<IQuery>();
            queryMock
                .Setup(mock => mock.Where(It.IsAny<Func<IEntity, bool>>()))
                .Returns(queryMock.Object);
            queryMock
                .Setup(mock => mock.Path(It.IsAny<string>()))
                .Returns(queryMock.Object);
            _factory.Setup(mock => mock.CreateQuery()).Returns(queryMock.Object);

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

            queryMock.Verify(mock => mock.Path("pattern"), Times.Once);
            queryMock.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
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
    }
}
