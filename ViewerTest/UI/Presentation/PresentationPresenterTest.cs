using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Images;
using Viewer.UI;
using Viewer.UI.Explorer;
using Viewer.UI.Presentation;

namespace ViewerTest.UI.Presentation
{
    [TestClass]
    public class PresentationPresenterTest
    {
        private Mock<IImageLoader> _imageLoader;
        private Mock<IPresentationView> _view;
        private Mock<ISelection> _selection;
        private Mock<IFileSystemErrorView> _dialogView;
        private PresentationPresenter _presenter;

        [TestInitialize]
        public void Setup()
        {
            _imageLoader = new Mock<IImageLoader>();
            _selection = new Mock<ISelection>();
            _dialogView = new Mock<IFileSystemErrorView>();
            _view = new Mock<IPresentationView>();

            var viewFactory = new ExportFactory<IPresentationView>(() =>
            {
                return new Tuple<IPresentationView, Action>(_view.Object, () => { });
            });
            _presenter = new PresentationPresenter(viewFactory, _selection.Object, _imageLoader.Object, _dialogView.Object);
        }

        [TestMethod]
        public void ViewGotFocus_PresenterIsNotInitialized()
        {
            _view.Raise(mock => mock.ViewGotFocus += null, EventArgs.Empty);

            // don't update the selection if the presenter does not have any data yet
            _selection.Verify(mock => mock.Replace(It.IsAny<IEnumerable<IEntity>>()), Times.Never);
        }
    }
}
