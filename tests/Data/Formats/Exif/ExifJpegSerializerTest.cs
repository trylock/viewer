using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.Formats.Exif;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data.Formats.Exif
{
    [TestClass]
    public class ExifJpegSerializerTest
    {
        private byte[] _testSegment;

        [TestInitialize]
        public void Setup()
        {
            _testSegment = File.ReadAllBytes(Path.Combine(
                Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory)),
                "MiscTestData/test.exif"));
        }

        [TestMethod]
        public void Deserialzie_NoExifSegment()
        {
            var segments = new[]
            {
                new JpegSegment(JpegSegmentType.Soi, new byte[0], 0),
                new JpegSegment(JpegSegmentType.App1, new byte[] { 0x01, 0x02, 0x03 }, 0),
                new JpegSegment(JpegSegmentType.Sos, new byte[0], 0),
                new JpegSegment(JpegSegmentType.Eoi, new byte[0], 0),
            };
            var reader = new ExifJpegSerializer();
            var attributes = reader.Deserialize(segments);
            Assert.AreEqual(0, attributes.Count());
        }

        [TestMethod]
        public void Deserialize_ActualExifMetadata()
        {
            var segments = new[]
            {
                new JpegSegment(JpegSegmentType.Soi, new byte[0], 0),
                new JpegSegment(JpegSegmentType.App1, new byte[] { 0x01, 0x02, 0x03 }, 0),
                new JpegSegment(JpegSegmentType.App1, _testSegment, 0),
                new JpegSegment(JpegSegmentType.Sos, new byte[0], 0),
                new JpegSegment(JpegSegmentType.Eoi, new byte[0], 0),
            };
            
            var reader = new ExifJpegSerializer();
            var attributes = reader.Deserialize(segments).ToList();

            var orientation = attributes.Find(item => item.Name == ExifJpegSerializer.Orientation).Value;
            Assert.AreEqual(1, ((IntValue)orientation).Value);
        }
    }
}
