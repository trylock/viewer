using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Images;
using Viewer.UI.Images;

namespace ViewerTest.Images
{
    [TestClass]
    public class ThumbnailTest
    {
        [TestMethod]
        public void GetThumbnailSize_HigherWidth()
        {
            var size = Thumbnail.GetThumbnailSize(new Size(800, 600), new Size(100, 200));
            Assert.AreEqual(100, size.Width);
            Assert.AreEqual(75, size.Height);

            size = Thumbnail.GetThumbnailSize(new Size(800, 600), new Size(200, 100));
            Assert.AreEqual(133, size.Width);
            Assert.AreEqual(100, size.Height);
        }

        [TestMethod]
        public void GetThumbnailSize_HigherHeight()
        {
            var size = Thumbnail.GetThumbnailSize(new Size(600, 800), new Size(200, 100));
            Assert.AreEqual(75, size.Width);
            Assert.AreEqual(100, size.Height);

            size = Thumbnail.GetThumbnailSize(new Size(600, 800), new Size(100, 200));
            Assert.AreEqual(100, size.Width);
            Assert.AreEqual(133, size.Height);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_ZeroOriginalImageHeight()
        {
            Thumbnail.GetThumbnailSize(new Size(10, 0), new Size(10, 10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_NegativeOriginalWidth()
        {
            Thumbnail.GetThumbnailSize(new Size(-10, 10), new Size(10, 10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_NegativeOriginalHeight()
        {
            Thumbnail.GetThumbnailSize(new Size(10, -10), new Size(10, 10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_NegativeTargetWidth()
        {
            Thumbnail.GetThumbnailSize(new Size(10, 10), new Size(-10, 10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetThumbnailSize_NegativeTargetHeight()
        {
            Thumbnail.GetThumbnailSize(new Size(10, 10), new Size(10, -10));
        }

        [TestMethod]
        public void GetThumbnailSize_HeightIsNever0()
        {
            var size = Thumbnail.GetThumbnailSize(new Size(1024, 1), new Size(100, 100));
            Assert.AreEqual(100, size.Width);
            Assert.AreEqual(1, size.Height);
        }

        [TestMethod]
        public void GetThumbnailSize_WidthIsNever0()
        {
            var size = Thumbnail.GetThumbnailSize(new Size(1, 1024), new Size(100, 100));
            Assert.AreEqual(1, size.Width);
            Assert.AreEqual(100, size.Height);
        }
    }
}
