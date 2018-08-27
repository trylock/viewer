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
        
        [TestInitialize]
        public void Setup()
        {
            _converter = new Mock<IValueConverter>();
            _testFunction = new Mock<IFunction>();
            _testFunction
                .Setup(mock => mock.Name)
                .Returns("test");
            _runtime = new Runtime(_converter.Object, new IFunction[]{ _testFunction.Object });
        }

        [TestMethod]
        public void CallAndFind()
        {
            _testFunction
                .Setup(mock => mock.Arguments)
                .Returns(new[] {TypeId.String, TypeId.String});
            _testFunction
                .Setup(mock => mock.Call(It.Is<ArgumentList>(args => 
                    args.Count == 2 &&
                    args.Get<StringValue>(0).Value == "1" &&
                    args.Get<StringValue>(1).Value == "test"
                )))
                .Returns(new StringValue("1+test"));

            _converter.Setup(mock => mock.ConvertTo(new IntValue(1), TypeId.String)).Returns(new StringValue("1"));
            _converter.Setup(mock => mock.ConvertTo(new StringValue("test"), TypeId.String)).Returns(new StringValue("test"));

            var result = _runtime.FindAndCall("test", new IntValue(1), new StringValue("test"));
            Assert.AreEqual("1+test", ((StringValue) result).Value);
        }

        [TestMethod]
        [ExpectedException(typeof(QueryRuntimeException), "Unknown function error(Integer, String)")]
        public void CallAndFind_UnknownFunction()
        {
            _runtime.FindAndCall("error", new IntValue(4), new StringValue("test"));
        }

        [TestMethod]
        [ExpectedException(typeof(QueryRuntimeException), "Unknown function test(Integer, Integer)")]
        public void CallAndFind_DifferentNumberOfParameters()
        {
            _testFunction
                .Setup(mock => mock.Arguments)
                .Returns(new[] { TypeId.Integer });

            _runtime.FindAndCall("test", new IntValue(1), new IntValue(2));
        }

        [TestMethod]
        public void CallAndFind_IncompatibleArgumentTypes()
        {
            _testFunction
                .Setup(mock => mock.Arguments)
                .Returns(new[] { TypeId.Integer });
            _testFunction
                .Setup(mock => mock.Call(It.Is<ArgumentList>(args =>
                    args.Count == 1 &&
                    args.Get<IntValue>(0).IsNull)
                ))
                .Returns(new StringValue(null));

            _converter
                .Setup(mock => mock.ConvertTo(new StringValue("42"), TypeId.Integer))
                .Returns(new IntValue(null));

            var result = _runtime.FindAndCall("test", new StringValue("42"));
            Assert.IsInstanceOfType(result, typeof(StringValue));
            Assert.IsTrue(result.IsNull);
        }
    }
}
