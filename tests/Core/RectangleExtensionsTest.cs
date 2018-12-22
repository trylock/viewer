using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Core;

namespace ViewerTest.Core
{
    [TestClass]
    public class RectangleExtensionsTest
    {
        [TestMethod]
        public void EnsureInside_ContainerAndRectangleAreEmpty()
        {
            var result = Rectangle.Empty.EnsureInside(Rectangle.Empty);
            Assert.AreEqual(Rectangle.Empty, result);
        }

        [TestMethod]
        public void EnsureInside_ContainerIsEmpty()
        {
            var result = new Rectangle(0, 0, 10, 10).EnsureInside(Rectangle.Empty);
            Assert.AreEqual(Rectangle.Empty, result);
        }

        [TestMethod]
        public void EnsureInside_RectangleIsInTheMiddleOfContainer()
        {
            var result = new Rectangle(10, 10, 10, 10).EnsureInside(new Rectangle(0, 0, 30, 30));
            Assert.AreEqual(new Rectangle(10, 10, 10, 10), result);
        }

        [TestMethod]
        public void EnsureInside_RectangleOverflowsInTheXAxis()
        {
            var result = new Rectangle(10, 10, 10, 10).EnsureInside(new Rectangle(0, 0, 19, 30));
            Assert.AreEqual(new Rectangle(9, 10, 10, 10), result);
        }

        [TestMethod]
        public void EnsureInside_RectangleOverflowsInTheYAxis()
        {
            var result = new Rectangle(10, 10, 10, 10).EnsureInside(new Rectangle(0, 0, 30, 19));
            Assert.AreEqual(new Rectangle(10, 9, 10, 10), result);
        }

        [TestMethod]
        public void EnsureInside_RectangleOverflowsInTheXAndTheYAxis()
        {
            var result = new Rectangle(10, 10, 10, 10).EnsureInside(new Rectangle(0, 0, 19, 19));
            Assert.AreEqual(new Rectangle(9, 9, 10, 10), result);
        }

        [TestMethod]
        public void Except_EmptyRectangle()
        {
            var input = new Rectangle(10, 5, 20, 30);
            var subtracted = Rectangle.Empty;
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]{ input }, result);
        }

        [TestMethod]
        public void Except_EmptyRectangleInside()
        {
            var input = new Rectangle(10, 5, 20, 30);
            var subtracted = new Rectangle(15, 10, 0, 0);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[] { input }, result);
        }

        [TestMethod]
        public void Except_SingleLeftRemainder()
        {
            var input = new Rectangle(0, 0, 20, 30);
            var subtracted = new Rectangle(5, -5, 50, 50);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                new Rectangle(0, 0, 5, 30), 
            }, result);
        }

        [TestMethod]
        public void Except_SingleRightRemainder()
        {
            var input = new Rectangle(0, 0, 20, 30);
            var subtracted = new Rectangle(-5, -5, 15, 50);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                new Rectangle(10, 0, 10, 30),
            }, result);
        }

        [TestMethod]
        public void Except_SingleBottomRemainder()
        {
            var input = new Rectangle(0, 0, 20, 30);
            var subtracted = new Rectangle(-5, -5, 50, 25);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                new Rectangle(0, 20, 20, 10),
            }, result);
        }

        [TestMethod]
        public void Except_SingleTopRemainder()
        {
            var input = new Rectangle(0, 0, 20, 30);
            var subtracted = new Rectangle(-5, 10, 50, 50);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                new Rectangle(0, 0, 20, 10),
            }, result);
        }

        [TestMethod]
        public void Except_TopLeftCorner()
        {
            var input = new Rectangle(0, 0, 20, 30);
            var subtracted = new Rectangle(-5, -5, 15, 15);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                new Rectangle(10, 0, 10, 30),
                new Rectangle(0, 10, 10, 20),
            }, result);
        }

        [TestMethod]
        public void Except_TopRightCorner()
        {
            var input = new Rectangle(0, 0, 20, 30);
            var subtracted = new Rectangle(10, -5, 15, 15);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                new Rectangle(0, 0, 10, 30),
                new Rectangle(10, 10, 10, 20),
            }, result);
        }

        [TestMethod]
        public void Except_BottomRightCorner()
        {
            var input = new Rectangle(0, 0, 20, 30);
            var subtracted = new Rectangle(10, 20, 15, 15);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                new Rectangle(0, 0, 10, 30),
                new Rectangle(10, 0, 10, 20),
            }, result);
        }

        [TestMethod]
        public void Except_BottomLeftCorner()
        {
            var input = new Rectangle(0, 0, 20, 30);
            var subtracted = new Rectangle(-5, 20, 15, 15);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                new Rectangle(10, 0, 10, 30),
                new Rectangle(0, 0, 10, 20),
            }, result);
        }

        [TestMethod]
        public void Except_Hole()
        {
            var input = new Rectangle(0, 0, 20, 30);
            var subtracted = new Rectangle(5, 10, 10, 10);
            var result = input.Except(subtracted).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                new Rectangle(0, 0, 5, 30),
                new Rectangle(15, 0, 5, 30),
                new Rectangle(5, 0, 10, 10),
                new Rectangle(5, 20, 10, 10),
            }, result);
        }
    }
}
