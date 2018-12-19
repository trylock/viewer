using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.Query;
using Viewer.Query.Execution;
using Viewer.Query.Expressions;
using Viewer.Query.Search;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Query.Execution
{
    [TestClass]
    public class SimpleQueryTest
    {
        private Mock<IEntityManager> _entityManager;
        private Mock<IFileSystem> _fileSystem;
        private Mock<IFileFinder> _fileFinder;
        private Mock<IRuntime> _runtime;
        private Mock<IPriorityComparerFactory> _priorityComparerFactory;
        private Mock<IReadableAttributeStorage> _reader;
        
        private SimpleQuery Create(string pattern)
        {
            var query = new SimpleQuery(
                _entityManager.Object,
                _fileSystem.Object,
                _runtime.Object,
                _priorityComparerFactory.Object,
                pattern
            );
            return query;
        }

        private ValueExpression CreatePredicate(Func<IEntity, bool> func)
        {
            var predicate = new Mock<ValueExpression>();
            predicate
                .Setup(mock => mock.CompilePredicate(It.IsAny<IRuntime>()))
                .Returns(func);
            predicate
                .Setup(mock => mock.ToString())
                .Returns("predicate");
            return predicate.Object;
        }

        [TestInitialize]
        public void Setup()
        {
            _entityManager = new Mock<IEntityManager>();
            _fileSystem = new Mock<IFileSystem>();
            _runtime = new Mock<IRuntime>();
            _priorityComparerFactory = new Mock<IPriorityComparerFactory>();
            _reader = new Mock<IReadableAttributeStorage>();
            _fileFinder = new Mock<IFileFinder>();

            _fileSystem
                .Setup(mock => mock.CreateFileFinder(It.IsAny<string>()))
                .Returns(_fileFinder.Object);
            _fileFinder.Setup(mock => mock.Pattern).Returns(new PathPattern("test"));
            _entityManager.Setup(mock => mock.CreateReader()).Returns(_reader.Object);
        }

        [TestMethod]
        public void Execute_NoMatchingDirectories()
        {
            var predicate = CreatePredicate(entity => true);
            var query = Create("test").AppendPredicate(predicate);

            Assert.AreEqual("select \"test\" where predicate", query.Text);

            _fileFinder
                .Setup(mock => mock.GetDirectories(It.IsAny<IComparer<string>>()))
                .Returns(new string[] { });

            var result = query.Execute(new ExecutionOptions()).ToList();
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Execute_QueryWithPredicate()
        {
            var predicate = CreatePredicate(entity =>
            {
                return entity.GetAttribute("a") == null;
            });
            var query = Create("test").AppendPredicate(predicate);

            Assert.AreEqual("select \"test\" where predicate", query.Text);

            _fileFinder
                .Setup(mock => mock.GetDirectories(It.IsAny<IComparer<string>>()))
                .Returns(new[]
                {
                    "a/b", "a/c"
                });

            _fileSystem
                .Setup(mock => mock.EnumerateFiles("a/b"))
                .Returns(new[] {"a/b/test.jpeg"});
            _fileSystem
                .Setup(mock => mock.EnumerateDirectories("a/b"))
                .Returns(new string[] {});
            _fileSystem
                .Setup(mock => mock.EnumerateFiles("a/c"))
                .Returns(new string[] { "a/c/test2.jpg" });
            _fileSystem
                .Setup(mock => mock.EnumerateDirectories("a/c"))
                .Returns(new string[] { "a/c/dir" });

            var results = new IEntity[]
            {
                new FileEntity("a/b/test.jpeg"),
                new FileEntity("a/c/test2.jpg")
                    .SetAttribute(new Attribute("a", new IntValue(1), AttributeSource.Custom)),
                new DirectoryEntity("a/c/dir"), 
            };
            _reader
                .Setup(mock => mock.Load("a/b/test.jpeg"))
                .Returns(results[0]);
            _reader
                .Setup(mock => mock.Load("a/c/test2.jpg"))
                .Returns(results[1]);
            _reader
                .Setup(mock => mock.Load("a/c/dir"))
                .Returns(results[2]);

            // execute the query
            var result = query.Execute(new ExecutionOptions()).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(result[0], results[0]);
            Assert.IsInstanceOfType(result[1], typeof(DirectoryEntity));
            Assert.AreEqual(result[1].Path, results[2].Path);
        }

        [TestMethod]
        public void Group_GroupByExpression()
        {
            var expression = new Mock<ValueExpression>();
            expression
                .Setup(mock => mock.ToString())
                .Returns("x + 1");
            expression
                .Setup(mock => mock.CompileFunction(It.IsAny<IRuntime>()))
                .Returns(entity => new IntValue(entity.GetValue<IntValue>("x")?.Value + 1));
            var query = Create("test").WithGroupFunction(expression.Object);

            Assert.AreEqual("select \"test\" group by x + 1", query.Text);

            var entity1 = new FileEntity("test");
            var entity2 = new FileEntity("test")
                .SetAttribute(new Attribute("x", new IntValue(2), AttributeSource.Custom));

            Assert.IsNull(((IntValue)query.GetGroup(entity1)).Value);
            Assert.AreEqual(3, ((IntValue)query.GetGroup(entity2)).Value);
        }
    }
}
