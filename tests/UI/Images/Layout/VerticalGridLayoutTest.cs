﻿using System;
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
                ClientBounds = new Rectangle(0, 0, 800, 600),
            };

            var smallGroup = new Group(new IntValue(0));
            var emptyGroup = new Group(new IntValue(1));
            var largeCollapsedGroup = new Group(new IntValue(2));
            largeCollapsedGroup.View.IsCollapsed = true;
            var largeGroup = new Group(new IntValue(3));
            
            for (var i = 0; i < 4; ++i)
            {
                smallGroup.Items.Add(new EntityView(new FileEntity("small_group_test" + i), null));
            }

            for (var i = 0; i < 130; ++i)
            {
                largeCollapsedGroup.Items.Add(new EntityView(new FileEntity("collapsed_group_test" + i), null));
            }

            for (var i = 0; i < 21; ++i)
            {
                largeGroup.Items.Add(new EntityView(new FileEntity("large_group_test" + i), null));
            }

            _layout.Groups = new List<Group>
            {
                smallGroup,
                emptyGroup,
                largeCollapsedGroup,
                largeGroup,
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

            item = _layout.GetItemAt(new Point(_layout.ClientBounds.Width + 1, 0));
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

            var expectedItem = _layout.Groups[0].Items[0];
            Assert.AreEqual(expectedItem, item);
        }

        [TestMethod]
        public void GetItemAt_FirstGroupLastItem()
        {
            var item = _layout.GetItemAt(new Point(130, 270));
            Assert.IsNotNull(item);

            var expectedItem = _layout.Groups[0].Items[3];
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

            var expectedItem = _layout.Groups[0].Items[2];
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

            var expectedGroup = _layout.Groups[0];
            CollectionAssert.AreEqual(new[]{ expectedGroup }, groups);
        }

        [TestMethod]
        public void GetGroupsIn_BoundsInGridArea()
        {
            var groups = _layout
                .GetGroupsIn(new Rectangle(50, 50, 100, 200))
                .Select(element => element.Item)
                .ToArray();

            var expectedGroup = _layout.Groups[0];
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
                _layout.Groups[1],
                _layout.Groups[2],
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
        public void GetItemsIn_LastRowIsFull()
        {
            var items = _layout.GetItemsIn(new Rectangle(0, 1340, 800, 1))
                .Select(element => element.Item)
                .ToArray();

            var expectedItems = _layout.Groups[3].Items.Skip(18).ToArray();
            CollectionAssert.AreEqual(expectedItems, items);
        }

        [TestMethod]
        public void GetItemsIn_AllItemsInGroup()
        {
            var items = _layout
                .GetItemsIn(new Rectangle(10, 35, 760, 250))
                .Select(element => element.Item)
                .ToArray();

            var expectedItems = _layout.Groups[0].Items.ToArray();
            CollectionAssert.AreEqual(expectedItems, items);
        }
        
        [TestMethod]
        public void GetItemsIn_TheLastRowIfItIsNotFull()
        {
            var items = _layout
                .GetItemsIn(new Rectangle(290, 190, 10, 10))
                .Select(element => element.Item)
                .ToArray();
            
            Assert.AreEqual(0, items.Length);
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
            var item = _layout.Groups[2].Items[2];
            var bounds = _layout.GetItemBounds(item);
            Assert.IsTrue(bounds.IsEmpty);
        }
        
        [TestMethod]
        public void GetItemBounds_VisibleItem()
        {
            var item = _layout.Groups[3].Items[4];
            var bounds = _layout.GetItemBounds(item);
            Assert.AreEqual(270, bounds.X);
            Assert.AreEqual(570, bounds.Y);
            Assert.AreEqual(260, bounds.Width);
            Assert.AreEqual(130, bounds.Height);
        }

        [TestMethod]
        public void GetGroupLabelsIn_ReturnPartiallyVisibleLabels()
        {
            var elements = _layout.GetGroupLabelsIn(new Rectangle(new Point(10, 10), new Size(100, 100)));
            var groups = elements.Select(element => element.Item).ToArray();
            CollectionAssert.AreEqual(new[]{ _layout.Groups[0] }, groups);
        }

        [TestMethod]
        public void AlignLocation_PointAboveTheGrid()
        {
            var location = _layout.AlignLocation(new Point(10, -100), false);
            Assert.AreEqual(new Point(10, 0), location);

            location = _layout.AlignLocation(new Point(10, -100), true);
            Assert.AreEqual(new Point(10, 165), location);
        }

        [TestMethod]
        public void AlignLocation_PointInGridLabel()
        {
            var location = _layout.AlignLocation(new Point(10, 10), false);
            Assert.AreEqual(new Point(10, 0), location);

            location = _layout.AlignLocation(new Point(10, 10), true);
            Assert.AreEqual(new Point(10, 165), location);
        }

        [TestMethod]
        public void AlignLocation_PointInTheFirstRow()
        {
            var location = _layout.AlignLocation(new Point(15, 40), false);
            Assert.AreEqual(new Point(15, 35), location);

            location = _layout.AlignLocation(new Point(15, 40), true);
            Assert.AreEqual(new Point(15, 165), location);
        }

        [TestMethod]
        public void FindItem_ToTheLeftOfTheFirstItem()
        {
            var element = _layout.FindItem(_layout.Groups[0].Items[0], new Point(-1, 0));
            Assert.IsNull(element);
        }

        [TestMethod]
        public void FindItem_ToTheRightOfTheFirstItem()
        {
            var element = _layout.FindItem(_layout.Groups[0].Items[0], new Point(1, 0));
            Assert.AreEqual(new Rectangle(270, 35, 260, 130), element.Bounds);
            Assert.AreEqual(_layout.Groups[0].Items[1], element.Item);
        }

        [TestMethod]
        public void FindItem_AboveTheFirstItem()
        {
            var element = _layout.FindItem(_layout.Groups[0].Items[0], new Point(0, -1));
            Assert.IsNull(element);
        }

        [TestMethod]
        public void FindItem_BelowTheFirstItem()
        {
            var element = _layout.FindItem(_layout.Groups[0].Items[0], new Point(0, 1));
            Assert.AreEqual(new Rectangle(0, 185, 260, 130), element.Bounds);
            Assert.AreEqual(_layout.Groups[0].Items[3], element.Item);
        }

        [TestMethod]
        public void FindItem_BelowTheGroup()
        {
            var element = _layout.FindItem(_layout.Groups[0].Items[3], new Point(0, 1));
            Assert.AreEqual(new Rectangle(0, 420, 260, 130), element.Bounds);
            Assert.AreEqual(_layout.Groups[3].Items[0], element.Item);
        }

        [TestMethod]
        public void FindItem_AboveTheFirstItemInAGroup()
        {
            var element = _layout.FindItem(_layout.Groups[3].Items[2], new Point(0, -1));
            Assert.AreEqual(new Rectangle(0, 185, 260, 130), element.Bounds);
            Assert.AreEqual(_layout.Groups[0].Items[3], element.Item);
        }

        [TestMethod]
        public void FindItem_BelowTheItemWhichIsNotInTheLastRow()
        {
            var element = _layout.FindItem(_layout.Groups[0].Items[2], new Point(0, 1));
            Assert.AreEqual(new Rectangle(0, 185, 260, 130), element.Bounds);
            Assert.AreEqual(_layout.Groups[0].Items[3], element.Item);
        }

        [TestMethod]
        public void AreSameQueries_SameQueriesOneStartsInAGap()
        {
            var result = _layout.AreSameQueries(
                Rectangle.FromLTRB(265, 40, 530, 80),
                Rectangle.FromLTRB(275, 40, 530, 80));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AreSameQueries_SameQueriesOneEndsInAGap()
        {
            var result = _layout.AreSameQueries(
                Rectangle.FromLTRB(265, 40, 541, 80),
                Rectangle.FromLTRB(265, 40, 555, 80));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AreSameQueries_TheFirstColumnIsDifferent()
        {
            var result = _layout.AreSameQueries(
                Rectangle.FromLTRB(260, 40, 530, 80),
                Rectangle.FromLTRB(275, 40, 530, 80));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AreSameQueries_TheLastColumnIsDifferent()
        {
            var result = _layout.AreSameQueries(
                Rectangle.FromLTRB(275, 40, 540, 80),
                Rectangle.FromLTRB(275, 40, 541, 80));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AreSameQueries_SameQueriesOneEndsInGapBetweenRows()
        {
            var result = _layout.AreSameQueries(
                new Rectangle(100, 10, 50, 50),
                new Rectangle(100, 10, 50, 160));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AreSameQueries_QueriesDifferInBottomRow()
        {
            var result = _layout.AreSameQueries(
                new Rectangle(100, 10, 50, 174),
                new Rectangle(100, 10, 50, 175));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AreSameQueries_QueriesDifferInTopRow()
        {
            var result = _layout.AreSameQueries(
                Rectangle.FromLTRB(10, 185, 10, 200),
                Rectangle.FromLTRB(10, 165, 10, 200));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AreSameQueries_SameQueriesOneIsInRowGap()
        {
            var result = _layout.AreSameQueries(
                Rectangle.FromLTRB(10, 185, 10, 200),
                Rectangle.FromLTRB(10, 166, 10, 200));
            Assert.IsTrue(result);
        }
    }
}
