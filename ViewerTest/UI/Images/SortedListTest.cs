using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class SortedListTest
    {
        [TestMethod]
        public void Add_ItemsInDescedingOrder()
        {
            const int count = 5000;

            var list = new SortedList<int>();
            for (var i = count; i >= 0; --i)
            {
                list.Add(i);
            }

            for (var i = 0; i < count; ++i)
            {
                Assert.AreEqual(i, list[i]);
            }
        }

        [TestMethod]
        public void Add_ItemsInAscedingOrder()
        {
            const int count = 5000;

            var list = new SortedList<int>();
            for (var i = 0; i < count; ++i)
            {
                list.Add(i);
            }

            for (var i = 0; i < count; ++i)
            {
                Assert.AreEqual(i, list[i]);
            }
        }

        [TestMethod]
        public void Merge_SortedLists()
        {
            var a = new SortedList<int>
            {
                1, 3, 5, 7
            };
            var b = new List<int>
            {
                2, 4, 6, 8, 9
            };
            var sorted = a.Merge(b);
            CollectionAssert.AreEqual(new[] { 1, 3, 5, 7 }, a.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 4, 6, 8, 9 }, b.ToArray());
            CollectionAssert.AreEqual(new[]{ 1, 2, 3, 4, 5, 6, 7, 8, 9 }, sorted.ToArray());
        }

        [TestMethod]
        public void Merge_EmptySecondList()
        {
            var a = new SortedList<int>{ 1, 2, 3, 4, 5 };
            var sorted = a.Merge(new int[]{});
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, a.ToArray());
            CollectionAssert.AreEqual(new[]{ 1, 2, 3, 4, 5 }, sorted.ToArray());
        }
    }
}
