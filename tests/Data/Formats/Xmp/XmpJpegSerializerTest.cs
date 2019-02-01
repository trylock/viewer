using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.Formats.Xmp;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data.Formats.Xmp
{
    [TestClass]
    public class XmpJpegSerializerTest
    {
        [TestMethod]
        public void Serialize_FileWithoutXmpSegments()
        {
            var expectedAttributes = new[]
            {
                new Attribute("int", new IntValue(42), AttributeSource.Custom),
                new Attribute("real", new RealValue(3.14159), AttributeSource.Custom),
                new Attribute("string", new StringValue("hello"), AttributeSource.Custom),
                new Attribute("date", new DateTimeValue(new DateTime(2019, 1, 20, 15, 16, 17)), AttributeSource.Custom),
            };

            var segmentsInFile = new[]
            {
                new JpegSegment(JpegSegmentType.Soi, new byte[0], 0),
                new JpegSegment(JpegSegmentType.App1, Encoding.UTF8.GetBytes("JFIF\0"), 0),
            };

            var serializer = new XmpJpegSerializer();
            var segments = serializer.Serialize(segmentsInFile, expectedAttributes);
            var actualAttributes = serializer.Deserialize(segments).ToArray();

            CollectionAssert.AreEqual(expectedAttributes, actualAttributes);
        }
    }
}
