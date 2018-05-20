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
    }
}
