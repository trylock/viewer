using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Query;

namespace ViewerTest.Query
{
    [TestClass]
    public class RuntimeTest
    {
        private Runtime _runtime;
        private Mock<IFunction> _testFunction;
        private Mock<IValueConverter> _converter;
        private Mock<IQueryErrorListener> _errorListener;

        [TestInitialize]
        public void Setup()
        {
            _converter = new Mock<IValueConverter>();
            _testFunction = new Mock<IFunction>();
            _errorListener = new Mock<IQueryErrorListener>();
            _testFunction
                .Setup(mock => mock.Name)
                .Returns("test");
            _runtime = new Runtime(_converter.Object, _errorListener.Object, new[]{ _testFunction.Object });
        }

        private IExecutionContext Create(params BaseValue[] arguments)
        {
            return new ExecutionContext(arguments, new NullQueryErrorListener(), null, 0, 0);
        }

        [TestMethod]
        public void CallAndFind()
        {
            _testFunction
                .Setup(mock => mock.Arguments)
                .Returns(new[] {TypeId.String, TypeId.String});
            _testFunction
                .Setup(mock => mock.Call(It.Is<ExecutionContext>(args => 
                    args.Count == 2 &&
                    args.Get<StringValue>(0).Value == "1" &&
                    args.Get<StringValue>(1).Value == "test"
                )))
                .Returns(new StringValue("1+test"));

            _converter.Setup(mock => mock.ConvertTo(new IntValue(1), TypeId.String)).Returns(new StringValue("1"));
            _converter.Setup(mock => mock.ConvertTo(new StringValue("test"), TypeId.String)).Returns(new StringValue("test"));

            var result = _runtime.FindAndCall("test", Create(new IntValue(1), new StringValue("test")));
            Assert.AreEqual("1+test", ((StringValue) result).Value);
        }

        [TestMethod]
        public void CallAndFind_UnknownFunction()
        {
            var result = _runtime.FindAndCall("error", Create(new IntValue(4), new StringValue("test")));
            Assert.IsTrue(result.IsNull);

            _errorListener.Verify(mock => mock.OnRuntimeError(0, 0, "Unknown function error(Integer, String)"), Times.Once);
        }

        [TestMethod]
        public void CallAndFind_DifferentNumberOfParameters()
        {
            _testFunction
                .Setup(mock => mock.Arguments)
                .Returns(new[] { TypeId.Integer });

            var result = _runtime.FindAndCall("test", Create(new IntValue(1), new IntValue(2)));
            Assert.IsTrue(result.IsNull);

            _errorListener.Verify(mock => mock.OnRuntimeError(0, 0, "Unknown function test(Integer, Integer)"), Times.Once);
        }

        [TestMethod]
        public void CallAndFind_IncompatibleArgumentTypes()
        {
            _testFunction
                .Setup(mock => mock.Arguments)
                .Returns(new[] { TypeId.Integer });
            _testFunction
                .Setup(mock => mock.Call(It.Is<ExecutionContext>(args =>
                    args.Count == 1 &&
                    args.Get<IntValue>(0).IsNull)
                ))
                .Returns(new StringValue(null));

            _converter
                .Setup(mock => mock.ConvertTo(new StringValue("42"), TypeId.Integer))
                .Returns(new IntValue(null));

            var result = _runtime.FindAndCall("test", Create(new StringValue("42")));
            Assert.IsInstanceOfType(result, typeof(StringValue));
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void CallAndFind_IncompatibleArgumentTypesWithMultipleArguments()
        {
            _testFunction
                .Setup(mock => mock.Arguments)
                .Returns(new[] { TypeId.Integer, TypeId.Integer });
            _testFunction
                .Setup(mock => mock.Call(It.Is<ExecutionContext>(args =>
                    args.Count == 2 &&
                    args.Get<IntValue>(0).IsNull &&
                    args.Get<IntValue>(1).IsNull
                )))
                .Returns(new IntValue(null));

            _converter
                .Setup(mock => mock.ConvertTo(new StringValue("1"), TypeId.Integer))
                .Returns(new IntValue(null));
            _converter
                .Setup(mock => mock.ConvertTo(new StringValue("2"), TypeId.Integer))
                .Returns(new IntValue(null));

            var result = _runtime.FindAndCall("test", Create(new StringValue("1"), new StringValue("2")));
            Assert.IsInstanceOfType(result, typeof(IntValue));
            Assert.IsTrue(result.IsNull);
        }
    }
}
