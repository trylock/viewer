using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class ThumbnailGeneratorTest
    {
        [TestMethod]
        public void GetThumbnailSize_HigherWidth()
        {
            var size = ThumbnailGenerator.GetThumbnailSize(new Size(800, 600), new Size(100, 200));
            Assert.AreEqual(100, size.Width);
            Assert.AreEqual(75, size.Height);

            size = ThumbnailGenerator.GetThumbnailSize(new Size(800, 600), new Size(200, 100));
            Assert.AreEqual(133, size.Width);
            Assert.AreEqual(100, size.Height);
        }

        [TestMethod]
        public void GetThumbnailSize_HigherHeight()
        {
            var size = ThumbnailGenerator.GetThumbnailSize(new Size(600, 800), new Size(200, 100));
            Assert.AreEqual(75, size.Width);
            Assert.AreEqual(100, size.Height);

            size = ThumbnailGenerator.GetThumbnailSize(new Size(600, 800), new Size(100, 200));
            Assert.AreEqual(100, size.Width);
            Assert.AreEqual(133, size.Height);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_ZeroOriginalImageHeight()
        {
            ThumbnailGenerator.GetThumbnailSize(new Size(10, 0), new Size(10, 10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_NegativeOriginalWidth()
        {
            ThumbnailGenerator.GetThumbnailSize(new Size(-10, 10), new Size(10, 10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_NegativeOriginalHeight()
        {
            ThumbnailGenerator.GetThumbnailSize(new Size(10, -10), new Size(10, 10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_NegativeTargetWidth()
        {
            ThumbnailGenerator.GetThumbnailSize(new Size(10, 10), new Size(-10, 10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_NegativeTargetHeight()
        {
            ThumbnailGenerator.GetThumbnailSize(new Size(10, 10), new Size(10, -10));
        }
    }
}
