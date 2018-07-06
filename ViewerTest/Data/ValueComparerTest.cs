using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;

namespace ViewerTest.Data
{
    [TestClass]
    public class ValueComparerTest
    {
        private readonly ValueComparer _comparer = new ValueComparer();

        private void TestIsLess(BaseValue x, BaseValue y)
        {
            Assert.AreEqual(-1, _comparer.Compare(x, y));
            Assert.AreEqual(1, _comparer.Compare(y, x));
        }

        [TestMethod]
        public void Compare_IsReflexive()
        {
            Assert.AreEqual(0, _comparer.Compare(null, null));

            Assert.AreEqual(0, _comparer.Compare(new IntValue(null), new IntValue(null)));
            Assert.AreEqual(0, _comparer.Compare(new RealValue(null), new RealValue(null)));
            Assert.AreEqual(0, _comparer.Compare(new StringValue(null), new StringValue(null)));
            Assert.AreEqual(0, _comparer.Compare(new DateTimeValue(null), new DateTimeValue(null)));
            Assert.AreEqual(0, _comparer.Compare(new IntValue(null), new RealValue(null)));

            Assert.AreEqual(0, _comparer.Compare(new IntValue(1), new IntValue(1)));
            Assert.AreEqual(0, _comparer.Compare(new RealValue(3.14), new RealValue(3.14)));
            Assert.AreEqual(0, _comparer.Compare(new StringValue("test"), new StringValue("test")));
            Assert.AreEqual(0, _comparer.Compare(new DateTimeValue(new DateTime(2018, 1, 26)), new DateTimeValue(new DateTime(2018, 1, 26))));
            Assert.AreEqual(0, _comparer.Compare(new IntValue(1), new StringValue("1")));
        }

        [TestMethod]
        public void Compare_Numbers()
        {
            TestIsLess(new IntValue(1), new IntValue(2));
            TestIsLess(new IntValue(1), new RealValue(3.14));
            TestIsLess(new IntValue(int.MaxValue), new IntValue(null));
            TestIsLess(new RealValue(3.14), new IntValue(null));
        }

        [TestMethod]
        public void Compare_Strings()
        {
            TestIsLess(new StringValue("alpha"), new StringValue("beta"));
            TestIsLess(new DateTimeValue(new DateTime(2019, 1, 17)), new StringValue("alpha"));
            TestIsLess(
                new DateTimeValue(new DateTime(2019, 1, 16)),
                new DateTimeValue(new DateTime(2019, 1, 17)));
        }

        [TestMethod]
        public void Compare_NumbersWithStrings()
        {
            TestIsLess(new IntValue(1), new StringValue("a"));
            TestIsLess(new IntValue(int.MaxValue), new StringValue("a"));
            TestIsLess(new RealValue(3.14), new StringValue("a"));
            TestIsLess(new StringValue("a"), new IntValue(null));
            TestIsLess(new StringValue("a"), new RealValue(null));
        }

        [TestMethod]
        public void Compare_Dates()
        {
            TestIsLess(new DateTimeValue(new DateTime(2010, 8, 2)), new DateTimeValue(new DateTime(2018, 7, 2)));
        }
    }
}
