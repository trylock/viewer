using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Formats.Exif;

namespace ViewerTest.Data.Formats.Exif
{
    [TestClass]
    public class ThumbnailExifAttributeParserTest
    {
        [TestMethod]
        public void Parse_MissingOffsetTag()
        {
            var dir = new ExifThumbnailDirectory();
            var metadata = new Mock<IExifMetadata>();
            metadata.Setup(mock => mock.GetDirectoryOfType<ExifDirectoryBase>()).Returns(dir);

            var parser = new ThumbnaiExifAttributeParser<ExifDirectoryBase>("thumbnail");
            Assert.IsNull(parser.Parse(metadata.Object));
        }

        [TestMethod]
        public void Parse_ThumbnailData()
        {
            var dir = new ExifThumbnailDirectory();
            dir.Set(ExifThumbnailDirectory.TagThumbnailOffset, 0);
            dir.Set(ExifThumbnailDirectory.TagThumbnailLength, 2);

            var segment = new JpegSegment(JpegSegmentType.App1, new byte[]
            {
                (byte)'E',
                (byte)'x',
                (byte)'i',
                (byte)'f',
                (byte)'\0',
                (byte)'\0',
                0x12, 0x23, 0x34
            }, 0);

            var metadata = new Mock<IExifMetadata>();
            metadata.Setup(mock => mock.Segment).Returns(segment);
            metadata.Setup(mock => mock.GetDirectoryOfType<ExifDirectoryBase>()).Returns(dir);

            var parser = new ThumbnaiExifAttributeParser<ExifDirectoryBase>("thumbnail");
            var thumbnail = parser.Parse(metadata.Object).Value as ImageValue;
            CollectionAssert.AreEqual(new byte[]{ 0x12, 0x23 }, thumbnail.Value);
        }
    }
}
