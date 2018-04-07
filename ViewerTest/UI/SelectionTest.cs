using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.UI;

namespace ViewerTest.UI
{
    [TestClass]
    public class SelectionTest
    {
        [TestMethod]
        public void Replace_NoChangedListener()
        {
            var entities = new[]
            {
                "1", "2"
            };

            var selection = new Selection();
            selection.Replace(entities);
            
            Assert.AreEqual(2, selection.Count);
            Assert.IsTrue(selection.Contains("1"));
            Assert.IsTrue(selection.Contains("2"));
        }

        [TestMethod]
        public void Replace_CallChangeListenerEveryTimeReplaceIsCalled()
        {
            var counter = 0;
            var selection = new Selection();
            selection.Changed += (sender, args) => { ++counter; };
            selection.Replace(Enumerable.Empty<string>());
            Assert.AreEqual(1, counter);
            Assert.AreEqual(0, selection.Count);
        }

        [TestMethod]
        public void Replace_NewSelection()
        {
            var oldSelection = new[]
            {
                "1", "2"
            };

            var newSelection = new[]
            {
                "3", "4"
            };

            var selection = new Selection();
            selection.Replace(oldSelection);
            selection.Replace(newSelection);

            CollectionAssert.AreEqual(newSelection, selection.ToArray());

            Assert.IsFalse(selection.Contains(oldSelection[0]));
            Assert.IsFalse(selection.Contains(oldSelection[1]));
            Assert.IsTrue(selection.Contains(newSelection[0]));
            Assert.IsTrue(selection.Contains(newSelection[1]));
        }
    }
}
