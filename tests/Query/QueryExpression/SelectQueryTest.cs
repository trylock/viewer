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
using Viewer.Query.QueryExpression;

namespace ViewerTest.Query.QueryExpression
{
    [TestClass]
    public class SelectQueryTest
    {
        private Mock<IFileSystem> _fileSystem;
        private Mock<IEntityManager> _entityManager;
        private SelectQuery _query;

        [TestInitialize]
        public void Setup()
        {
            _fileSystem = new Mock<IFileSystem>();
            _entityManager = new Mock<IEntityManager>();
            _query = new SelectQuery(
                _fileSystem.Object,
                _entityManager.Object,
                "pattern",
                FileAttributes.System);
        }

        [TestMethod]
        public void Text()
        {
            Assert.IsTrue(string.Equals(
                "select \"pattern\"", 
                _query.Text, 
                StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
