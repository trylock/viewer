using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data.Formats.Exif;

namespace ViewerTest.Data.Formats.Exif
{
    [TestClass]
    public class ExifMetadataTest
    {
        [TestMethod]
        public void GetDirectoryOfType_DirectoriesIsNull()
        {
            var metadata = new ExifMetadata(null, null);
            Assert.IsNull(metadata.GetDirectoryOfType<string>());
        }

        [TestMethod]
        public void GetDirectoryOfType_DirectoriesIsEmpty()
        {
            var directories = new List<Directory>();
            var metadata = new ExifMetadata(null, directories);
            Assert.IsNull(metadata.GetDirectoryOfType<ExifIfd0Directory>());
        }

        [TestMethod]
        public void GetDirectoryOfType_CorrectDirectory()
        {
            var firstDir = new ExifIfd0Directory();
            var secondDir = new ExifThumbnailDirectory();
            var directories = new List<Directory>{ firstDir, secondDir };
            var metadata = new ExifMetadata(null, directories);
            Assert.AreEqual(firstDir, metadata.GetDirectoryOfType<ExifIfd0Directory>());
        }
    }
}
