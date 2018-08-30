using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;
using Viewer.Data;
using Viewer.Data.Formats.Exif;
using Viewer.Data.Storage;
using Viewer.Images;
using Viewer.IO;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class ThumbnailLoaderTest
    {
        private Mock<IImageLoader> _imageLoader;
        private Mock<IThumbnailGenerator> _thumbnailGenerator;
        private Mock<IAttributeStorage> _storage;
        private Mock<IFileSystem> _fileSystem;
        private ThumbnailLoader _loader;

        [TestInitialize]
        public void Setup()
        {
            _fileSystem = new Mock<IFileSystem>();
            _imageLoader = new Mock<IImageLoader>();
            _storage = new Mock<IAttributeStorage>();
            _thumbnailGenerator = new Mock<IThumbnailGenerator>();
            _loader = new ThumbnailLoader(_imageLoader.Object, _thumbnailGenerator.Object, _storage.Object, _fileSystem.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LoadEmbeddedThumbnailAsync_NullEntity()
        {
            _loader.LoadEmbeddedThumbnailAsync(null, Size.Empty, CancellationToken.None);
        }

        [TestMethod]
        public void LoadEmbeddedThumbnailAsync_NoEmbeddedThumbnail()
        {
            var entity = new FileEntity("test1");
            
            _imageLoader
                .Setup(mock => mock.LoadThumbnailAsync(entity, CancellationToken.None))
                .Returns(Task.FromResult<SKBitmap>(null));

            var result = _loader.LoadEmbeddedThumbnailAsync(entity, new Size(1, 1), CancellationToken.None);

            Assert.AreEqual(TaskStatus.RanToCompletion, result.Status);
            Assert.AreEqual(Size.Empty, result.Result.OriginalSize);
            Assert.AreEqual(null, result.Result.ThumbnailImage);
        }

        [TestMethod]
        public async Task LoadEmbeddedThumbnailAsync_InternalResultsAreDisposed()
        {
            var entity = new FileEntity("test1");
            var embeddedThumbnail = new BitmapMock();
            var generatedThumbnail = new BitmapMock();

            _imageLoader
                .Setup(mock => mock.LoadThumbnailAsync(entity, CancellationToken.None))
                .Returns(Task.FromResult<SKBitmap>(embeddedThumbnail));
            _thumbnailGenerator
                .Setup(mock => mock.GetThumbnail(embeddedThumbnail, new Size(1, 1)))
                .Returns(generatedThumbnail);

            Assert.IsFalse(embeddedThumbnail.IsDisposed);
            Assert.IsFalse(generatedThumbnail.IsDisposed);

            await _loader.LoadEmbeddedThumbnailAsync(entity, new Size(1, 1), CancellationToken.None);

            Assert.IsTrue(embeddedThumbnail.IsDisposed);
            Assert.IsTrue(generatedThumbnail.IsDisposed);
        }

        [TestMethod]
        public async Task LoadEmbeddedThumbnailAsync_LoadedThumbnailIsDisposedIfGeneratorThrows()
        {
            var entity = new FileEntity("test1");
            var embeddedThumbnail = new BitmapMock();

            _imageLoader
                .Setup(mock => mock.LoadThumbnailAsync(entity, CancellationToken.None))
                .Returns(Task.FromResult<SKBitmap>(embeddedThumbnail));
            _thumbnailGenerator
                .Setup(mock => mock.GetThumbnail(embeddedThumbnail, new Size(1, 1)))
                .Throws(new Exception("test"));

            Assert.IsFalse(embeddedThumbnail.IsDisposed);

            var exception = "";
            try
            {
                await _loader.LoadEmbeddedThumbnailAsync(entity, new Size(1, 1), CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e.Message;
            }

            Assert.AreEqual("test", exception);
            Assert.IsTrue(embeddedThumbnail.IsDisposed);
        }

        [TestMethod]
        public async Task LoadEmbeddedThumbnailAsync_TaskIsCanceledAfterAThumbnailIsGenerated()
        {
            var cancellation = new CancellationTokenSource();
            var entity = new FileEntity("test");
            var embeddedThumbnail = new BitmapMock();
            var generatedThumbnail = new BitmapMock();

            _imageLoader
                .Setup(mock => mock.LoadThumbnailAsync(entity, cancellation.Token))
                .Returns(Task.FromResult<SKBitmap>(embeddedThumbnail));
            _thumbnailGenerator
                .Setup(mock => mock.GetThumbnail(embeddedThumbnail, new Size(1, 1)))
                .Callback(() => cancellation.Cancel())
                .Returns(generatedThumbnail);

            Assert.IsFalse(embeddedThumbnail.IsDisposed);
            Assert.IsFalse(generatedThumbnail.IsDisposed);

            var result = await _loader.LoadEmbeddedThumbnailAsync(entity, new Size(1, 1), cancellation.Token);
            Assert.IsInstanceOfType(result.ThumbnailImage, typeof(Bitmap));
            Assert.IsTrue(embeddedThumbnail.IsDisposed);
            Assert.IsTrue(generatedThumbnail.IsDisposed);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task LoadEmbeddedThumbnailAsync_TaskIsCanceledAfterAThumbnailLoaded()
        {
            var cancellation = new CancellationTokenSource();
            var entity = new FileEntity("test");
            var embeddedThumbnail = new BitmapMock();

            _imageLoader
                .Setup(mock => mock.LoadThumbnailAsync(entity, cancellation.Token))
                .Callback(() => cancellation.Cancel())
                .Returns(Task.FromResult<SKBitmap>(embeddedThumbnail));

            Assert.IsFalse(embeddedThumbnail.IsDisposed);
            
            try
            {
                await _loader.LoadEmbeddedThumbnailAsync(entity, new Size(1, 1), cancellation.Token);
            }
            finally
            {
                Assert.IsTrue(embeddedThumbnail.IsDisposed);

                _thumbnailGenerator.Verify(mock => mock.GetThumbnail(It.IsAny<SKBitmap>(), It.IsAny<Size>()), Times.Never);
            }
        }

        [TestMethod]
        public async Task LoadNativeThumbnailAsync_DisposeIntermediateResults()
        {
            var originalImage = new BitmapMock();
            var thumbnailImage = new BitmapMock();
            var entity = new FileEntity("test");
            _fileSystem
                .Setup(mock => mock.ReadAllBytes(entity.Path))
                .Returns(new byte[] { 0x42 });
            _imageLoader
                .Setup(mock => mock.LoadImage(entity, It.Is<Stream>(stream => stream.ReadByte() == 0x42 && stream.ReadByte() < 0)))
                .Returns(originalImage);
            _thumbnailGenerator
                .Setup(mock => mock.GetThumbnail(originalImage, new Size(1, 1)))
                .Returns(thumbnailImage);
            
            Assert.IsFalse(originalImage.IsDisposed);
            Assert.IsFalse(thumbnailImage.IsDisposed);
            var image = await _loader.LoadNativeThumbnailAsync(entity, new Size(1, 1), CancellationToken.None);
            
            Assert.IsNotNull(entity.GetValue<ImageValue>(ExifAttributeReaderFactory.ThumbnailAttrName));
            Assert.IsTrue(originalImage.IsDisposed);
            Assert.IsTrue(thumbnailImage.IsDisposed);
        }

        [TestMethod]
        public async Task LoadNativeThumbnailAsync_DisposeOriginalImageIfGeneratorthrows()
        {
            var originalImage = new BitmapMock();
            var entity = new FileEntity("test");
            _fileSystem
                .Setup(mock => mock.ReadAllBytes(entity.Path))
                .Returns(new byte[] { 0x42 });
            _imageLoader
                .Setup(mock => mock.LoadImage(entity, It.Is<Stream>(stream => stream.ReadByte() == 0x42 && stream.ReadByte() < 0)))
                .Returns(originalImage);
            _thumbnailGenerator
                .Setup(mock => mock.GetThumbnail(originalImage, new Size(1, 1)))
                .Throws(new Exception("test"));

            Assert.IsFalse(originalImage.IsDisposed);
            var exception = "";
            try
            {
                var image = await _loader.LoadNativeThumbnailAsync(entity, new Size(1, 1), CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e.Message;
            }

            Assert.AreEqual("test", exception);
            Assert.IsNull(entity.GetValue<ImageValue>(ExifAttributeReaderFactory.ThumbnailAttrName));
            Assert.IsTrue(originalImage.IsDisposed);
        }

        [TestMethod]
        public async Task LoadNativeThumbnailAsync_ReturnExceptionsThrownByImageLoader()
        {
            var entity = new FileEntity("test");
            _fileSystem
                .Setup(mock => mock.ReadAllBytes(entity.Path))
                .Returns(new byte[] { 0x42 });
            _imageLoader
                .Setup(mock => mock.LoadImage(entity, It.Is<Stream>(stream => stream.ReadByte() == 0x42 && stream.ReadByte() < 0)))
                .Throws(new Exception("loader exception"));
            
            var exception = "";
            try
            {
                var image = await _loader.LoadNativeThumbnailAsync(entity, new Size(1, 1), CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e.Message;
            }

            Assert.AreEqual("loader exception", exception);
            Assert.IsNull(entity.GetValue<ImageValue>(ExifAttributeReaderFactory.ThumbnailAttrName));
        }

        [TestMethod]
        public async Task LoadNativeThumbnailAsync_ReturnExceptionsThrownByFileSystem()
        {
            var entity = new FileEntity("test");
            _fileSystem
                .Setup(mock => mock.ReadAllBytes(entity.Path))
                .Throws(new Exception("IO exception"));

            var exception = "";
            try
            {
                var image = await _loader.LoadNativeThumbnailAsync(entity, new Size(1, 1), CancellationToken.None);
            }
            catch (Exception e)
            {
                exception = e.Message;
            }

            Assert.AreEqual("IO exception", exception);
            Assert.IsNull(entity.GetValue<ImageValue>(ExifAttributeReaderFactory.ThumbnailAttrName));
        }

        [TestMethod]
        public async Task LoadNativeThumbnailAsync_CancelAfterAThumbnailHasBeenGenerated()
        {
            var cancellation = new CancellationTokenSource();
            var originalImage = new BitmapMock();
            var thumbnailImage = new BitmapMock();
            var entity = new FileEntity("test");
            _fileSystem
                .Setup(mock => mock.ReadAllBytes(entity.Path))
                .Returns(new byte[] { 0x42 });
            _imageLoader
                .Setup(mock => mock.LoadImage(entity, It.Is<Stream>(stream => stream.ReadByte() == 0x42 && stream.ReadByte() < 0)))
                .Returns(originalImage);
            _thumbnailGenerator
                .Setup(mock => mock.GetThumbnail(originalImage, new Size(1, 1)))
                .Returns(() =>
                {
                    cancellation.Cancel();
                    return thumbnailImage;
                });

            Assert.IsFalse(originalImage.IsDisposed);
            Assert.IsFalse(thumbnailImage.IsDisposed);
            var isCanceled = false;
            try
            {
                var image = await _loader.LoadNativeThumbnailAsync(entity, new Size(1, 1), cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                isCanceled = true;
            }
            
            Assert.IsTrue(isCanceled);
            Assert.IsNull(entity.GetValue<ImageValue>(ExifAttributeReaderFactory.ThumbnailAttrName));
            Assert.IsTrue(originalImage.IsDisposed);
            Assert.IsTrue(thumbnailImage.IsDisposed);
        }

        [TestMethod]
        public async Task LoadNativeThumbnailAsync_CancelAfterAnImageHasBeenDecoded()
        {
            var cancellation = new CancellationTokenSource();
            var originalImage = new BitmapMock();
            var entity = new FileEntity("test");
            _fileSystem
                .Setup(mock => mock.ReadAllBytes(entity.Path))
                .Returns(new byte[] { 0x42 });
            _imageLoader
                .Setup(mock => mock.LoadImage(entity, It.Is<Stream>(stream => stream.ReadByte() == 0x42 && stream.ReadByte() < 0)))
                .Returns(() =>
                {
                    cancellation.Cancel();
                    return originalImage;
                });

            Assert.IsFalse(originalImage.IsDisposed);
            var isCanceled = false;
            try
            {
                var image = await _loader.LoadNativeThumbnailAsync(entity, new Size(1, 1), cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                isCanceled = true;
            }

            Assert.IsTrue(isCanceled);
            Assert.IsNull(entity.GetValue<ImageValue>(ExifAttributeReaderFactory.ThumbnailAttrName));
            Assert.IsTrue(originalImage.IsDisposed);
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task LoadNativeThumbnailAsync_ProcessCanceledRequest()
        {
            var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var entity = new FileEntity("test");

            var tasks = new Task[100];
            for (var i = 0; i < tasks.Length; ++i)
            {
                tasks[i] = _loader.LoadNativeThumbnailAsync(entity, new Size(1, 1), cancellation.Token);
            }
            
            await Task.WhenAll(tasks);
        }
    }
}
