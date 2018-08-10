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
            selection.Replace(new []{ new FileEntity("test1"), new FileEntity("test2") });
            
            Assert.AreEqual(2, selection.Count);
        }

        [TestMethod]
        public void Replace_CallChangeListenerEveryTimeReplaceIsCalled()
        {
            var counter = 0;
            var selection = new Selection();
            selection.Changed += (sender, args) => { ++counter; };
            selection.Replace(Enumerable.Empty<IEntity>());
            Assert.AreEqual(1, counter);
            Assert.AreEqual(0, selection.Count);
        }

        [TestMethod]
        public void Replace_NewSelection()
        {
            var oldSelection = new[]
            {
                new FileEntity("test0"),
                new FileEntity("test1"),
            };

            var newSelection = new[]
            {
                new FileEntity("test2"),
                new FileEntity("test3"),
            };

            var selection = new Selection();
            selection.Replace(oldSelection);
            selection.Replace(newSelection);

            CollectionAssert.AreEqual(newSelection, selection.ToArray());
        }
    }
}
