using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Images;
using Viewer.UI;
using Viewer.UI.Images;
using ViewerTest.Data;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class ImagesPresenterTest
    {
        private IAttributeStorage _storage;
        private Mock<IImagesView> _viewMock;
        private Mock<ISelection> _selectionMock;
        private Mock<IClipboardService> _clipboardMock;
        private Mock<IEntityManager> _entities;
        private Mock<IQueryEvaluator> _evaluator;
        private Mock<IApplicationState> _state;
        private ImagesPresenter _presenter;

        private List<Entity> _data;
        private List<EntityView> _items;

        [TestInitialize]
        public void Setup()
        {
            _storage = new MemoryAttributeStorage();
            _viewMock = new Mock<IImagesView>();
            _clipboardMock = new Mock<IClipboardService>();
            _entities = new Mock<IEntityManager>();
            _evaluator = new Mock<IQueryEvaluator>();
            _state = new Mock<IApplicationState>();

            _data = new List<Entity>();
            _items = new List<EntityView>();
            for (int i = 0; i < 16; ++i)
            {
                var index = i;
                var entity = new Entity(i.ToString());
                _storage.Store(entity);
                _data.Add(entity);
                _items.Add(new EntityView(entity, null));
                _entities.Setup(mock => mock[index]).Returns(_data[index]);
            }

            _entities.Setup(mock => mock.GetEnumerator()).Returns(_data.GetEnumerator());

            var viewFactory = new ExportFactory<IImagesView>(() =>
            {
                return new Tuple<IImagesView, Action>(_viewMock.Object, () => { });
            });
            
            var imageLoaderMock = new Mock<IImageLoader>();
            imageLoaderMock.Setup(mock => mock.GetImageSize(It.IsAny<IEntity>())).Returns(new Size(1, 1));

            _selectionMock = new Mock<ISelection>();
            _presenter = new ImagesPresenter(viewFactory, null, _selectionMock.Object, _storage, _clipboardMock.Object, imageLoaderMock.Object, _state.Object, _evaluator.Object);
            _presenter.ShowEntities(_entities.Object);
            
            _viewMock.Setup(mock => mock.Items).Returns(_items);
        }

        [TestMethod]
        public void Selection_EndPointIsToTheRight()
        {
            // there is no item at [1, 1] or [4, 2]
            _viewMock.Setup(mock => mock.GetItemAt(new Point(1, 1))).Returns(-1);
            _viewMock.Setup(mock => mock.GetItemAt(new Point(4, 2))).Returns(-1);

            // select items with a mouse cursor
            _viewMock.Setup(mock => mock.GetItemsIn(new Rectangle(1, 1, 3, 1))).Returns(new[] { 5, 6 });
            
            // test selection
            _viewMock.Raise(mock => mock.HandleMouseDown += null, new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            _viewMock.Raise(mock => mock.HandleMouseMove += null, new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));
            _viewMock.Raise(mock => mock.HandleMouseUp += null, new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));

            // presenter updated items 5 and 6
            _viewMock.Verify(mock => mock.UpdateItems(new int[]{ 5, 6 }));
            
            // make sure we have updated just the selection items
            var index = 0;
            foreach (var item in _items)
            {
                if (index == 5 || index == 6)
                {
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else
                {
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_ResetSelectionWhenUserClicksOnItemThatIsNotSelected()
        {
            // there is no item at [1, 1] or [4, 2]
            _viewMock.Setup(mock => mock.GetItemAt(new Point(1, 1))).Returns(-1);
            _viewMock.Setup(mock => mock.GetItemAt(new Point(4, 2))).Returns(-1);

            // there is an item at [0, 0]
            _viewMock.Setup(mock => mock.GetItemAt(new Point(0, 0))).Returns(0);

            // select items with a mouse cursor
            _viewMock.Setup(mock => mock.GetItemsIn(new Rectangle(1, 1, 3, 1))).Returns(new[] { 5, 6 });

            // simulate first selection
            _viewMock.Raise(mock => mock.HandleMouseDown += null, new MouseEventArgs(MouseButtons.Left, 0, 1, 1, 0));
            _viewMock.Raise(mock => mock.HandleMouseMove += null, new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));
            _viewMock.Raise(mock => mock.HandleMouseUp += null, new MouseEventArgs(MouseButtons.Left, 0, 4, 2, 0));

            // simulate second selection
            _viewMock.Raise(mock => mock.HandleMouseDown += null, new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
            
            // presenter updated view
            _viewMock.Verify(mock => mock.UpdateItems(new []{ 5, 6 }));
            _viewMock.Verify(mock => mock.UpdateItems(new[] { 0 }));

            // make sure we have updated just the selection items
            var index = 0;
            foreach (var item in _items)
            {
                if (index == 0)
                {
                    Assert.AreEqual(ResultItemState.Selected, item.State);
                }
                else
                {
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void ActiveItem_ChangeActiveItem()
        {
            // there are items at [1, 1] and [2, 1]
            _viewMock.Setup(mock => mock.GetItemAt(new Point(1, 1))).Returns(0);
            _viewMock.Setup(mock => mock.GetItemAt(new Point(2, 1))).Returns(1);
            
            // simulate mouse movement
            _viewMock.Raise(mock => mock.HandleMouseMove += null, new MouseEventArgs(MouseButtons.None, 0, 1, 1, 0));
            _viewMock.Raise(mock => mock.HandleMouseMove += null, new MouseEventArgs(MouseButtons.None, 0, 2, 1, 0));

            // presenter updated view
            _viewMock.Verify(mock => mock.UpdateItem(0));
            _viewMock.Verify(mock => mock.UpdateItem(1));

            var index = 0;
            foreach (var item in _items)
            {
                if (index == 1)
                {
                    Assert.AreEqual(ResultItemState.Active, item.State);
                }
                else
                {
                    Assert.AreEqual(ResultItemState.None, item.State);
                }
                ++index;
            }
        }

        [TestMethod]
        public void Selection_SelectAllItemsOnControlPlusA()
        {
            _viewMock.Raise(mock => mock.HandleKeyDown += null, new KeyEventArgs(Keys.Control | Keys.A));

            _viewMock.Verify(mock => mock.UpdateItems());

            foreach (var item in _items)
            {
                Assert.AreEqual(ResultItemState.Selected, item.State);
            }
        }

        [TestMethod]
        public void BeginEditItemName_NoSelectedItem()
        {
            _viewMock.Raise(mock => mock.BeginEditItemName += null, EventArgs.Empty);

            // presenter did not show the edit form
            _viewMock.Verify(mock => mock.ShowItemEditForm(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void BeginEditItemName_EditFocusedItem()
        {
            _viewMock.Setup(mock => mock.GetItemAt(new Point(0, 0))).Returns(0);
            
            var testEntity = _storage.Load("test");
            Assert.IsNull(testEntity);
            Assert.AreEqual("0", _items[0].Data.Path);

            // user selects an item and modify its name
            _viewMock.Raise(mock => mock.HandleMouseDown += null, new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));

            // show the edit form
            _viewMock.Raise(mock => mock.BeginEditItemName += null, EventArgs.Empty);
            _viewMock.Verify(mock => mock.ShowItemEditForm(0));

            // rename the item
            _viewMock.Raise(mock => mock.RenameItem += null, new RenameEventArgs("test"));
            _viewMock.Verify(mock => mock.HideItemEditForm());
            _viewMock.Verify(mock => mock.UpdateItem(0));

            testEntity = _storage.Load("test");
            Assert.AreEqual("test", _items[0].Data.Path);
            Assert.IsNotNull(testEntity);
        }
        
    }
}
