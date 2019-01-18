using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.UI;
using Viewer.UI.Images;
using Viewer.UI.Images.Layout;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class SelectionStateTest
    {
        private Mock<ISelection> _selection;
        private Mock<ISelectionView> _view;
        private Mock<ImagesLayout> _layout;
        private SelectionState _state;

        [TestInitialize]
        public void Setup()
        {
            _layout = new Mock<ImagesLayout>();
            _layout
                .Setup(mock => mock.AreSameQueries(It.IsAny<Rectangle>(), It.IsAny<Rectangle>()))
                .Returns(false);

            _selection = new Mock<ISelection>();
            _view = new Mock<ISelectionView>();
            _view
                .Setup(mock => mock.ItemLayout)
                .Returns(_layout.Object);
            _state = new SelectionState(_view.Object, _selection.Object);
        }

        [TestMethod]
        public void RangeSelection()
        {
            var items = new[]
            {
                new EntityView(new FileEntity("test"), new Mock<ILazyThumbnail>().Object),
                new EntityView(new FileEntity("test2"), new Mock<ILazyThumbnail>().Object),
                new EntityView(new FileEntity("test3"), new Mock<ILazyThumbnail>().Object),
            };

            _view
                .Setup(mock => mock.GetItemAt(new Point(10, 10)))
                .Returns<EntityView>(null);
            _view
                .Setup(mock => mock.GetItemsIn(new Rectangle(1, 1, 9, 9)))
                .Returns(items.Take(2).ToList());
            _view
                .Setup(mock => mock.ModifierKeyState)
                .Returns(Keys.None);
            _selection
                .Setup(mock => mock.GetEnumerator())
                .Returns(new List<IEntity>().GetEnumerator());

            // left mouse button in an empty space at (10, 10)
            _view.Raise(view => view.ProcessMouseDown += null, this, new MouseEventArgs(MouseButtons.Left, 0, 10, 10, 0));

            _view.Verify(mock => mock.GetItemAt(new Point(10, 10)), Times.Once);
            _view.Verify(mock => mock.UpdateItems(), Times.Once);

            // drag cursor to (1, 1)
            _view.Raise(view => view.ProcessMouseMove += null, this, new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            
            _view.Verify(mock => mock.GetItemsIn(new Rectangle(1, 1, 9, 9)), Times.Once);
            _view.Verify(mock => mock.ShowSelection(new Rectangle(1, 1, 9, 9)), Times.Once);
            _view.Verify(mock => mock.UpdateItems(), Times.Exactly(2));

            // release mouse button at (1, 1)
            _view.Raise(view => view.ProcessMouseUp += null, this, new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            
            _view.Verify(mock => mock.GetItemsIn(new Rectangle(1, 1, 9, 9)), Times.Exactly(2));
            _view.Verify(mock => mock.HideSelection(), Times.Once);
            _view.Verify(mock => mock.UpdateItems(), Times.Exactly(3));
            
            _view.Verify(mock => mock.ModifierKeyState, Times.AtLeastOnce);
            _view.Verify(mock => mock.ItemLayout, Times.AtLeastOnce);
            _view.VerifyNoOtherCalls();

            // verify that view items have been update properly
            Assert.AreEqual(EntityViewState.Selected, items[0].State);
            Assert.AreEqual(EntityViewState.Selected, items[1].State);
            Assert.AreEqual(EntityViewState.None, items[2].State);

            // verify that global selection has been updated properly
            _selection.Verify(mock => mock.GetEnumerator(), Times.AtLeastOnce);
            _selection.Verify(mock => mock.Replace(It.Is<IEnumerable<IEntity>>(entities => 
                entities.SequenceEqual(items.Select(item => item.Data).Take(2))
            )), Times.AtLeastOnce);
            _selection.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void RightClickOutsideOfSelectionResetsSelection()
        {
            var items = new[]
            {
                new EntityView(new FileEntity("test"), new Mock<ILazyThumbnail>().Object),
                new EntityView(new FileEntity("test2"), new Mock<ILazyThumbnail>().Object),
                new EntityView(new FileEntity("test3"), new Mock<ILazyThumbnail>().Object),
            };
            
            _view
                .Setup(mock => mock.GetItemAt(new Point(10, 10)))
                .Returns<EntityView>(null);
            _view
                .Setup(mock => mock.GetItemAt(new Point(5, 5)))
                .Returns(items[0]);
            _view
                .Setup(mock => mock.ModifierKeyState)
                .Returns(Keys.None);
            _selection
                .SetupSequence(mock => mock.GetEnumerator())
                .Returns(new List<IEntity>{}.GetEnumerator())
                .Returns(new List<IEntity> { items[0].Data }.GetEnumerator())
                .Returns(new List<IEntity> { items[0].Data }.GetEnumerator());

            // click on the first item to select it
            _view.Raise(view => view.ProcessMouseDown += null, this, new MouseEventArgs(MouseButtons.Left, 0, 5, 5, 0));
            _view.Raise(view => view.ProcessMouseUp += null, this, new MouseEventArgs(MouseButtons.Left, 0, 5, 5, 0));
            
            _view.Verify(mock => mock.GetItemAt(new Point(5, 5)), Times.Exactly(2));
            _view.Verify(mock => mock.UpdateItems(), Times.Once);
            Assert.AreEqual(EntityViewState.Selected, items[0].State);
            Assert.AreEqual(EntityViewState.None, items[1].State);
            Assert.AreEqual(EntityViewState.None, items[2].State);

            // right mouse button in an empty space at (10, 10)
            _view.Raise(view => view.ProcessMouseDown += null, this, new MouseEventArgs(MouseButtons.Right, 0, 10, 10, 0));

            _view.Verify(mock => mock.GetItemAt(new Point(10, 10)), Times.Once);

            Assert.AreEqual(EntityViewState.Selected, items[0].State);
            Assert.AreEqual(EntityViewState.None, items[1].State);
            Assert.AreEqual(EntityViewState.None, items[2].State);

            // release mouse button in an empty space at (10, 10)
            _view.Raise(view => view.ProcessMouseUp += null, this, new MouseEventArgs(MouseButtons.Right, 0, 10, 10, 0));

            _view.Verify(mock => mock.GetItemAt(new Point(10, 10)), Times.Exactly(2));
            _view.Verify(mock => mock.UpdateItems(), Times.Exactly(2));
            
            _view.Verify(mock => mock.ModifierKeyState, Times.AtLeastOnce);
            _view.VerifyNoOtherCalls();

            // verify that view items have been update properly
            Assert.AreEqual(EntityViewState.None, items[0].State);
            Assert.AreEqual(EntityViewState.None, items[1].State);
            Assert.AreEqual(EntityViewState.None, items[2].State);

            // verify that global selection has been updated properly
            _selection.Verify(mock => mock.GetEnumerator(), Times.AtLeastOnce);
            _selection.Verify(mock => mock.Replace(It.Is<IEnumerable<IEntity>>(entities =>
                entities.Count() == 1 && entities.First() == items[0].Data
            )), Times.AtLeastOnce);
            _selection.Verify(mock => mock.Replace(It.Is<IEnumerable<IEntity>>(entities =>
                entities.SequenceEqual(Enumerable.Empty<IEntity>())
            )), Times.AtLeastOnce);
            _selection.VerifyNoOtherCalls();
        }
    }
}
