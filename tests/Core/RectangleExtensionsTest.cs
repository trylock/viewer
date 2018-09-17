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
    }
}
