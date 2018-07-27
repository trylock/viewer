using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Images;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class LazyImageTest
    {
        private IEntity _entity;
        private Size _thumbnailSize;
        private Mock<IImageLoader> _imageLoader;
        private Mock<IThumbnailGenerator> _thumbnailGenerator;
        private PhotoThumbnail _thumbnail;

        [TestInitialize]
        public void Setup()
        {
            _entity = new Entity("test");
            _thumbnailSize = new Size(100, 100);
            _imageLoader = new Mock<IImageLoader>();
            _thumbnailGenerator = new Mock<IThumbnailGenerator>();
            _thumbnail = new PhotoThumbnail(_imageLoader.Object, _thumbnailGenerator.Object, _entity);
        }

        [TestMethod]
        public void Current_StartLoadingImage()
        {
            var image = _thumbnail.GetCurrent(_thumbnailSize);
            image = _thumbnail.GetCurrent(_thumbnailSize);

            _imageLoader.Verify(mock => mock.LoadThumbnailAsync(_entity), Times.Once);
        }

        [TestMethod]
        public void Current_ReplaceLoadedImageWithCurrentImage()
        {
            var image = new Bitmap(1, 1);
            var task = Task.FromResult((Image) image);
            _imageLoader
                .Setup(mock => mock.LoadThumbnailAsync(_entity))
                .Returns(task);
            _thumbnailGenerator
                .Setup(mock => mock.GetThumbnail(image, _thumbnailSize))
                .Returns(image);

            var current = _thumbnail.GetCurrent(_thumbnailSize);
            current = _thumbnail.GetCurrent(_thumbnailSize);

            Assert.AreEqual(image, current);
            _imageLoader.Verify(mock => mock.LoadThumbnailAsync(_entity), Times.Once);
        }
        
        [TestMethod]
        public void Resize_ResizeThumbnailWithLoadedThumbnail()
        {
            var image1 = new Bitmap(1, 1);
            var image2 = new Bitmap(1, 1);
            _imageLoader
                .Setup(mock => mock.LoadThumbnailAsync(_entity))
                .Returns(Task.FromResult((Image)image1));
            _imageLoader
                .Setup(mock => mock.LoadThumbnailAsync(_entity))
                .Returns(Task.FromResult((Image)image2));

            var current = _thumbnail.GetCurrent(_thumbnailSize);
            current = _thumbnail.GetCurrent(_thumbnailSize);
            Assert.AreEqual(image1, current);
            _thumbnailSize = new Size(200, 200);
            Assert.AreEqual(image1, current);
            current = _thumbnail.GetCurrent(_thumbnailSize);
            Assert.AreEqual(image2, current);
        }
    }
}
