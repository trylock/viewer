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

namespace ViewerTest.UI.Images
{
    public class NullThumbnailGeneratorMock : IThumbnailGenerator
    {
        public Image GetThumbnail(Image original, Size thumbnailArea)
        {
            return null;
        }
    }

    [TestClass]
    public class ImagesPresenterTest
    {
        private ImagesViewMock _viewMock;
        private ClipboardServiceMock _clipboardMock;
        private ImagesPresenter _presenter;

        [TestInitialize]
        public void Setup()
        {
            _viewMock = new ImagesViewMock(8, 8);
            _clipboardMock = new ClipboardServiceMock();
            var storage = new MemoryAttributeStorage();
            var thumbnailGenerator = new NullThumbnailGeneratorMock();
            _presenter = new ImagesPresenter(_viewMock, null, storage, _clipboardMock, thumbnailGenerator);
            _presenter.AddItemsInternal(Enumerable.Repeat<AttributeCollection>(null, 16));
        }

        [TestMethod]
        public void Selection_EndPointIsToTheRight()
        {
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            _viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));
            Assert.AreEqual(1, _viewMock.CurrentSelection.X);
            Assert.AreEqual(1, _viewMock.CurrentSelection.Y);
            Assert.AreEqual(3, _viewMock.CurrentSelection.Width);
            Assert.AreEqual(1, _viewMock.CurrentSelection.Height);

            _viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));
            Assert.IsTrue(_viewMock.CurrentSelection.IsEmpty);
            
            CollectionAssert.AreEqual(new[]{ 5, 6 }, _presenter.Selection.OrderBy(x => x).ToArray());

            // make sure we have updated just the selection items
            var index = 0;
            foreach (var item in _viewMock.Items)
            {
                if (index == 5 || index == 6)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else
                {
                    Assert.IsFalse(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_EndPointIsToTheLeft()
        {
            Assert.AreEqual(16, _viewMock.Items.Count);

            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 5, 3, 0));
            _viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.Left, 0, 2, 2, 0));
            Assert.AreEqual(2, _viewMock.CurrentSelection.X);
            Assert.AreEqual(2, _viewMock.CurrentSelection.Y);
            Assert.AreEqual(3, _viewMock.CurrentSelection.Width);
            Assert.AreEqual(1, _viewMock.CurrentSelection.Height);

            _viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 2, 2, 0));
            Assert.IsTrue(_viewMock.CurrentSelection.IsEmpty);

            CollectionAssert.AreEqual(new[] { 5, 6 }, _presenter.Selection.OrderBy(x => x).ToArray());

            // make sure we have updated just the selection items
            var index = 0;
            foreach (var item in _viewMock.Items)
            {
                if (index == 5 || index == 6)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else
                {
                    Assert.IsFalse(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_UnionWithPreviousSelection()
        {
            // first selection
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            _viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 2, 2, 0));

            // second selection (union)
            _viewMock.TriggerKeyDown(new KeyEventArgs(Keys.Shift));
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 5, 0, 0));
            _viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 6, 0, 0));

            CollectionAssert.AreEqual(new[] { 3, 5 }, _presenter.Selection.OrderBy(x => x).ToArray());

            var index = 0;
            foreach (var item in _viewMock.Items)
            {
                if (index == 5 || index == 3)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else
                {
                    Assert.IsFalse(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_SymetricDifferenceWithPreviousSelection()
        {
            // first selection
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            _viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 2, 2, 0));

            // second selection (symetric difference)
            _viewMock.TriggerKeyDown(new KeyEventArgs(Keys.Control));
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 5, 2, 0));
            _viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 0, 2, 0));

            CollectionAssert.AreEqual(new[] { 4, 6 }, _presenter.Selection.OrderBy(x => x).ToArray());

            var index = 0;
            foreach (var item in _viewMock.Items)
            {
                if (index == 4 || index == 6)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else if (index == 5)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                else
                {
                    Assert.IsFalse(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_Reset()
        {
            // first selection
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            _viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));

            // reset the selection
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0));
            _viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));

            CollectionAssert.AreEqual(new[] { 0 }, _presenter.Selection.ToArray());

            var index = 0;
            foreach (var item in _viewMock.Items)
            {
                if (index == 0)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else if (index == 5 || index == 6)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                else
                {
                    Assert.IsFalse(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_SelectAllWithCtrlA()
        {
            _viewMock.TriggerKeyDown(new KeyEventArgs(Keys.A | Keys.Control));

            CollectionAssert.AreEqual(
                Enumerable.Range(0, 16).ToArray(), 
                _presenter.Selection.OrderBy(x => x).ToArray());

            var index = 0;
            foreach (var item in _viewMock.Items)
            {
                Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                Assert.AreEqual(ResultItemState.Selected, item.State);
                ++index;
            }
        }

        [TestMethod]
        public void ActiveItem_OnlyOneItemCanBeActive()
        {
            // move with mouse between items
            _viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.None, 0, 1, 1, 0));
            Assert.AreEqual(-1, _presenter.ActiveItem);

            var index = 0;
            foreach (var item in _viewMock.Items)
            {
                Assert.IsFalse(_viewMock.UpdatedItems.Contains(index));
                Assert.AreEqual(ResultItemState.None, item.State);
                ++index;
            }
            _viewMock.ResetMock();

            // select an item
            _viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.None, 0, 2, 2, 0));
            Assert.AreEqual(5, _presenter.ActiveItem);
            index = 0;
            foreach (var item in _viewMock.Items)
            {
                if (index == 5)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.Active, item.State);
                }
                else
                {
                    Assert.IsFalse(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
            _viewMock.ResetMock();

            // change an item
            _viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.None, 0, 4, 2, 0));
            Assert.AreEqual(6, _presenter.ActiveItem);
            index = 0;
            foreach (var item in _viewMock.Items)
            {
                if (index == 5)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                else if (index == 6)
                {
                    Assert.IsTrue(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.Active, item.State);
                }
                else
                {
                    Assert.IsFalse(_viewMock.UpdatedItems.Contains(index));
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void NameEditForm_HideWithEscape()
        {
            // make sure we have an item selected
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Right, 0, 2, 2, 0));

            // begin editing its name
            Assert.AreEqual(-1, _viewMock.EditIndex);
            _viewMock.TriggerBeginEditItemName();
            Assert.AreEqual(5, _viewMock.EditIndex);

            // cancel edit with escape
            _viewMock.TriggerKeyDown(new KeyEventArgs(Keys.Escape));
            Assert.AreEqual(-1, _viewMock.EditIndex);
        }

        [TestMethod]
        public void NameEditForm_HideWithLostOfFocus()
        {
            // make sure we have an item selected
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Right, 0, 2, 2, 0));

            // begin editing its name
            Assert.AreEqual(-1, _viewMock.EditIndex);
            _viewMock.TriggerBeginEditItemName();
            Assert.AreEqual(5, _viewMock.EditIndex);

            // cancel edit with clicking at other items
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Right, 0, 1, 1, 0));
        }

        [TestMethod]
        public void NameEditForm_NoItem()
        {
            Assert.AreEqual(-1, _presenter.FocusedItem);
            _viewMock.TriggerBeginEditItemName();
            Assert.AreEqual(-1, _presenter.FocusedItem);
        }

        [TestMethod]
        public void NameEditForm_UserCanCancelTheEdit()
        {
            // make sure we have an item selected
            _viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Right, 0, 2, 2, 0));

            // begin editing its name
            Assert.AreEqual(-1, _viewMock.EditIndex);
            _viewMock.TriggerBeginEditItemName();
            Assert.AreEqual(5, _viewMock.EditIndex);

            _viewMock.TriggerCancelEditItemName();
            Assert.AreEqual(-1, _viewMock.EditIndex);
        }
    }
}
