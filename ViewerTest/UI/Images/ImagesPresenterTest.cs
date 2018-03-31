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
        [TestMethod]
        public void Selection_EndPointIsToTheRight()
        {
            var viewMock = new ImagesViewMock(8, 8);
            var storage = new MemoryAttributeStorage();
            var thumbnailGenerator = new NullThumbnailGeneratorMock();
            var presenter = new ImagesPresenter(viewMock, storage, thumbnailGenerator);
            
            viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));
            Assert.AreEqual(1, viewMock.CurrentSelection.X);
            Assert.AreEqual(1, viewMock.CurrentSelection.Y);
            Assert.AreEqual(3, viewMock.CurrentSelection.Width);
            Assert.AreEqual(1, viewMock.CurrentSelection.Height);

            viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));
            Assert.IsTrue(viewMock.CurrentSelection.IsEmpty);
            
            CollectionAssert.AreEqual(new[]{ 5, 6 }, presenter.Selection.OrderBy(x => x).ToArray());

            // make sure we have updated just the selection items
            var index = 0;
            foreach (var item in viewMock.Items)
            {
                if (index == 5 || index == 6)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else
                {
                    Assert.IsFalse(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_EndPointIsToTheLeft()
        {
            var viewMock = new ImagesViewMock(8, 8);
            var storage = new MemoryAttributeStorage();
            var thumbnailGenerator = new NullThumbnailGeneratorMock();
            var presenter = new ImagesPresenter(viewMock, storage, thumbnailGenerator);
            
            Assert.AreEqual(16, viewMock.Items.Count);

            viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 5, 3, 0));
            viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.Left, 0, 2, 2, 0));
            Assert.AreEqual(2, viewMock.CurrentSelection.X);
            Assert.AreEqual(2, viewMock.CurrentSelection.Y);
            Assert.AreEqual(3, viewMock.CurrentSelection.Width);
            Assert.AreEqual(1, viewMock.CurrentSelection.Height);

            viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 2, 2, 0));
            Assert.IsTrue(viewMock.CurrentSelection.IsEmpty);

            CollectionAssert.AreEqual(new[] { 5, 6 }, presenter.Selection.OrderBy(x => x).ToArray());

            // make sure we have updated just the selection items
            var index = 0;
            foreach (var item in viewMock.Items)
            {
                if (index == 5 || index == 6)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else
                {
                    Assert.IsFalse(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_UnionWithPreviousSelection()
        {
            var viewMock = new ImagesViewMock(8, 8);
            var storage = new MemoryAttributeStorage();
            var thumbnailGenerator = new NullThumbnailGeneratorMock();
            var presenter = new ImagesPresenter(viewMock, storage, thumbnailGenerator);

            // first selection
            viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 2, 2, 0));

            // second selection (union)
            viewMock.TriggerKeyDown(new KeyEventArgs(Keys.Shift));
            viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 5, 0, 0));
            viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 6, 0, 0));

            CollectionAssert.AreEqual(new[] { 3, 5 }, presenter.Selection.OrderBy(x => x).ToArray());

            var index = 0;
            foreach (var item in viewMock.Items)
            {
                if (index == 5 || index == 3)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else
                {
                    Assert.IsFalse(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_SymetricDifferenceWithPreviousSelection()
        {
            var viewMock = new ImagesViewMock(8, 8);
            var storage = new MemoryAttributeStorage();
            var thumbnailGenerator = new NullThumbnailGeneratorMock();
            var presenter = new ImagesPresenter(viewMock, storage, thumbnailGenerator);

            // first selection
            viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 2, 2, 0));

            // second selection (symetric difference)
            viewMock.TriggerKeyDown(new KeyEventArgs(Keys.Control));
            viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 5, 2, 0));
            viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 0, 2, 0));

            CollectionAssert.AreEqual(new[] { 4, 6 }, presenter.Selection.OrderBy(x => x).ToArray());

            var index = 0;
            foreach (var item in viewMock.Items)
            {
                if (index == 4 || index == 6)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else if (index == 5)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                else
                {
                    Assert.IsFalse(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_Reset()
        {
            var viewMock = new ImagesViewMock(8, 8);
            var storage = new MemoryAttributeStorage();
            var thumbnailGenerator = new NullThumbnailGeneratorMock();
            var presenter = new ImagesPresenter(viewMock, storage, thumbnailGenerator);

            // first selection
            viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));

            // reset the selection
            viewMock.TriggerMouseDown(new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0));
            viewMock.TriggerMouseUp(new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));

            CollectionAssert.AreEqual(new[] { 0 }, presenter.Selection.ToArray());

            var index = 0;
            foreach (var item in viewMock.Items)
            {
                if (index == 0)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else if (index == 5 || index == 6)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                else
                {
                    Assert.IsFalse(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_SelectAllWithCtrlA()
        {
            var viewMock = new ImagesViewMock(8, 8);
            var storage = new MemoryAttributeStorage();
            var thumbnailGenerator = new NullThumbnailGeneratorMock();
            var presenter = new ImagesPresenter(viewMock, storage, thumbnailGenerator);
            presenter.AddItemsInternal(Enumerable.Repeat<AttributeCollection>(null, 16));

            viewMock.TriggerKeyDown(new KeyEventArgs(Keys.A | Keys.Control));

            CollectionAssert.AreEqual(
                Enumerable.Range(0, 16).ToArray(), 
                presenter.Selection.OrderBy(x => x).ToArray());

            foreach (var item in viewMock.Items)
            {
                Assert.IsTrue(item.IsUpdated);
                Assert.AreEqual(ResultItemState.Selected, item.State);
            }
        }

        [TestMethod]
        public void ActiveItem_OnlyOneItemCanBeActive()
        {
            var viewMock = new ImagesViewMock(8, 8);
            var storage = new MemoryAttributeStorage();
            var thumbnailGenerator = new NullThumbnailGeneratorMock();
            var presenter = new ImagesPresenter(viewMock, storage, thumbnailGenerator);

            // move with mouse between items
            viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.None, 0, 1, 1, 0));
            Assert.AreEqual(-1, presenter.ActiveItem);
            foreach (var item in viewMock.Items)
            {
                Assert.IsFalse(item.IsUpdated);
                Assert.AreEqual(ResultItemState.None, item.State);
            }
            viewMock.ResetMock();

            // select an item
            viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.None, 0, 2, 2, 0));
            Assert.AreEqual(5, presenter.ActiveItem);
            var index = 0;
            foreach (var item in viewMock.Items)
            {
                if (index == 5)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.Active, item.State);
                }
                else
                {
                    Assert.IsFalse(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
            viewMock.ResetMock();

            // change an item
            viewMock.TriggerMouseMove(new MouseEventArgs(MouseButtons.None, 0, 4, 2, 0));
            Assert.AreEqual(6, presenter.ActiveItem);
            index = 0;
            foreach (var item in viewMock.Items)
            {
                if (index == 5)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                else if (index == 6)
                {
                    Assert.IsTrue(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.Active, item.State);
                }
                else
                {
                    Assert.IsFalse(item.IsUpdated);
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }
    }
}
