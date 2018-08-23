using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.UI;
using ViewerTest.Data;

namespace ViewerTest.UI
{
    [TestClass]
    public class SelectionTest
    {
        private Selection _selection;

        [TestInitialize]
        public void Setup()
        {
            _selection = new Selection();
        }

        [TestMethod]
        public void Replace_NoChangedListener()
        {
            _selection.Replace(new []{ new FileEntity("test1"), new FileEntity("test2") });
            
            Assert.AreEqual(2, _selection.Count());
        }

        [TestMethod]
        public void Replace_CallChangeListenerEveryTimeReplaceIsCalled()
        {
            var counter = 0;
            _selection.Changed += (sender, args) => { ++counter; };
            _selection.Replace(Enumerable.Empty<IEntity>());
            Assert.AreEqual(1, counter);
            Assert.AreEqual(0, _selection.Count());
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

            _selection.Replace(oldSelection);
            _selection.Replace(newSelection);

            CollectionAssert.AreEqual(newSelection, _selection.ToArray());
        }
    }
}
