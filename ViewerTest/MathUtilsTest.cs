using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer;

namespace ViewerTest
{
    [TestClass]
    public class MathUtilsTest
    {
        [TestMethod]
        public void RoundUpDiv_PositiveIntegers()
        {
            Assert.AreEqual(1, MathUtils.RoundUpDiv(1, 42));
            Assert.AreEqual(2, MathUtils.RoundUpDiv(43, 42));
            Assert.AreEqual(1, MathUtils.RoundUpDiv(2, 2));
            Assert.AreEqual(1, MathUtils.RoundUpDiv(42, 42));
            Assert.AreEqual(12, MathUtils.RoundUpDiv(57, 5));
            Assert.AreEqual(0, MathUtils.RoundUpDiv(0, 5));
            Assert.AreEqual(0, MathUtils.RoundUpDiv(0, 42));
        }

        [TestMethod]
        public void RoundUpDiv_NegativeNumerator()
        {
            Assert.AreEqual(0, MathUtils.RoundUpDiv(-1, 5));
            Assert.AreEqual(-1, MathUtils.RoundUpDiv(-6, 5));
            Assert.AreEqual(-2, MathUtils.RoundUpDiv(-10, 5));
            Assert.AreEqual(0, MathUtils.RoundUpDiv(-41, 42));
        }

        [TestMethod]
        public void RoundUpDiv_NegativeDenominator()
        {
            Assert.AreEqual(0, MathUtils.RoundUpDiv(1, -5));
            Assert.AreEqual(-1, MathUtils.RoundUpDiv(6, -5));
            Assert.AreEqual(-2, MathUtils.RoundUpDiv(10, -5));
            Assert.AreEqual(0, MathUtils.RoundUpDiv(41, -42));
        }

        [TestMethod]
        public void RoundUpDiv_NegativeIntegers()
        {
            Assert.AreEqual(1, MathUtils.RoundUpDiv(-1, -42));
            Assert.AreEqual(2, MathUtils.RoundUpDiv(-43, -42));
            Assert.AreEqual(1, MathUtils.RoundUpDiv(-2, -2));
            Assert.AreEqual(1, MathUtils.RoundUpDiv(-42, -42));
            Assert.AreEqual(12, MathUtils.RoundUpDiv(-57, -5));
            Assert.AreEqual(0, MathUtils.RoundUpDiv(0, -5));
            Assert.AreEqual(0, MathUtils.RoundUpDiv(0, -42));
        }
    }
}
