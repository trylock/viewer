using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Query;
using Viewer.QueryRuntime;

namespace ViewerTest.QueryRuntime
{
    [TestClass]
    public class IntValueDivisionFunctionTest
    {
        private Mock<IExecutionContext> _context;
        
        [TestInitialize]
        public void Setup()
        {
            _context = new Mock<IExecutionContext>();
        }

        [TestMethod]
        public void Call_DiviteNonZeroInts()
        {
            _context.Setup(mock => mock.Get<IntValue>(0)).Returns(new IntValue(5));
            _context.Setup(mock => mock.Get<IntValue>(1)).Returns(new IntValue(2));

            var function = new IntValueDivisionFunction();
            var result = function.Call(_context.Object);
            Assert.IsInstanceOfType(result, typeof(IntValue));
            Assert.AreEqual(5 / 2, ((IntValue) result).Value);
        }

        [TestMethod]
        public void Call_DivideByZero()
        {
            _context.Setup(mock => mock.Get<IntValue>(0)).Returns(new IntValue(5));
            _context.Setup(mock => mock.Get<IntValue>(1)).Returns(new IntValue(0));
            _context.Setup(mock => mock.Error(It.IsAny<string>())).Returns(new IntValue(null));

            var function = new IntValueDivisionFunction();
            var result = function.Call(_context.Object);
            Assert.IsInstanceOfType(result, typeof(IntValue));
            Assert.IsTrue(result.IsNull);

            _context.Verify(mock => mock.Error("Division by zero."), Times.Once);
        }
    }
}
