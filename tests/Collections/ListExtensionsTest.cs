using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Core.Collections;

namespace ViewerTest.Collections
{
    [TestClass]
    public class ListExtensionsTest
    {
        [TestMethod]
        public void LowerBound_EmptyList()
        {
            var list = new List<int>();
            var bound = list.LowerBound(1);
            Assert.AreEqual(0, bound);
        }

        [TestMethod]
        public void LowerBound_ValueIsBeforeTheFirstElement()
        {
            var list = new List<int> { 1, 3, 5 };
            var bound = list.LowerBound(0);
            Assert.AreEqual(0, bound);
        }

        [TestMethod]
        public void LowerBound_ValueIsTheFirstElement()
        {
            var list = new List<int> { 1, 3, 5 };
            var bound = list.LowerBound(1);
            Assert.AreEqual(0, bound);
        }

        [TestMethod]
        public void LowerBound_ValueIsAfterTheFirstElement()
        {
            var list = new List<int> { 1, 3, 5 };
            var bound = list.LowerBound(2);
            Assert.AreEqual(1, bound);
        }

        [TestMethod]
        public void LowerBound_ValueIsBeforeTheLastElement()
        {
            var list = new List<int> { 1, 3, 5 };
            var bound = list.LowerBound(4);
            Assert.AreEqual(2, bound);
        }

        [TestMethod]
        public void LowerBound_ValueIsTheLastElement()
        {
            var list = new List<int> { 1, 3, 5 };
            var bound = list.LowerBound(5);
            Assert.AreEqual(2, bound);
        }

        [TestMethod]
        public void LowerBound_ValueIsAfterTheLastElement()
        {
            var list = new List<int> { 1, 3, 5 };
            var bound = list.LowerBound(6);
            Assert.AreEqual(3, bound);
        }

        [TestMethod]
        public void LowerBound_ValueIsTheMiddleElement()
        {
            var list = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
            var bound = list.LowerBound(4);
            Assert.AreEqual(3, bound);
        }
    }
}
