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
        private Mock<IQueryViewRepository> _queryViewRepository;
        private Mock<IQueryFactory> _factory;
        private Mock<IQuery> _query;
        private Mock<IRuntime> _runtime;
        private QueryCompiler _compiler;

        [TestInitialize]
        public void Setup()
        {
            _queryViewRepository = new Mock<IQueryViewRepository>();

            _query = new Mock<IQuery>();
            _query
                .Setup(mock => mock.Where(It.IsAny<Func<IEntity, bool>>()))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.Except(It.IsAny<IExecutableQuery>()))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.Intersect(It.IsAny<IExecutableQuery>()))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.Union(It.IsAny<IExecutableQuery>()))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.WithComparer(It.IsAny<IComparer<IEntity>>()))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.WithText(It.IsAny<string>()))
                .Returns(_query.Object);

            _factory = new Mock<IQueryFactory>();
            _factory
                .Setup(mock => mock.CreateQuery())
                .Returns(_query.Object);
            _factory
                .Setup(mock => mock.CreateQuery(It.IsAny<string>()))
                .Returns(_query.Object);
            
            _runtime = new Mock<IRuntime>();
            _compiler = new QueryCompiler(_factory.Object, _runtime.Object, _queryViewRepository.Object, new NullErrorListener());
        }

        [TestMethod]
        public void Compile_SelectOnly()
        {
            _compiler.Compile(new StringReader("SELECT \"a/b/**/cd/e*/f?\""), new NullErrorListener());

            _factory.Verify(mock => mock.CreateQuery("a/b/**/cd/e*/f?"), Times.Once);
        }

        private IExecutionContext Context(params BaseValue[] args)
        {
            return It.Is<IExecutionContext>(context => 
                context.Count == args.Length && 
                context.SequenceEqual(args));
        }
        
        [TestMethod]
        public void Compile_WhereConstantExpressionAlwaysTrue()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(1), new IntValue(1))))
                .Returns(new IntValue(1));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE 1 = 1"), new NullErrorListener());

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate => predicate(null) && predicate(new FileEntity("test"))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_WhereConstantExpressionAlwaysFalse()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(1), new IntValue(2))))
                .Returns(new IntValue(null));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE 1 = 2"), new NullErrorListener());

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate => !(predicate(null) && predicate(new FileEntity("test")))
            )), Times.Once);
        }
        
        [TestMethod]
        public void Compile_WhereComparisonWithAttribute()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(4), new IntValue(4))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(null), new IntValue(4))))
                .Returns(new IntValue(null));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE test = 4"), new NullErrorListener());

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate => 
                    !predicate(new FileEntity("test")) && 
                    predicate(new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(4), AttributeSource.Custom)))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_ComparisonBetweenAttributes()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(null), new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(1), new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(1), new IntValue(4))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(4), new IntValue(4))))
                .Returns(new IntValue(1));

            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE test1 = test2"), new NullErrorListener());

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(
                predicate =>
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test").SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))
                        .SetAttribute(new Attribute("test2", new IntValue(4), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(4), AttributeSource.Custom))
                        .SetAttribute(new Attribute("test2", new IntValue(4), AttributeSource.Custom)))
            )), Times.Once);
        }

        [TestMethod]
        public void Compile_OrderBySimpleKey()
        {
            _compiler.Compile(new StringReader("SELECT \"pattern\" ORDER BY test"), new NullErrorListener());

            _query.Verify(mock => mock.WithComparer(It.Is<IComparer<IEntity>>(comparer => 
                comparer.Compare(
                    new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)),
                    new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(2), AttributeSource.Custom))
                ) < 0 &&
                comparer.Compare(
                    new FileEntity("test"),
                    new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(2), AttributeSource.Custom))
                ) > 0
            )));
        }

        [TestMethod]
        public void Compile_OrderByComplexExpression()
        {
            _compiler.Compile(new StringReader("SELECT \"pattern\" ORDER BY test / 3 DESC"), new NullErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(1), new IntValue(3))))
                .Returns(new IntValue(1 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(2), new IntValue(3))))
                .Returns(new IntValue(2 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(3), new IntValue(3))))
                .Returns(new IntValue(3 / 3));

            _query.Verify(mock => mock.WithComparer(It.Is<IComparer<IEntity>>(comparer =>
                comparer.Compare(
                    new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)),
                    new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(2), AttributeSource.Custom))
                ) == 0 &&
                comparer.Compare(
                    new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)),
                    new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(3), AttributeSource.Custom))
                ) > 0
            )));
        }

        [TestMethod]
        public void Compile_OrderByMultipleExpressions()
        {
            _compiler.Compile(new StringReader("SELECT \"pattern\" ORDER BY test1 / 3 DESC, test2"), new NullErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(1), new IntValue(3))))
                .Returns(new IntValue(1 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(2), new IntValue(3))))
                .Returns(new IntValue(2 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(3), new IntValue(3))))
                .Returns(new IntValue(3 / 3));

            _query.Verify(mock => mock.WithComparer(It.Is<IComparer<IEntity>>(comparer =>
                comparer.Compare(
                    new FileEntity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))
                        .SetAttribute(new Attribute("test2", new RealValue(3.14), AttributeSource.Custom)),
                    new FileEntity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(2), AttributeSource.Custom))
                        .SetAttribute(new Attribute("test2", new RealValue(2.20), AttributeSource.Custom))
                ) > 0 
            )));
        }

        [TestMethod]
        public void Compile_CustomFunctionInvocation()
        {
            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE test(1, \"value\")"), new NullErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("test", Context(new IntValue(1), new StringValue("value"))))
                .Returns(new RealValue(3.14));

            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate => 
                predicate(new FileEntity("test"))
            )));
        }

        [TestMethod]
        public void Compile_Subquery()
        {
            _compiler.Compile(
                new StringReader("SELECT (SELECT \"pattern\" WHERE test = 1 ORDER BY test DESC) WHERE test2 = 2"), new NullErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(1), new IntValue(1))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(2), new IntValue(2))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(null), new IntValue(1))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(null), new IntValue(2))))
                .Returns(new IntValue(null));

            _factory.Verify(mock => mock.CreateQuery("pattern"));
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate => 
                !predicate(new FileEntity("test").SetAttribute(new Attribute("test2", new IntValue(2), AttributeSource.Custom))) &&
                predicate(new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)))
            )));
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate =>
                predicate(new FileEntity("test").SetAttribute(new Attribute("test2", new IntValue(2), AttributeSource.Custom))) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)))
            )));
        }

        [TestMethod]
        public void Compile_AdditionHasLowerPrecedenceThanMultiplication()
        {
            _compiler.Compile(new StringReader("SELECT \"pattern\" WHERE 1 + 2 * 3 = 7"), new NullErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("+", Context(new IntValue(1), new IntValue(6))))
                .Returns(new IntValue(7));
            _runtime
                .Setup(mock => mock.FindAndCall("*", Context(new IntValue(2), new IntValue(3))))
                .Returns(new IntValue(6));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(7), new IntValue(7))))
                .Returns(new IntValue(1));

            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate => predicate(new FileEntity("test")))));
        }

        [TestMethod]
        public void Compile_SetOperations()
        {
            var order = 0;

            // intersect has higher precedence than any other set operation
            _query
                .Setup(mock => mock.Intersect(It.IsAny<IExecutableQuery>()))
                .Callback(() => Assert.AreEqual(0, order++))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.Union(It.IsAny<IExecutableQuery>()))
                .Callback(() => Assert.AreEqual(1, order++))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.Except(It.IsAny<IExecutableQuery>()))
                .Callback(() => Assert.AreEqual(2, order++))
                .Returns(_query.Object);

            _compiler.Compile(new StringReader("SELECT \"a\" UNION SELECT \"b\" INTERSECT SELECT \"c\" EXCEPT SELECT \"d\""), new NullErrorListener());
        }

        [TestMethod]
        public void Compile_ChangePrecedenceOfSetOperators()
        {
            var order = 0;
            
            _query
                .Setup(mock => mock.Union(It.IsAny<IExecutableQuery>()))
                .Callback(() => Assert.AreEqual(0, order++))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.Except(It.IsAny<IExecutableQuery>()))
                .Callback(() => Assert.AreEqual(1, order++))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.Intersect(It.IsAny<IExecutableQuery>()))
                .Callback(() => Assert.AreEqual(2, order++))
                .Returns(_query.Object);

            _compiler.Compile(new StringReader("(SELECT \"a\" UNION SELECT \"b\") INTERSECT (SELECT \"c\" EXCEPT SELECT \"d\")"), new NullErrorListener());
        }

        [TestMethod]
        public void Compile_AttributeIdentifierWithSpecialCharacters()
        {
            _compiler.Compile(new StringReader("SELECT \"a\" WHERE `identifier with spaces and special characters ěščřžýáíéůú`"), new NullErrorListener());

            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate => 
                !predicate(new FileEntity("test")) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("`identifier with spaces and special characters ěščřžýáíéůú`", new IntValue(1), AttributeSource.Custom))) &&
                predicate(new FileEntity("test").SetAttribute(new Attribute("identifier with spaces and special characters ěščřžýáíéůú", new IntValue(1), AttributeSource.Custom)))
            )));
        }

        [TestMethod]
        public void Compile_KeywordsAreCaseInsensitive()
        {
            _compiler.Compile(new StringReader("SelEcT \"a\" wHEre b ORDer bY c"), new NullErrorListener());

            _factory.Verify(mock => mock.CreateQuery("a"));
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate =>
                !predicate(new FileEntity("test")) &&
                predicate(new FileEntity("test").SetAttribute(new Attribute("b", new IntValue(1), AttributeSource.Custom)))
            )));
        }

        [TestMethod]
        public void Compile_ExpressionWithAndOr()
        {
            _compiler.Compile(new StringReader("SELECT \"a\" WHERE a OR b AND c"), new NullErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("and", Context(new IntValue(2), new IntValue(3))))
                .Returns(new IntValue(3));
            _runtime
                .Setup(mock => mock.FindAndCall("and", Context(new IntValue(2), new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("and", Context(new IntValue(null), new IntValue(3))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("and", Context(new IntValue(null), new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("or", Context(new IntValue(1), new IntValue(3))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("or", Context(new IntValue(1), new IntValue(null))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("or", Context(new IntValue(null), new IntValue(3))))
                .Returns(new IntValue(3));
            _runtime
                .Setup(mock => mock.FindAndCall("or", Context(new IntValue(null), new IntValue(null))))
                .Returns(new IntValue(null));

            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate => 
                !predicate(new FileEntity("test")) &&
                predicate(new FileEntity("test").SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom))) &&
                predicate(new FileEntity("test")
                    .SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))
                    .SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom))) &&
                predicate(new FileEntity("test")
                    .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))
                    .SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))) &&
                predicate(new FileEntity("test")
                    .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))
                    .SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom))) &&
                predicate(new FileEntity("test")
                    .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))
                    .SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))
                    .SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom)))
            )));
        }

        [TestMethod]
        public void Compile_ParenthesesInLogicalExpression()
        {
            _compiler.Compile(new StringReader("select \"a\" where (a or b) and c"), new NullErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("and", Context(new IntValue(1), new IntValue(3))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("and", Context(new IntValue(1), new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("and", Context(new IntValue(null), new IntValue(3))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("and", Context(new IntValue(null), new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("or", Context(new IntValue(1), new IntValue(2))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("or", Context(new IntValue(1), new IntValue(null))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("or", Context(new IntValue(null), new IntValue(2))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("or", Context(new IntValue(null), new IntValue(null))))
                .Returns(new IntValue(null));

            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate =>
                !predicate(new FileEntity("test")) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom))) &&
                predicate(new FileEntity("test")
                    .SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))
                    .SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom))) &&
                !predicate(new FileEntity("test")
                    .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))
                    .SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))) &&
                predicate(new FileEntity("test")
                    .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))
                    .SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom))) &&
                predicate(new FileEntity("test")
                    .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))
                    .SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))
                    .SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom)))
            )));
        }

        [TestMethod]
        public void Compile_Not()
        {
            _compiler.Compile(new StringReader("select \"a\" where not a"), new NullErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("not", Context(new IntValue(null))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("not", Context(new IntValue(1))))
                .Returns(new IntValue(null));

            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate => 
                predicate(new FileEntity("test")) &&
                predicate(new FileEntity("test").SetAttribute(new Attribute("a", new IntValue(null), AttributeSource.Custom))) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom)))
            )));
        }

        [TestMethod]
        public void Compile_LowerCaseDescOrdering()
        {
            _compiler.Compile(new StringReader("select \"a\" order by a desc"), new NullErrorListener());

            _query.Verify(mock => mock.WithComparer(It.Is<IComparer<IEntity>>(comparer =>
                comparer.Compare(
                    new FileEntity("a").SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom)),
                    new FileEntity("b").SetAttribute(new Attribute("a", new IntValue(2), AttributeSource.Custom))
                ) > 0
            )));
        }

        [TestMethod]
        public void Compile_View()
        {
            _queryViewRepository
                .Setup(mock => mock.Find("view"))
                .Returns(new QueryView("view", "select \"path\" where test1 = 1", null));

            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(null), new IntValue(1))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(null), new IntValue(2))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(2), new IntValue(2))))
                .Returns(new IntValue(2));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(1), new IntValue(1))))
                .Returns(new IntValue(1));

            _compiler.Compile(new StringReader("select view where test2 = 2"), new NullErrorListener());
            
            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate =>
                !predicate(new FileEntity("test")) &&
                predicate(new FileEntity("test").SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("test2", new IntValue(2), AttributeSource.Custom))) 
            )));

            _query.Verify(mock => mock.Where(It.Is<Func<IEntity, bool>>(predicate =>
                !predicate(new FileEntity("test")) &&
                !predicate(new FileEntity("test").SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))) &&
                predicate(new FileEntity("test").SetAttribute(new Attribute("test2", new IntValue(2), AttributeSource.Custom)))
            )));
        }

        [TestMethod]
        public void Compile_NestedQueryExpression()
        {
            _query
                .Setup(mock => mock.Union(It.IsAny<IQuery>()))
                .Returns(_query.Object);

            _compiler.Compile(new StringReader("select (select \"a\" union select \"b\")"), new NullErrorListener());

            _query.Verify(mock => mock.Union(It.IsAny<IExecutableQuery>()), Times.Once);
            _factory.Verify(mock => mock.CreateQuery("a"), Times.Once);
            _factory.Verify(mock => mock.CreateQuery("b"), Times.Once);
        }

        [TestMethod]
        public void Compile_CorrectlySetQueryText()
        {
            _compiler.Compile(new StringReader("select \"a\" where tag"), new NullErrorListener());

            _query.Verify(mock => mock.WithText("select \"a\" where tag"), Times.Once);
        }

        [TestMethod]
        public void Compile_ParseTheEntireInput()
        {
            var listener = new Mock<IErrorListener>();
            _compiler.Compile(new StringReader("select \"a\" asdf"), listener.Object);

            listener.Verify(mock => mock.ReportCompilerError(1, 11, It.IsAny<string>()), Times.AtLeastOnce);
        }
    }
}
