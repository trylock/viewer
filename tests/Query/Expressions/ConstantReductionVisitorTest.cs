using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Query;
using Viewer.Query.Expressions;

namespace ViewerTest.Query.Expressions
{
    [TestClass]
    public class ConstantReductionVisitorTest
    {
        private Mock<IRuntime> _runtime;
        private ConstantReductionVisitor _visitor;

        [TestInitialize]
        public void Setup()
        {
            _runtime = new Mock<IRuntime>();
            _visitor = new ConstantReductionVisitor(_runtime.Object);
        }

        [TestMethod]
        public void Reduce_ConstantExpression()
        {
            var expr = new ConstantExpression(1, 2, new IntValue(5));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));
            Assert.AreEqual(expr.Value, ((ConstantExpression)reduced).Value);
            Assert.AreEqual(1, expr.Line);
            Assert.AreEqual(2, expr.Column);
        }

        [TestMethod]
        public void Reduce_AttributeAccessExpression()
        {
            var expr = new AttributeAccessExpression(1, 2, "a");
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(AttributeAccessExpression));
            Assert.AreEqual("a", ((AttributeAccessExpression)reduced).Name);
            Assert.AreEqual(1, reduced.Line);
            Assert.AreEqual(2, reduced.Column);
        }

        [TestMethod]
        public void Reduce_BinaryOperatorWithConstantOperands()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("+", 
                    It.Is<IExecutionContext>(context =>
                        context.Line == 1 &&
                        context.Column == 2 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(1)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(3));

            var expr = new AdditionExpression(1, 2, 
                new ConstantExpression(0, 1, new IntValue(1)), 
                new ConstantExpression(2, 3, new IntValue(2)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));
            Assert.AreEqual(new IntValue(3), ((ConstantExpression)reduced).Value);
            Assert.AreEqual(1, reduced.Line);
            Assert.AreEqual(2, reduced.Column);
        }

        [TestMethod]
        public void Reduce_BinaryOperatorWithTransitivelyConstantOperands()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("+",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 0 &&
                        context.Column == 1 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(1)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(3));
            _runtime
                .Setup(mock => mock.FindAndCall("+",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 0 &&
                        context.Column == 3 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(3)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(5));

            var expr = new AdditionExpression(0, 3,
                new AdditionExpression(0, 1, 
                    new ConstantExpression(0, 0, new IntValue(1)),
                    new ConstantExpression(0, 2, new IntValue(2))), 
                new ConstantExpression(0, 4, new IntValue(2)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));
            Assert.AreEqual(new IntValue(5), ((ConstantExpression)reduced).Value);
            Assert.AreEqual(0, reduced.Line);
            Assert.AreEqual(3, reduced.Column);
        }

        [TestMethod]
        public void Reduce_LeftOperandOfABinaryOperatorIsConstant()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("+",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 0 &&
                        context.Column == 2 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(1)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(3));

            var expr = 
                new AdditionExpression(1, 2, 
                    new AdditionExpression(0, 2,
                        new ConstantExpression(0, 0, new IntValue(1)),
                        new ConstantExpression(0, 0, new IntValue(2))),
                    new AttributeAccessExpression(2, 2, "a"));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(AdditionExpression));

            var addition = reduced as AdditionExpression;
            Assert.AreEqual(new IntValue(3), ((ConstantExpression)addition.Left).Value);
            Assert.AreEqual(0, addition.Left.Line);
            Assert.AreEqual(2, addition.Left.Column);
            
            Assert.AreEqual("a", ((AttributeAccessExpression)addition.Right).Name);
            Assert.AreEqual(2, addition.Right.Line);
            Assert.AreEqual(2, addition.Right.Column);
        }

        [TestMethod]
        public void Reduce_RightOperandOfABinaryOperatorIsConstant()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("+",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 2 &&
                        context.Column == 2 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(1)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(3));

            var expr =
                new AdditionExpression(1, 2,
                    new AttributeAccessExpression(0, 2, "a"),
                    new AdditionExpression(2, 2,
                        new ConstantExpression(0, 0, new IntValue(1)),
                        new ConstantExpression(0, 0, new IntValue(2))));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(AdditionExpression));

            var addition = reduced as AdditionExpression;

            Assert.AreEqual("a", ((AttributeAccessExpression)addition.Left).Name);
            Assert.AreEqual(0, addition.Left.Line);
            Assert.AreEqual(2, addition.Left.Column);

            Assert.AreEqual(new IntValue(3), ((ConstantExpression)addition.Right).Value);
            Assert.AreEqual(2, addition.Right.Line);
            Assert.AreEqual(2, addition.Right.Column);
        }

        [TestMethod]
        public void Reduce_FunctionWithConstantParameters()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("f",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 0 &&
                        context.Column == 0 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(1)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(3));

            var expr =
                new FunctionCallExpression(0, 0, "f", new []
                {
                    new ConstantExpression(0, 1, new IntValue(1)),
                    new ConstantExpression(0, 3, new IntValue(2)),
                });
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression) reduced;

            Assert.AreEqual(new IntValue(3), constant.Value);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_FunctionWithTransitivelyConstantParameters()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("+",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 0 &&
                        context.Column == 2 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(1)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(3));
            _runtime
                .Setup(mock => mock.FindAndCall("f",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 0 &&
                        context.Column == 0 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(3)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(6));

            var expr =
                new FunctionCallExpression(0, 0, "f", new ValueExpression[]
                {
                    new AdditionExpression(0, 2, 
                        new ConstantExpression(0, 1, new IntValue(1)), 
                        new ConstantExpression(0, 3, new IntValue(2))), 
                    new ConstantExpression(0, 3, new IntValue(2)),
                });
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;

            Assert.AreEqual(new IntValue(6), constant.Value);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_FunctionWithSomeConstantParameters()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("+",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 0 &&
                        context.Column == 3 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(1)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(3));

            var expr =
                new FunctionCallExpression(0, 0, "f", new ValueExpression[]
                {
                    new AdditionExpression(0, 3, 
                        new ConstantExpression(0, 2, new IntValue(1)), 
                        new ConstantExpression(0, 4, new IntValue(2))), 
                    new AttributeAccessExpression(0, 5, "a"), 
                });
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(FunctionCallExpression));

            var parameters = ((FunctionCallExpression) reduced).Parameters;
            Assert.AreEqual(2, parameters.Count);

            Assert.AreEqual(new IntValue(3), ((ConstantExpression) parameters[0]).Value);
            Assert.AreEqual(0, parameters[0].Line);
            Assert.AreEqual(3, parameters[0].Column);

            Assert.AreEqual("a", ((AttributeAccessExpression)parameters[1]).Name);
            Assert.AreEqual(0, parameters[1].Line);
            Assert.AreEqual(5, parameters[1].Column);
        }

        [TestMethod]
        public void Reduce_OrWithLeftConstantOperand()
        {
            var expr =
                new OrExpression(0, 1,
                    new ConstantExpression(0, 0, new IntValue(1)), 
                    new AttributeAccessExpression(0, 2, "a"));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsFalse(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_OrWithRightConstantOperand()
        {
            var expr =
                new OrExpression(0, 1,
                    new AttributeAccessExpression(0, 0, "a"),
                    new ConstantExpression(0, 2, new IntValue(1)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsFalse(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(2, constant.Column);
        }

        [TestMethod]
        public void Reduce_OrWithBothConstantOperand()
        {
            var expr =
                new OrExpression(0, 1,
                    new ConstantExpression(0, 0, new IntValue(0)),
                    new ConstantExpression(0, 2, new IntValue(1)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsFalse(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_OrWithBothOperandsNull()
        {
            var expr =
                new OrExpression(0, 1,
                    new ConstantExpression(0, 0, new IntValue(null)),
                    new ConstantExpression(0, 2, new IntValue(null)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsTrue(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }
        
        [TestMethod]
        public void Reduce_AndWithLeftNullConstantOperand()
        {
            var expr =
                new AndExpression(0, 1,
                    new ConstantExpression(0, 0, new IntValue(null)),
                    new AttributeAccessExpression(0, 2, "a"));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsTrue(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_AndWithRightConstantOperand()
        {
            var expr =
                new AndExpression(0, 1,
                    new AttributeAccessExpression(0, 0, "a"),
                    new ConstantExpression(0, 2, new IntValue(null)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsTrue(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(2, constant.Column);
        }

        [TestMethod]
        public void Reduce_AndWithBothConstantOperands()
        {
            var expr =
                new AndExpression(0, 1,
                    new ConstantExpression(0, 0, new IntValue(0)),
                    new ConstantExpression(0, 2, new IntValue(1)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsFalse(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_AndWithBothOperandsNull()
        {
            var expr =
                new AndExpression(0, 1,
                    new ConstantExpression(0, 0, new IntValue(null)),
                    new ConstantExpression(0, 2, new IntValue(null)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsTrue(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_NotWithConstantOperand()
        {
            var expr = new NotExpression(0, 0, new ConstantExpression(0, 4, new IntValue(1)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsTrue(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_UnaryMinusWithConstantOperand()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("-",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 0 &&
                        context.Column == 0 &&
                        context.Count == 1 &&
                        context[0].Equals(new IntValue(1))
                    )))
                .Returns(new IntValue(-1));

            var expr = new UnaryMinusExpression(0, 0, new ConstantExpression(0, 4, new IntValue(1)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.AreEqual(-1, ((IntValue)constant.Value).Value);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_NotWithConstantNullOperand()
        {
            var expr = new NotExpression(0, 0, new ConstantExpression(0, 4, new IntValue(null)));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(ConstantExpression));

            var constant = (ConstantExpression)reduced;
            Assert.IsFalse(constant.Value.IsNull);
            Assert.AreEqual(0, constant.Line);
            Assert.AreEqual(0, constant.Column);
        }

        [TestMethod]
        public void Reduce_NotReduceSubexpressionOfItsParameter()
        {
            _runtime
                .Setup(mock => mock.FindAndCall("+",
                    It.Is<IExecutionContext>(context =>
                        context.Line == 0 &&
                        context.Column == 1 &&
                        context.Count == 2 &&
                        context[0].Equals(new IntValue(1)) &&
                        context[1].Equals(new IntValue(2))
                    )))
                .Returns(new IntValue(3));

            var expr = new NotExpression(0, 0, 
                new AdditionExpression(0, 3, 
                    new AdditionExpression(0, 1, 
                        new ConstantExpression(0, 0, new IntValue(1)), 
                        new ConstantExpression(0, 2, new IntValue(2))), 
                    new AttributeAccessExpression(0, 4, "a")));
            var reduced = expr.Accept(_visitor);

            Assert.IsInstanceOfType(reduced, typeof(NotExpression));

            var addition = (AdditionExpression) ((NotExpression) reduced).Parameter;
            
            Assert.AreEqual(new IntValue(3), ((ConstantExpression) addition.Left).Value);
            Assert.AreEqual(0, addition.Left.Line);
            Assert.AreEqual(1, addition.Left.Column);

            Assert.AreEqual("a", ((AttributeAccessExpression)addition.Right).Name);
            Assert.AreEqual(0, addition.Right.Line);
            Assert.AreEqual(4, addition.Right.Column);
        }
    }
}
