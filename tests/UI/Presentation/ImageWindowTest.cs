using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Images;
using Viewer.UI.Presentation;

namespace ViewerTest.UI.Presentation
{
    [TestClass]
    public class ImageWindowTest
    {
        // entities in the presentation
        private FileEntity[] _entities;
        private Mock<IImageLoader> _imageLoader;
        private ImageWindow _window;

        [TestInitialize]
        public void Setup()
        {
            _entities = new[]
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
                new FileEntity("test3"),
                new FileEntity("test4"),
                new FileEntity("test5"),
                new FileEntity("test6"),
            };
            _imageLoader = new Mock<IImageLoader>();
            _window = new ImageWindow(_imageLoader.Object, _entities, 3);
        }

        [TestMethod]
        public async Task SetPosition_DisposeImagesOutsideOfTheWindow()
        {
            // size of the thumbnail corresponds to the entity number
            var image6First = new BitmapMock(6, 6);
            var image6Second = new BitmapMock(6, 6);
            var image1 = new BitmapMock(1, 1);
            var image2 = new BitmapMock(2, 2);
            var image3 = new BitmapMock(3, 3);
            var image5 = new BitmapMock(5, 5);
            _imageLoader
                .SetupSequence(mock => mock.LoadImage(It.Is<IEntity>(entity => entity == _entities[5])))
                .Returns(image6First)
                .Returns(image6Second);
            _imageLoader
                .Setup(mock => mock.LoadImage(It.Is<IEntity>(entity => entity == _entities[0])))
                .Returns(image1);
            _imageLoader
                .Setup(mock => mock.LoadImage(It.Is<IEntity>(entity => entity == _entities[1])))
                .Returns(image2);
            _imageLoader
                .Setup(mock => mock.LoadImage(It.Is<IEntity>(entity => entity == _entities[2])))
                .Returns(image3);
            _imageLoader
                .Setup(mock => mock.LoadImage(It.Is<IEntity>(entity => entity == _entities[4])))
                .Returns(image5);

            _window.SetPosition(0);
            var first = await _window.GetCurrentAsync();
            Assert.AreEqual(1, first.Width);

            _window.Next();
            var second = await _window.GetCurrentAsync();
            Assert.AreEqual(2, second.Width);

            _window.Previous();
            var third = await _window.GetCurrentAsync();
            Assert.AreEqual(1, third.Width);

            _window.Previous();
            var fourth = await _window.GetCurrentAsync();
            Assert.AreEqual(6, fourth.Width);

            // The window disposes images asynchronnously. This should give the window enought time
            // to finish all pending operations.
            await Task.Delay(100);

            Assert.IsFalse(image1.IsDisposed);
            Assert.IsTrue(image2.IsDisposed);
            Assert.IsTrue(image3.IsDisposed);
            Assert.IsFalse(image5.IsDisposed);
            Assert.IsTrue(image6First.IsDisposed);
            Assert.IsFalse(image6Second.IsDisposed);
        }
    }
}
