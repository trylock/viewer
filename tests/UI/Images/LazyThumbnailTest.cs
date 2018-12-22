using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sharpen;
using Viewer.Data;
using Viewer.Images;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class LazyThumbnailTest
    {
        private IEntity _entity;
        private Size _thumbnailSize;
        private Mock<IThumbnailLoader> _thumbnailLoader;
        private PhotoThumbnail _thumbnail;

        [TestInitialize]
        public void Setup()
        {
            _entity = new FileEntity("test");
            _thumbnailSize = new Size(100, 100);
            _thumbnailLoader = new Mock<IThumbnailLoader>();
            _thumbnail = new PhotoThumbnail(_thumbnailLoader.Object, _entity, CancellationToken.None);
        }

        [TestMethod]
        public void GetCurrent_StartLoadingImage()
        {
            _thumbnail.GetCurrent(_thumbnailSize);

            _thumbnailLoader.Verify(
                mock => mock.LoadEmbeddedThumbnailAsync(_entity, _thumbnailSize, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public void Current_ReplaceLoadedImageWithCurrentImage()
        {
            var image = new Bitmap(1, 1);
            var task = Task.FromResult(new Thumbnail(image, image.Size));
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, _thumbnailSize, It.IsAny<CancellationToken>()))
                .Returns(task);

            var current = _thumbnail.GetCurrent(_thumbnailSize);
            current = _thumbnail.GetCurrent(_thumbnailSize);

            Assert.AreEqual(image, current);
            _thumbnailLoader.Verify(
                mock => mock.LoadEmbeddedThumbnailAsync(_entity, It.IsAny<Size>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public void GetCurrent_ResizeThumbnailWithLoadedThumbnail()
        {
            var smallSize = new Size(1, 1);
            var largeSize = new Size(200, 200);
            var image1 = new Bitmap(1, 1);
            var image2 = new Bitmap(1, 1);
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, smallSize, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Thumbnail(image1, smallSize)));
            _thumbnailLoader
                .Setup(mock => mock.LoadNativeThumbnailAsync(_entity, largeSize, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Thumbnail(image2, largeSize)));

            var current = _thumbnail.GetCurrent(smallSize);
            current = _thumbnail.GetCurrent(smallSize);
            Assert.AreEqual(image1, current);
            current = _thumbnail.GetCurrent(largeSize);
            current = _thumbnail.GetCurrent(largeSize);
            Assert.AreEqual(image2, current);

            _thumbnailLoader.Verify(
                mock => mock.LoadEmbeddedThumbnailAsync(It.IsAny<IEntity>(), smallSize, It.IsAny<CancellationToken>()),
                Times.Once);
            _thumbnailLoader.Verify(
                mock => mock.LoadEmbeddedThumbnailAsync(It.IsAny<IEntity>(), largeSize, It.IsAny<CancellationToken>()),
                Times.Once);
            _thumbnailLoader.Verify(
                mock => mock.LoadNativeThumbnailAsync(It.IsAny<IEntity>(), It.IsAny<Size>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void GetCurrent_MissingEmbeddedThumbnail()
        {
            var size = new Size(200, 200);
            var image = new Bitmap(1, 1);
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromResult(new Thumbnail(null, new Size(1, 1))));
            _thumbnailLoader
                .Setup(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromResult(new Thumbnail(image, size)));

            var current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(PhotoThumbnail.Default, current);
            current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(image, current);

            _thumbnailLoader.Verify(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Once);
            _thumbnailLoader.Verify(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Once);
        }

        [TestMethod]
        public void GetCurrent_InvalidEmbeddedThumbnail()
        {
            var size = new Size(200, 200);
            var image = new Bitmap(1, 1);
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromException<Thumbnail>(new ArgumentException()));
            _thumbnailLoader
                .Setup(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromResult(new Thumbnail(image, size)));

            var current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(PhotoThumbnail.Default, current);
            current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(image, current);

            _thumbnailLoader.Verify(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Once);
            _thumbnailLoader.Verify(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Once);
        }

        [TestMethod]
        public void GetCurrent_InvalidNativeThumbnail()
        {
            var size = new Size(200, 200);
            var image = new Bitmap(1, 1);
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromResult(new Thumbnail(image, new Size(1, 1))));
            _thumbnailLoader
                .Setup(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromException<Thumbnail>(new ArgumentException()));

            var current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(image, current);
            current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(image, current);

            _thumbnailLoader.Verify(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Once);
            _thumbnailLoader.Verify(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Once);
        }

        [TestMethod]
        public async Task GetCurrent_RetryLoadingNativeThumbnailIfItsFileIsBusy()
        {
            PhotoThumbnail.RetryDelay = TimeSpan.FromMilliseconds(40);

            var size = new Size(200, 200);
            var embedded = new Bitmap(1, 1);
            var native = new Bitmap(1, 1);
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromResult(new Thumbnail(embedded, new Size(1, 1))));
            _thumbnailLoader
                .SetupSequence(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromException<Thumbnail>(new IOException()))
                .Returns(Task.FromResult(new Thumbnail(native, size)));

            var current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(embedded, current);
            current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(embedded, current);

            await Task.Delay(PhotoThumbnail.RetryDelay + PhotoThumbnail.RetryDelay);

            current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(native, current);

            _thumbnailLoader.Verify(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Once);
            _thumbnailLoader.Verify(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Exactly(2));
        }

        [TestMethod]
        public async Task GetCurrent_DontRetryLoadingNativeThumbnailIfItsFileIsNotBusy()
        {
            PhotoThumbnail.RetryDelay = TimeSpan.FromMilliseconds(40);

            var embedded = new Bitmap(1, 1);
            var size = new Size(200, 200);
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromResult(new Thumbnail(embedded, new Size(1, 1))));
            _thumbnailLoader
                .Setup(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None))
                .Returns(Task.FromException<Thumbnail>(new FileNotFoundException()));

            var current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(embedded, current);
            current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(embedded, current);

            await Task.Delay(PhotoThumbnail.RetryDelay + PhotoThumbnail.RetryDelay);

            current = _thumbnail.GetCurrent(size);
            Assert.AreEqual(embedded, current);

            _thumbnailLoader.Verify(mock => mock.LoadEmbeddedThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Once);
            _thumbnailLoader.Verify(mock => mock.LoadNativeThumbnailAsync(_entity, size, CancellationToken.None),
                Times.Once);
        }

        [TestMethod]
        public void GetCurrent_UsingThumbnailIsSafeAfterDisposing()
        {
            var image = new Bitmap(1, 1);
            var task = Task.FromResult(new Thumbnail(image, image.Size));
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, _thumbnailSize, It.IsAny<CancellationToken>()))
                .Returns(task);

            var current = _thumbnail.GetCurrent(_thumbnailSize);
            current = _thumbnail.GetCurrent(_thumbnailSize);

            Assert.AreEqual(image, current);
            _thumbnail.Dispose();

            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, _thumbnailSize, It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(50000).ContinueWith(_ => new Thumbnail(image, image.Size)));

            current = _thumbnail.GetCurrent(_thumbnailSize);
            Assert.AreEqual(current, PhotoThumbnail.Default);
        }

        [TestMethod]
        public async Task GetCurrent_DisposeDuringLoading()
        {
            var image = new Bitmap(1, 1);
            var task = Task.Delay(500).ContinueWith(_ => new Thumbnail(image, image.Size));
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, _thumbnailSize, It.IsAny<CancellationToken>()))
                .Returns(task);

            var current = _thumbnail.GetCurrent(_thumbnailSize);

            Assert.AreEqual(PhotoThumbnail.Default, current);
            _thumbnail.Dispose();

            var newImage = new Bitmap(1, 1);
            _thumbnailLoader
                .Setup(mock => mock.LoadEmbeddedThumbnailAsync(_entity, _thumbnailSize, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Thumbnail(newImage, newImage.Size)));

            current = _thumbnail.GetCurrent(_thumbnailSize);
            Assert.AreEqual(newImage, current);

            await task;

            // make sure the image has been disposed
            Assert.ThrowsException<ArgumentException>(() => image.Clone());
        }
    }
}
