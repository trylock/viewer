using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.UI.Images;
using Viewer.UI.Images.Layout;

namespace ViewerTest.UI.Images.Layout
{
    [TestClass]
    public class VerticalGridLayoutTest
    {
        private VerticalGridLayout _layout;

        [TestInitialize]
        public void Setup()
        {
            _layout = new VerticalGridLayout
            {
                GroupLabelSize = new Size(0, 25),
                GroupLabelMargin = new Padding(5),
                ItemMargin = new Padding(5, 10, 5, 10),
                ItemPadding = new Padding(5),
                ThumbnailAreaSize = new Size(200, 120),
            };
            _layout.Resize(new Size(800, 0));

            var smallGroup = new Group(new IntValue(0));
            var emptyGroup = new Group(new IntValue(1));
            var largeCollapsedGroup = new Group(new IntValue(2))
            {
                IsCollapsed = true
            };
            var largeGroup = new Group(new IntValue(3));
            
            for (var i = 0; i < 4; ++i)
            {
                smallGroup.Items.Add(new EntityView(null, null));
            }

            for (var i = 0; i < 130; ++i)
            {
                largeCollapsedGroup.Items.Add(new EntityView(null, null));
            }

            for (var i = 0; i < 20; ++i)
            {
                largeGroup.Items.Add(new EntityView(null, null));
            }

            _layout.Groups = new SortedDictionary<BaseValue, Group>
            {
                { smallGroup.Key, smallGroup },
                { emptyGroup.Key, emptyGroup },
                { largeCollapsedGroup.Key, largeCollapsedGroup },
                { largeGroup.Key, largeGroup },
            };
        }

        [TestMethod]
        public void GetItemAt_OutsideOfGridArea()
        {
            var item = _layout.GetItemAt(new Point(-1, 0));
            Assert.IsNull(item);

            item = _layout.GetItemAt(new Point(0, -1));
            Assert.IsNull(item);

            item = _layout.GetItemAt(new Point(-1, -1));
            Assert.IsNull(item);

            item = _layout.GetItemAt(new Point(_layout.ClientSize.Width + 1, 0));
            Assert.IsNull(item);
        }
        
        [TestMethod]
        public void GetItemAt_GroupLabel()
        {
            var item = _layout.GetItemAt(new Point(10, 10));
            Assert.IsNull(item);

            item = _layout.GetItemAt(new Point(10, 325));
            Assert.IsNull(item);
        }

        [TestMethod]
        public void GetItemAt_Between2Items()
        {
            var item = _layout.GetItemAt(new Point(215, 175));
            Assert.IsNull(item);
        }

        [TestMethod]
        public void GetItemAt_FirstGroupFirstItem()
        {
            var item = _layout.GetItemAt(new Point(130, 145));
            Assert.IsNotNull(item);

            var expectedItem = _layout.Groups[new IntValue(0)].Items[0];
            Assert.AreEqual(expectedItem, item);
        }

        [TestMethod]
        public void GetItemAt_FirstGroupLastItem()
        {
            var item = _layout.GetItemAt(new Point(130, 270));
            Assert.IsNotNull(item);

            var expectedItem = _layout.Groups[new IntValue(0)].Items[3];
            Assert.AreEqual(expectedItem, item);
        }

        [TestMethod]
        public void GetItemAt_CollapsedGroup()
        {
            var item = _layout.GetItemAt(new Point(40, 395));
            Assert.IsNull(item);
        }

        [TestMethod]
        public void GetItemAt_CellSizeStretchesInWidth()
        {
            var item = _layout.GetItemAt(new Point(790, 70));
            Assert.IsNotNull(item);

            var expectedItem = _layout.Groups[new IntValue(0)].Items[2];
            Assert.AreEqual(expectedItem, item);
        }

        [TestMethod]
        public void GetItemAt_EmptySpaceInTheLastRow()
        {
            var item = _layout.GetItemAt(new Point(300, 190));
            Assert.IsNull(item);
        }

        [TestMethod]
        public void GetGroupsIn_EmptyBounds()
        {
            var groups = _layout.GetGroupsIn(Rectangle.Empty).ToList();

            Assert.AreEqual(0, groups.Count);
        }

        [TestMethod]
        public void GetGroupsIn_BoundsInGroupLabel()
        {
            var groups = _layout
                .GetGroupsIn(new Rectangle(0, 0, 1, 1))
                .Select(element => element.Item)
                .ToArray();

            var expectedGroup = _layout.Groups[new IntValue(0)];
            CollectionAssert.AreEqual(new[]{ expectedGroup }, groups);
        }

        [TestMethod]
        public void GetGroupsIn_BoundsInGridArea()
        {
            var groups = _layout
                .GetGroupsIn(new Rectangle(50, 50, 100, 200))
                .Select(element => element.Item)
                .ToArray();

            var expectedGroup = _layout.Groups[new IntValue(0)];
            CollectionAssert.AreEqual(new[] { expectedGroup }, groups);
        }

        [TestMethod]
        public void GetGroupsIn_MultipleGroups()
        {
            var groups = _layout
                .GetGroupsIn(new Rectangle(50, 315, 100, 60))
                .Select(element => element.Item)
                .ToArray();

            var expectedGroups = new[]
            {
                _layout.Groups[new IntValue(1)],
                _layout.Groups[new IntValue(2)],
            };
            CollectionAssert.AreEqual(expectedGroups, groups);
        }

        [TestMethod]
        public void GetItemsIn_EmptyBounds()
        {
            var items = _layout.GetGroupsIn(Rectangle.Empty).ToList();

            Assert.AreEqual(0, items.Count);
        }
        
        [TestMethod]
        public void GetItemsIn_ItemsInGroupLabel()
        {
            var items = _layout.GetItemsIn(new Rectangle(10, 20, 100, 14)).ToList();

            Assert.AreEqual(0, items.Count);
        }

        [TestMethod]
        public void GetItemsIn_AllItemsInGroup()
        {
            var items = _layout
                .GetItemsIn(new Rectangle(10, 35, 760, 250))
                .Select(element => element.Item)
                .ToArray();

            var expectedItems = _layout.Groups[new IntValue(0)].Items.ToArray();
            CollectionAssert.AreEqual(expectedItems, items);
        }

        [TestMethod]
        public void GetItemBounds_NullItem()
        {
            var bounds = _layout.GetItemBounds(null);
            Assert.IsTrue(bounds.IsEmpty);
        }

        [TestMethod]
        public void GetItemBounds_NonExistentItem()
        {
            var bounds = _layout.GetItemBounds(new EntityView(null, null));
            Assert.IsTrue(bounds.IsEmpty);
        }

        [TestMethod]
        public void GetItemBounds_ItemInCollapsedGroup()
        {
            var item = _layout.Groups[new IntValue(2)].Items[2];
            var bounds = _layout.GetItemBounds(item);
            Assert.IsTrue(bounds.IsEmpty);
        }
        
        [TestMethod]
        public void GetItemBounds_VisibleItem()
        {
            var item = _layout.Groups[new IntValue(3)].Items[4];
            var bounds = _layout.GetItemBounds(item);
            Assert.AreEqual(270, bounds.X);
            Assert.AreEqual(570, bounds.Y);
            Assert.AreEqual(210, bounds.Width);
            Assert.AreEqual(130, bounds.Height);
        }
    }
}
