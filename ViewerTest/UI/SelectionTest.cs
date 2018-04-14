using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.UI;
using ViewerTest.Data;

namespace ViewerTest.UI
{
    [TestClass]
    public class SelectionTest
    {
        [TestMethod]
        public void Replace_NoChangedListener()
        {
            var selection = new Selection();
            var entities = new EntityManagerMock(new Entity("test1"), new Entity("test2"));
            selection.Replace(entities, new []{ 0, 1 });
            
            Assert.AreEqual(2, selection.Count);
            Assert.IsTrue(selection.Contains(0));
            Assert.IsTrue(selection.Contains(1));
        }

        [TestMethod]
        public void Replace_CallChangeListenerEveryTimeReplaceIsCalled()
        {
            var counter = 0;
            var selection = new Selection();
            var entities = new EntityManagerMock();
            selection.Changed += (sender, args) => { ++counter; };
            selection.Replace(entities, Enumerable.Empty<int>());
            Assert.AreEqual(1, counter);
            Assert.AreEqual(0, selection.Count);
        }

        [TestMethod]
        public void Replace_NewSelection()
        {
            var oldSelection = new[]
            {
                0, 1
            };

            var newSelection = new[]
            {
                2, 3
            };

            var selection = new Selection();
            var entities = new EntityManagerMock(new Entity("test"), new Entity("test2"), new Entity("test3"), new Entity("test4"));
            selection.Replace(entities, oldSelection);
            selection.Replace(entities, newSelection);

            CollectionAssert.AreEqual(newSelection, selection.ToArray());

            Assert.IsFalse(selection.Contains(oldSelection[0]));
            Assert.IsFalse(selection.Contains(oldSelection[1]));
            Assert.IsTrue(selection.Contains(newSelection[0]));
            Assert.IsTrue(selection.Contains(newSelection[1]));
        }
    }
}
