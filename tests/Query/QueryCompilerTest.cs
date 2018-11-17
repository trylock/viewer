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
using Viewer.Query.Expressions;
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

            // setup the query mock so that all factory methods return the same query mock
            _query = new Mock<IQuery>();
            _query
                .Setup(mock => mock.Where(It.IsAny<ValueExpression>()))
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
                .Setup(mock => mock.WithComparer(It.IsAny<IComparer<IEntity>>(), It.IsAny<string>()))
                .Returns(_query.Object);
            _query
                .Setup(mock => mock.View(It.IsAny<string>()))
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
            _compiler = new QueryCompiler(_factory.Object, _runtime.Object, _queryViewRepository.Object, new NullQueryErrorListener());
        }

        [TestMethod]
        public void Compile_SelectOnly()
        {
            _compiler.Compile(new StringReader("SELECT \"a/b/**/cd/e*/f?\""), new NullQueryErrorListener());

            _factory.Verify(mock => mock.CreateQuery("a/b/**/cd/e*/f?"), Times.Once);
        }

        private IExecutionContext Context(params BaseValue[] args)
        {
            return It.Is<IExecutionContext>(context => 
                context.Count == args.Length && 
                context.SequenceEqual(args));
        }

        private ValueExpression CheckPredicate(Func<Func<IEntity, bool>, bool> testPredicateFunction)
        {
            return It.Is<ValueExpression>(expression =>
                testPredicateFunction(expression.CompilePredicate(_runtime.Object)) 
            );
        }

        [TestMethod]
        public void Compile_WhereConstantExpressionAlwaysTrue()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(1), new IntValue(1))))
                .Returns(new IntValue(1));

            const string queryText = "SELECT \"pattern\" WHERE 1 = 1";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(
                CheckPredicate(
                    predicate => predicate(null) && predicate(new FileEntity("test"))
                )
            ), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_WhereConstantExpressionAlwaysFalse()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(1), new IntValue(2))))
                .Returns(new IntValue(null));

            const string queryText = "SELECT \"pattern\" WHERE 1 = 2";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(
                CheckPredicate(
                    predicate => !(predicate(null) && predicate(new FileEntity("test")))
                )
            ), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
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

            const string queryText = "SELECT \"pattern\" WHERE test = 4";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);
            _query.Verify(mock => mock.Where(
                CheckPredicate(
                    predicate => 
                        !predicate(new FileEntity("test")) && 
                        predicate(new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(4), AttributeSource.Custom)))
                )
            ), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_RealValueLiteral()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new RealValue(3.14159), new IntValue(null))))
                .Returns(new RealValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new RealValue(3.14159), new RealValue(3.14159))))
                .Returns(new RealValue(3.14159));

            const string queryText = "SELECT \"a\" WHERE 3.14159 = x";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.Where(
                CheckPredicate(
                    predicate =>
                        !predicate(new FileEntity("test")) &&
                        predicate(new FileEntity("test")
                            .SetAttribute(new Attribute("x", new RealValue(3.14159), AttributeSource.Custom)))
                )
            ), Times.Once);
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

            const string queryText = "SELECT \"pattern\" WHERE test1 = test2";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);

            _query.Verify(mock => mock.Where(
                CheckPredicate(
                    predicate =>
                        !predicate(new FileEntity("test")) &&
                        !predicate(new FileEntity("test").SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))) &&
                        !predicate(new FileEntity("test")
                            .SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))
                            .SetAttribute(new Attribute("test2", new IntValue(4), AttributeSource.Custom))) &&
                        predicate(new FileEntity("test")
                            .SetAttribute(new Attribute("test1", new IntValue(4), AttributeSource.Custom))
                            .SetAttribute(new Attribute("test2", new IntValue(4), AttributeSource.Custom)))
                )
            ), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_ComparisonIsNonAssociative()
        {
            var listener = new Mock<IQueryErrorListener>();
            
            const string queryText = "SELECT \"pattern\" WHERE 1 = 1 < 2";
            var result = _compiler.Compile(new StringReader(queryText), listener.Object);
            
            Assert.IsNull(result);

            listener.Verify(mock => mock.BeforeCompilation());
            listener.Verify(mock => mock.OnCompilerError(1, 29, It.IsAny<string>()));
            listener.Verify(mock => mock.AfterCompilation());
            listener.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_OrderBySimpleKey()
        {
            const string queryText = "SELECT \"pattern\" ORDER BY test";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.WithComparer(
                It.Is<IComparer<IEntity>>(comparer => 
                    comparer.Compare(
                        new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)),
                        new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(2), AttributeSource.Custom))
                    ) < 0 &&
                    comparer.Compare(
                        new FileEntity("test"),
                        new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(2), AttributeSource.Custom))
                    ) > 0
                ), 
                "test"
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_OrderByComplexExpression()
        {
            const string queryText = "SELECT \"pattern\" ORDER BY test / 3 DESC";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(1), new IntValue(3))))
                .Returns(new IntValue(1 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(2), new IntValue(3))))
                .Returns(new IntValue(2 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(3), new IntValue(3))))
                .Returns(new IntValue(3 / 3));

            _query.Verify(mock => mock.WithComparer(
                It.Is<IComparer<IEntity>>(comparer =>
                    comparer.Compare(
                        new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)),
                        new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(2), AttributeSource.Custom))
                    ) == 0 &&
                    comparer.Compare(
                        new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)),
                        new FileEntity("test").SetAttribute(new Attribute("test", new IntValue(3), AttributeSource.Custom))
                    ) > 0
                ), 
                "test / 3 DESC"
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_OrderByMultipleExpressions()
        {
            const string queryText = "SELECT \"pattern\" ORDER BY test1 / 3 DESC, test2";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(1), new IntValue(3))))
                .Returns(new IntValue(1 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(2), new IntValue(3))))
                .Returns(new IntValue(2 / 3));
            _runtime
                .Setup(mock => mock.FindAndCall("/", Context(new IntValue(3), new IntValue(3))))
                .Returns(new IntValue(3 / 3));

            _query.Verify(mock => mock.WithComparer(
                It.Is<IComparer<IEntity>>(comparer =>
                    comparer.Compare(
                        new FileEntity("test")
                            .SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))
                            .SetAttribute(new Attribute("test2", new RealValue(3.14), AttributeSource.Custom)),
                        new FileEntity("test")
                            .SetAttribute(new Attribute("test1", new IntValue(2), AttributeSource.Custom))
                            .SetAttribute(new Attribute("test2", new RealValue(2.20), AttributeSource.Custom))
                    ) > 0 
                ), 
                "test1 / 3 DESC, test2"
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_CustomFunctionInvocation()
        {
            const string queryText = "SELECT \"pattern\" WHERE test(1, \"value\")";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("test", Context(new IntValue(1), new StringValue("value"))))
                .Returns(new RealValue(3.14));

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate => 
                    predicate(new FileEntity("test"))
                )
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);

            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_Subquery()
        {
            const string queryText = "SELECT (SELECT \"pattern\" WHERE test = 1 ORDER BY test DESC) WHERE test2 = 2";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

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

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate => 
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test2", new IntValue(2), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test2", new IntValue(2), AttributeSource.Custom))) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithComparer(
                It.Is<IComparer<IEntity>>(comparer => 
                    comparer.Compare(
                        new FileEntity("test1")
                            .SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)),
                        new FileEntity("test2")
                            .SetAttribute(new Attribute("test", new IntValue(2), AttributeSource.Custom))
                    ) > 0
                ),
                "test DESC"
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_AdditionHasLowerPrecedenceThanMultiplication()
        {
            const string queryText = "SELECT \"pattern\" WHERE 1 + 2 * 3 = 7";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("+", Context(new IntValue(1), new IntValue(6))))
                .Returns(new IntValue(7));
            _runtime
                .Setup(mock => mock.FindAndCall("*", Context(new IntValue(2), new IntValue(3))))
                .Returns(new IntValue(6));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(7), new IntValue(7))))
                .Returns(new IntValue(1));

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate => predicate(new FileEntity("test")))
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_AdditionSubtractionIsLeftAssociative()
        {
            const string queryText = "SELECT \"pattern\" WHERE 1 + 2 * 3 - 4 + 5 = 8";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("+", Context(new IntValue(1), new IntValue(6))))
                .Returns(new IntValue(7));
            _runtime
                .Setup(mock => mock.FindAndCall("*", Context(new IntValue(2), new IntValue(3))))
                .Returns(new IntValue(6));
            _runtime
                .Setup(mock => mock.FindAndCall("-", Context(new IntValue(7), new IntValue(4))))
                .Returns(new IntValue(3));
            _runtime
                .Setup(mock => mock.FindAndCall("+", Context(new IntValue(3), new IntValue(5))))
                .Returns(new IntValue(8));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(8), new IntValue(8))))
                .Returns(new IntValue(8));

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate => predicate(new FileEntity("test")))
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
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

            const string queryText = "SELECT \"a\" UNION SELECT \"b\" INTERSECT SELECT \"c\" EXCEPT SELECT \"d\"";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.Union(It.IsAny<IExecutableQuery>()), Times.Once);
            _query.Verify(mock => mock.Intersect(It.IsAny<IExecutableQuery>()), Times.Once);
            _query.Verify(mock => mock.Except(It.IsAny<IExecutableQuery>()), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
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

            _compiler.Compile(new StringReader("(SELECT \"a\" UNION SELECT \"b\") INTERSECT (SELECT \"c\" EXCEPT SELECT \"d\")"), new NullQueryErrorListener());
        }

        [TestMethod]
        public void Compile_AttributeIdentifierWithSpecialCharacters()
        {
            const string queryText = "SELECT \"a\" WHERE `identifier with spaces and special characters ěščřžýáíéůú`";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate => 
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("`identifier with spaces and special characters ěščřžýáíéůú`", new IntValue(1), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("identifier with spaces and special characters ěščřžýáíéůú", new IntValue(1), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_KeywordsAreCaseInsensitive()
        {
            const string queryText = "SelEcT \"a\" wHEre b ORDer bY c";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _factory.Verify(mock => mock.CreateQuery("a"));

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("b", new IntValue(1), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithComparer(
                It.Is<IComparer<IEntity>>(comparer =>
                    comparer.Compare(
                        new FileEntity("test1")
                            .SetAttribute(new Attribute("c", new IntValue(1), AttributeSource.Custom)),
                        new FileEntity("test2")
                            .SetAttribute(new Attribute("c", new IntValue(2), AttributeSource.Custom))
                    ) < 0
                ),
                "c"
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_ExpressionWithAndOr()
        {
            const string queryText = "SELECT \"a\" WHERE a OR b AND c";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

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

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate => 
                    !predicate(new FileEntity("test")) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom))) &&
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
                )
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_ParenthesesInLogicalExpression()
        {
            const string queryText = "select \"a\" where (a or b) and c";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

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

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("b", new IntValue(2), AttributeSource.Custom))) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("c", new IntValue(3), AttributeSource.Custom))) &&
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
                )
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_Not()
        {
            const string queryText = "select \"a\" where not a";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("not", Context(new IntValue(null))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("not", Context(new IntValue(1))))
                .Returns(new IntValue(null));

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate => 
                    predicate(new FileEntity("test")) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(null), AttributeSource.Custom))) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_LowerCaseDescOrdering()
        {
            const string queryText = "select \"a\" order by a desc";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.WithComparer(
                It.Is<IComparer<IEntity>>(comparer =>
                    comparer.Compare(
                        new FileEntity("a")
                            .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom)),
                        new FileEntity("b")
                            .SetAttribute(new Attribute("a", new IntValue(2), AttributeSource.Custom))
                    ) > 0
                ),
                "a desc"
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }
        
        [TestMethod]
        public void Compile_NestedFunctionCallsWithoutParameters()
        {
            const string queryText = "select \"a\" where f(g(h(k()))) = a";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());
            
            _runtime
                .Setup(mock => mock.FindAndCall("k", Context()))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("h", Context(new IntValue(1))))
                .Returns(new IntValue(2));
            _runtime
                .Setup(mock => mock.FindAndCall("g", Context(new IntValue(2))))
                .Returns(new IntValue(3));
            _runtime
                .Setup(mock => mock.FindAndCall("f", Context(new IntValue(3))))
                .Returns(new IntValue(4));

            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(4), new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(4), new IntValue(2))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(4), new IntValue(4))))
                .Returns(new IntValue(4));

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(2), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(4), AttributeSource.Custom)))
                )
            ));
        }

        [TestMethod]
        public void Compile_NestedFunctionCallsWithAttributeNameParameter()
        {
            const string queryText = "select \"a\" where f(g(h(k(a))))";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("k", Context(new IntValue(42))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("h", Context(new IntValue(1))))
                .Returns(new IntValue(2));
            _runtime
                .Setup(mock => mock.FindAndCall("g", Context(new IntValue(2))))
                .Returns(new IntValue(3));
            _runtime
                .Setup(mock => mock.FindAndCall("f", Context(new IntValue(3))))
                .Returns(new IntValue(4));

            _runtime
                .Setup(mock => mock.FindAndCall("k", Context(new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("k", Context(new IntValue(2))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("h", Context(new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("g", Context(new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("f", Context(new IntValue(null))))
                .Returns(new IntValue(null));
            
            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(2), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(42), AttributeSource.Custom)))
                )
            ));
        }

        [TestMethod]
        public void Compile_NestedFunctionCalls()
        {
            const string queryText = "select \"a\" where f(1, g(b(2), 3), h(), a(4, 5, 6)) = a";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("b", Context(new IntValue(2))))
                .Returns(new IntValue(1));
            _runtime
                .Setup(mock => mock.FindAndCall("g", Context(new IntValue(1), new IntValue(3))))
                .Returns(new IntValue(2));
            _runtime
                .Setup(mock => mock.FindAndCall("h", Context()))
                .Returns(new IntValue(3));
            _runtime
                .Setup(mock => mock.FindAndCall("a", Context(new IntValue(4), new IntValue(5), new IntValue(6))))
                .Returns(new IntValue(4));
            _runtime
                .Setup(mock =>
                    mock.FindAndCall("f", Context(new IntValue(1), new IntValue(2), new IntValue(3), new IntValue(4))))
                .Returns(new IntValue(42));

            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(42), new IntValue(null))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(42), new IntValue(41))))
                .Returns(new IntValue(null));
            _runtime
                .Setup(mock => mock.FindAndCall("=", Context(new IntValue(42), new IntValue(42))))
                .Returns(new IntValue(42));

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(41), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(42), AttributeSource.Custom)))
                )
            ));
        }

        [TestMethod]
        public void Compile_View()
        {
            const string queryViewText = "select \"path\" where test1 = 1";
            const string queryText = "select view where test2 = 2";

            _queryViewRepository
                .Setup(mock => mock.Find("view"))
                .Returns(new QueryView("view", queryViewText, null));

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

            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());
            
            _query.Verify(mock => mock.View("view"), Times.Once);
            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test2", new IntValue(2), AttributeSource.Custom))) 
                )
            ));
            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test1", new IntValue(1), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                    .SetAttribute(new Attribute("test2", new IntValue(2), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithText(queryViewText), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_UnterminatedComplexViewIdentifierBeforeEof()
        {
            const string queryViewText = "select \"path\"";
            const string queryText = "select `view";

            _queryViewRepository
                .Setup(mock => mock.Find("view"))
                .Returns(new QueryView("view", queryViewText, null));

            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.View("view"), Times.Once);
            _query.Verify(mock => mock.WithText(queryViewText), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_UnterminatedComplexViewIdentifierBeforeNewLine()
        {
            const string queryViewText = "select \"path\"";
            const string queryText = "select `view\nwhere test";

            _queryViewRepository
                .Setup(mock => mock.Find("view"))
                .Returns(new QueryView("view", queryViewText, null));

            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.View("view"), Times.Once);
            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithText(queryViewText), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_UnterminatedComplexViewIdentifierBeforeCarriageReturn()
        {
            const string queryViewText = "select \"path\"";
            const string queryText = "select `view\rwhere test";

            _queryViewRepository
                .Setup(mock => mock.Find("view"))
                .Returns(new QueryView("view", queryViewText, null));

            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.View("view"), Times.Once);
            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithText(queryViewText), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_UnterminatedComplexAttributeIdentifierBeforeEof()
        {
            const string queryText = "select \"a\" where `some long id";

            var listener = new Mock<IQueryErrorListener>();
            _compiler.Compile(new StringReader(queryText), listener.Object);
            
            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("`some long id", new IntValue(1), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("some long id", new IntValue(2), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();

            listener.Verify(mock => mock.BeforeCompilation());
            listener.Verify(mock => mock.OnCompilerError(1, 17, "Unterminated COMPLEX_ID"), Times.Once);
            listener.Verify(mock => mock.AfterCompilation());
            listener.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_UnterminatedComplexAttributeIdentifierBeforeNewLine()
        {
            const string queryText = "select \"a\" where `some long id\norder by test";

            var listener = new Mock<IQueryErrorListener>();
            _compiler.Compile(new StringReader(queryText), listener.Object);

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("`some long id", new IntValue(1), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("some long id", new IntValue(2), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithComparer(
                It.Is<IComparer<IEntity>>(comparer =>
                    comparer.Compare(
                        new FileEntity("test")
                            .SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)),
                        new FileEntity("test")
                            .SetAttribute(new Attribute("test", new IntValue(2), AttributeSource.Custom))
                    ) < 0
                ),
                "test"
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();

            listener.Verify(mock => mock.BeforeCompilation());
            listener.Verify(mock => mock.OnCompilerError(1, 17, "Unterminated COMPLEX_ID"), Times.Once);
            listener.Verify(mock => mock.AfterCompilation());
            listener.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_UnterminatedComplexAttributeIdentifierBeforecarriageReturn()
        {
            const string queryText = "select \"a\" where `some long id\rorder by test";

            var listener = new Mock<IQueryErrorListener>();
            _compiler.Compile(new StringReader(queryText), listener.Object);

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    !predicate(new FileEntity("test")) &&
                    !predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("`some long id", new IntValue(1), AttributeSource.Custom))) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("some long id", new IntValue(2), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithComparer(
                It.Is<IComparer<IEntity>>(comparer =>
                    comparer.Compare(
                        new FileEntity("test")
                            .SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)),
                        new FileEntity("test")
                            .SetAttribute(new Attribute("test", new IntValue(2), AttributeSource.Custom))
                    ) < 0
                ),
                "test"
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();

            listener.Verify(mock => mock.BeforeCompilation());
            listener.Verify(mock => mock.OnCompilerError(1, 17, "Unterminated COMPLEX_ID"), Times.Once);
            listener.Verify(mock => mock.AfterCompilation());
            listener.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_ComplexFunctionIdentifier()
        {
            const string queryText = "select \"a\" where `some function`(1, 2)";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _runtime
                .Setup(mock => mock.FindAndCall("some function", Context(new IntValue(1), new IntValue(2))))
                .Returns(new IntValue(9000));

            _query.Verify(mock => mock.Where(
                CheckPredicate(predicate =>
                    predicate(new FileEntity("test")) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("test", new IntValue(1), AttributeSource.Custom)))
                )
            ));
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_NestedQueryExpression()
        {
            _query
                .Setup(mock => mock.Union(It.IsAny<IQuery>()))
                .Returns(_query.Object);

            const string queryText = "select (select \"a\" union select \"b\")";
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.Union(It.IsAny<IExecutableQuery>()), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
            _factory.Verify(mock => mock.CreateQuery("a"), Times.Once);
            _factory.Verify(mock => mock.CreateQuery("b"), Times.Once);
        }

        [TestMethod]
        public void Compile_ParseTheEntireInput()
        {
            var listener = new Mock<IQueryErrorListener>();
            _compiler.Compile(new StringReader("select \"a\" asdf"), listener.Object);

            listener.Verify(mock => mock.OnCompilerError(1, 11, It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void Compile_UnknownView()
        {
            var listener = new Mock<IQueryErrorListener>();
            _compiler.Compile(new StringReader("select testView"), listener.Object);

            listener.Verify(mock => mock.OnCompilerError(1, 7, "Unknown view 'testView'"), Times.AtLeastOnce);
        }

        [TestMethod]
        public void Compile_InvalidPathCharactersInPattern()
        {
            var listener = new Mock<IQueryErrorListener>();

            _factory
                .Setup(mock => mock.CreateQuery("file1 < file2"))
                .Throws(new ArgumentException("Pattern contains invalid characters"));

            var result = _compiler.Compile(new StringReader("select \"file1 < file2\""), listener.Object);

            // this error is unrecoverable
            Assert.IsNull(result);

            listener.Verify(mock => mock.OnCompilerError(1, 7, It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void Compile_RecoverFromUnclosedStringAtTheEndOfInput()
        {
            var listener = new Mock<IQueryErrorListener>();
            var result = _compiler.Compile(new StringReader("select \"pattern"), listener.Object);

            // this error is recoverable
            Assert.AreEqual(_query.Object, result);

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);

            // the error is still reported
            listener.Verify(mock => mock.BeforeCompilation());
            listener.Verify(mock => mock.OnCompilerError(1, 7, "Unterminated STRING"), Times.Once);
            listener.Verify(mock => mock.AfterCompilation());
            listener.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_RecoverFromUnclosedStringAtLineBreak()
        {
            var listener = new Mock<IQueryErrorListener>();
            var result = _compiler.Compile(new StringReader("select \"pattern\n\rwhere a"), listener.Object);

            // this error is recoverable
            Assert.AreEqual(_query.Object, result);

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);

            _query.Verify(mock => mock.Where(CheckPredicate(
                predicate =>
                    !predicate(new FileEntity("test")) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom))) 
            )));

            // the error is still reported
            listener.Verify(mock => mock.BeforeCompilation());
            listener.Verify(mock => mock.OnCompilerError(1, 7, "Unterminated STRING"), Times.Once);
            listener.Verify(mock => mock.AfterCompilation());
            listener.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_RecoverFromUnclosedStringAtCarriageReturn()
        {
            var listener = new Mock<IQueryErrorListener>();
            var result = _compiler.Compile(new StringReader("select \"pattern\rwhere a"), listener.Object);

            // this error is recoverable
            Assert.AreEqual(_query.Object, result);

            _factory.Verify(mock => mock.CreateQuery("pattern"), Times.Once);

            _query.Verify(mock => mock.Where(CheckPredicate(
                predicate =>
                    !predicate(new FileEntity("test")) &&
                    predicate(new FileEntity("test")
                        .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom)))
            )));

            // the error is still reported
            listener.Verify(mock => mock.BeforeCompilation());
            listener.Verify(mock => mock.OnCompilerError(1, 7, "Unterminated STRING"), Times.Once);
            listener.Verify(mock => mock.AfterCompilation());
            listener.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_ComplexIdentifierInSelect()
        {
            const string queryViewText = "select \"path\"";
            const string queryText = "select `complex id`";

            _queryViewRepository
                .Setup(mock => mock.Find("complex id"))
                .Returns(new QueryView("complex id", queryViewText, null));
            
            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.View("complex id"), Times.Once);
            _query.Verify(mock => mock.WithText(queryViewText), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Compile_UnicodeAlphanumericCharactersInIdentifier()
        {
            const string identifier = "ěščřžýáíéůúßüな";
            const string queryViewText = "select \"path\"";
            const string queryText = "select " + identifier;

            _queryViewRepository
                .Setup(mock => mock.Find(identifier))
                .Returns(new QueryView(identifier, queryViewText, null));

            _compiler.Compile(new StringReader(queryText), new NullQueryErrorListener());

            _query.Verify(mock => mock.View(identifier), Times.Once);
            _query.Verify(mock => mock.WithText(queryViewText), Times.Once);
            _query.Verify(mock => mock.WithText(queryText), Times.Once);
            _query.VerifyNoOtherCalls();
        }
    }
}
