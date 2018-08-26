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
    public class EnumerableExtensionsTest
    {
        [TestMethod]
        public void Merge_EmptyLists()
        {
            var first = new int[] {};
            var second = new int[] { };
            var merged = first.Merge(second, Comparer<int>.Default);
            Assert.AreNotEqual(first, merged);
            Assert.AreNotEqual(second, merged);
            Assert.AreEqual(0, merged.Count);
        }

        [TestMethod]
        public void Merge_FirstListIsEmpty()
        {
            var first = new int[] { };
            var second = new int[] { 1, 2, 3 };
            var merged = first.Merge(second, Comparer<int>.Default);
            Assert.AreNotEqual(first, merged);
            Assert.AreNotEqual(second, merged);
            CollectionAssert.AreEqual(new[]{ 1, 2, 3 }, merged);
        }

        [TestMethod]
        public void Merge_SecondListIsEmpty()
        {
            var first = new int[] { 1, 4, 8 };
            var second = new int[] { };
            var merged = first.Merge(second, Comparer<int>.Default);
            Assert.AreNotEqual(first, merged);
            Assert.AreNotEqual(second, merged);
            CollectionAssert.AreEqual(new[] { 1, 4, 8 }, merged);
        }

        [TestMethod]
        public void Merge_SameNumberOfElements()
        {
            var first = new int[] { 1, 4, 8 };
            var second = new int[] { 2, 5, 9 };
            var merged = first.Merge(second, Comparer<int>.Default);
            Assert.AreNotEqual(first, merged);
            Assert.AreNotEqual(second, merged);
            CollectionAssert.AreEqual(new[] { 1, 2, 4, 5, 8, 9 }, merged);
        }

        [TestMethod]
        public void Merge_DifferentNumberOfElements()
        {
            var first = new int[] { 1, 2 };
            var second = new int[] { 2, 5, 9 };
            var merged = first.Merge(second, Comparer<int>.Default);
            Assert.AreNotEqual(first, merged);
            Assert.AreNotEqual(second, merged);
            CollectionAssert.AreEqual(new[] { 1, 2, 2, 5, 9 }, merged);
        }
    }
}
