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

namespace ViewerTest.Images
{
    [TestClass]
    public class ThumbnailLoaderTest
    {
        private Mock<IImageLoader> _imageLoader;
        private Mock<IAttributeStorage> _storage;
        private Mock<IFileSystem> _fileSystem;
        private ThumbnailLoader _loader;

        [TestInitialize]
        public void Setup()
        {
            _fileSystem = new Mock<IFileSystem>();
            _imageLoader = new Mock<IImageLoader>();
            _storage = new Mock<IAttributeStorage>();
            _loader = new ThumbnailLoader(_imageLoader.Object, _storage.Object, _fileSystem.Object);
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
            var result = _loader.LoadEmbeddedThumbnailAsync(entity, new Size(1, 1), CancellationToken.None);

            Assert.AreEqual(TaskStatus.RanToCompletion, result.Status);
            Assert.AreEqual(Size.Empty, result.Result.OriginalSize);
            Assert.AreEqual(null, result.Result.ThumbnailImage);
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
            Assert.IsNull(entity.GetValue<ImageValue>(ExifAttributeReaderFactory.Thumbnail));
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
