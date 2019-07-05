using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data.Formats.Jpeg;
using Viewer.Data.SQLite;
using Viewer.Data.Storage;
using Viewer.Images;
using Viewer.IO;
using Viewer.UI.Images;
using ViewerTest.Query.Execution;

namespace ViewerTest.Images
{
    [TestClass]
    public class ThumbnailLoaderIntegrationTest
    {
        private IThumbnailLoader _loader;
        private IAttributeStorage _storage;
        private CompositionContainer _container;

        [TestInitialize]
        public void Setup()
        {
            var catalog = new AggregateCatalog(
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Data.IEntity))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Query.IRuntime))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.QueryRuntime.IntValueAdditionFunction))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.IO.IFileSystem))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Images.IThumbnailLoader))));

            _container = new CompositionContainer(catalog);

            _container.GetExportedValue<IStorageConfiguration>();
            _storage = _container.GetExportedValue<IAttributeStorage>();
            _loader = _container.GetExportedValue<IThumbnailLoader>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _container.Dispose();
            _container = null;
        }

        private string GetPath(string name)
        {
            return Path.Combine(
                Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory)), "ImageTestData", name);
        }

        [TestMethod]
        public void LoadRGBImage()
        {
            var entity = _storage.Load(GetPath("rgb.jpg"));
            var thumbnail = _loader.LoadNativeThumbnailAsync(
                entity,
                new Size(200, 200),
                CancellationToken.None).Result;

            Assert.AreEqual(200, thumbnail.ThumbnailImage.Width);
            thumbnail.ThumbnailImage.Dispose();
        }

        [TestMethod]
        public void LoadGrayscaleImage()
        {
            var entity = _storage.Load(GetPath("grayscale.jpg"));
            var thumbnail = _loader.LoadNativeThumbnailAsync(
                entity,
                new Size(200, 200),
                CancellationToken.None).Result;

            Assert.AreEqual(200, thumbnail.ThumbnailImage.Width);
            thumbnail.ThumbnailImage.Dispose();
        }

        [TestMethod]
        public void LoadIndexedImage()
        {
            var entity = _storage.Load(GetPath("indexed.jpg"));
            var thumbnail = _loader.LoadNativeThumbnailAsync(
                entity,
                new Size(200, 200),
                CancellationToken.None).Result;

            Assert.AreEqual(200, thumbnail.ThumbnailImage.Width);
            thumbnail.ThumbnailImage.Dispose();
        }

        [TestMethod]
        public void LoadBlackAndWhiteImage()
        {
            var entity = _storage.Load(GetPath("indexed-binary.jpg"));
            var thumbnail = _loader.LoadNativeThumbnailAsync(
                entity,
                new Size(200, 200),
                CancellationToken.None).Result;

            Assert.AreEqual(200, thumbnail.ThumbnailImage.Width);
            thumbnail.ThumbnailImage.Dispose();
        }
    }
}
