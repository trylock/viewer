using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Exif;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Exif;

namespace ViewerTest.Data.Formats.Exif
{
    [TestClass]
    public class ExifAttributeParserTest
    {
        [TestMethod]
        public void Parse_InvalidDateFormat()
        {
            var dir = new ExifIfd0Directory();
            dir.Set(ExifIfd0Directory.TagDateTimeOriginal, "    :  :     :  :  ");

            var metadata = new Mock<IExifMetadata>();
            metadata.Setup(mock => mock.GetDirectoryOfType<ExifIfd0Directory>()).Returns(dir);

            var parser = new ExifAttributeParser<ExifIfd0Directory>("test", ExifIfd0Directory.TagDateTimeOriginal, AttributeType.DateTime);
            var attr = parser.Parse(metadata.Object);
            Assert.IsNull(attr);
        }
    }
}
