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
    public class FractionTest
    {
        [TestMethod]
        public void Fraction_Simplify()
        {
            var frac = new Fraction(256, 768);
            Assert.AreEqual(1, frac.Numerator);
            Assert.AreEqual(3, frac.Denominator);
        }

        [TestMethod]
        [ExpectedException(typeof(DivideByZeroException))]
        public void Fraction_ZeroDenominator()
        {
            var frac = new Fraction(0, 0);
        }

        [TestMethod]
        public void Fraction_CompareFractions()
        {
            var a = new Fraction(256, 768);
            var b = new Fraction(3, 9);
            Assert.IsTrue(a == b);
        }

        [TestMethod]
        public void Fraction_ConvertToDouble()
        {
            var frac = new Fraction(39, 52);
            Assert.AreEqual(3 / 4.0, (double) frac);
        }
    }
}
