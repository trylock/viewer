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
                new Entity("1", DateTime.Now, DateTime.Now),
                new Entity("2", DateTime.Now, DateTime.Now),
            };

            var selection = new Selection();
            selection.Replace(entities);
            
            Assert.AreEqual(2, selection.Count);
            Assert.IsTrue(selection.Contains(entities[0]));
            Assert.IsTrue(selection.Contains(entities[1]));
        }

        [TestMethod]
        public void Replace_CallChangeListenerEveryTimeReplaceIsCalled()
        {
            var counter = 0;
            var selection = new Selection();
            selection.Changed += (sender, args) => { ++counter; };
            selection.Replace(Enumerable.Empty<Entity>());
            Assert.AreEqual(1, counter);
            Assert.AreEqual(0, selection.Count);
        }

        [TestMethod]
        public void Replace_NewSelection()
        {
            var oldSelection = new[]
            {
                new Entity("1", DateTime.Now, DateTime.Now),
                new Entity("2", DateTime.Now, DateTime.Now),
            };

            var newSelection = new[]
            {
                new Entity("3", DateTime.Now, DateTime.Now),
                new Entity("4", DateTime.Now, DateTime.Now),
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
