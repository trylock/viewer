using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data.Formats.Attributes;

namespace ViewerTest.Data.Formats.Attributes
{
    [TestClass]
    public class JpegSegmentByteReaderTest
    {
        [TestMethod]
        [ExpectedException(typeof(EndOfStreamException))]
        public void ReadByte_EmptyInput()
        {
            var reader = new JpegSegmentByteReader(new List<JpegSegment>(), 5);
            Assert.AreEqual(0, reader.Position);
            Assert.IsTrue(reader.IsEnd);
            reader.ReadByte();
        }

        [TestMethod]
        public void ReadByte_PastSegmentHeader()
        {
            var reader = new JpegSegmentByteReader(new List<JpegSegment>
            {
                new JpegSegment(JpegSegmentType.App1, new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00, // segment header
                    0x12, 0x34
                }, 0)
            }, 5);
            var value = reader.ReadByte();
            Assert.AreEqual(0x12, value);
            Assert.AreEqual(6, reader.Position);
            value = reader.ReadByte();
            Assert.AreEqual(0x34, value);
            Assert.AreEqual(7, reader.Position);
            Assert.IsTrue(reader.IsEnd);
        }

        
        [TestMethod]
        public void ReadByte_TwoSegments()
        {
            var reader = new JpegSegmentByteReader(new List<JpegSegment>
            {
                new JpegSegment(JpegSegmentType.App1, new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00, // segment header
                    0x12,
                }, 0),
                new JpegSegment(JpegSegmentType.App1, new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00, // segment header
                    0x34,
                }, 6)
            }, 5);
            var value = reader.ReadByte();
            Assert.AreEqual(0x12, value);
            Assert.AreEqual(11, reader.Position);
            value = reader.ReadByte();
            Assert.AreEqual(0x34, value);
            Assert.AreEqual(12, reader.Position);
            Assert.IsTrue(reader.IsEnd);
        }

        [TestMethod]
        [ExpectedException(typeof(EndOfStreamException))]
        public void ReadByte_PastEnd()
        {
            var reader = new JpegSegmentByteReader(new List<JpegSegment>
            {
                new JpegSegment(JpegSegmentType.App1, new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00, // segment header
                    0x12,
                }, 0)
            }, 5);
            var value = reader.ReadByte();
            Assert.AreEqual(0x12, value);
            Assert.AreEqual(6, reader.Position);
            reader.ReadByte();
        }
    }
}
