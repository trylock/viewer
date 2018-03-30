using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.UI;

namespace ViewerTest.UI
{
    [TestClass]
    public class RectangleExtensionsTest
    {
        [TestMethod]
        public void Shrink_EmptyRectangle()
        {
            var result = new Rectangle(0, 0, 0, 0).Shrink(1);
            Assert.AreEqual(0, result.X);
            Assert.AreEqual(0, result.Y);
            Assert.AreEqual(0, result.Width);
            Assert.AreEqual(0, result.Height);
        }

        [TestMethod]
        public void Shrink_MoreThanItsWidth()
        {
            var result = new Rectangle(10, 10, 5, 15).Shrink(5);
            Assert.AreEqual(12, result.X);
            Assert.AreEqual(15, result.Y);
            Assert.AreEqual(0, result.Width);
            Assert.AreEqual(5, result.Height);
        }

        [TestMethod]
        public void Shrink_MoreThanItsHeight()
        {
            var result = new Rectangle(10, 10, 15, 5).Shrink(5);
            Assert.AreEqual(15, result.X);
            Assert.AreEqual(12, result.Y);
            Assert.AreEqual(5, result.Width);
            Assert.AreEqual(0, result.Height);
        }

        [TestMethod]
        public void Shrink_LargeRectangle()
        {
            var result = new Rectangle(10, 10, 20, 20).Shrink(2);
            Assert.AreEqual(12, result.X);
            Assert.AreEqual(12, result.Y);
            Assert.AreEqual(16, result.Width);
            Assert.AreEqual(16, result.Height);
        }
    }
}
