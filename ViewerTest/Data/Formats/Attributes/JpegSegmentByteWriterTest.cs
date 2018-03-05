using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data.Formats.Attributes;

namespace ViewerTest.Data.Formats.Attributes
{
    [TestClass]
    public class JpegSegmentByteWriterTest
    {
        [TestMethod]
        public void ToSegment_NoData()
        {
            var header = new byte[] { 0xAB, 0xCD };
            var writer = new JpegSegmentByteWriter(header, 5);
            var segments = writer.ToSegments();
            Assert.AreEqual(0, segments.Count);
        }

        [TestMethod]
        public void ToSegment_UnfinishedSegment()
        {
            var header = new byte[] { 0xAB, 0xCD };
            var writer = new JpegSegmentByteWriter(header, 5);
            writer.WriteByte(0xEE);
            writer.WriteByte(0xFF);

            var segments = writer.ToSegments();
            Assert.AreEqual(1, segments.Count);
            CollectionAssert.AreEqual(new byte[]{ 0xAB, 0xCD, 0xEE, 0xFF }, segments[0].Bytes);
        }

        [TestMethod]
        public void ToSegment_2Segments()
        {
            var header = new byte[] { 0xAB, 0xCD };
            var writer = new JpegSegmentByteWriter(header, 4);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xDD);
            writer.WriteByte(0xEE);

            var segments = writer.ToSegments();
            Assert.AreEqual(2, segments.Count);
            CollectionAssert.AreEqual(new byte[]
            {
                0xAB, 0xCD, 0xCC, 0xDD,
            }, segments[0].Bytes);

            CollectionAssert.AreEqual(new byte[]
            {
                0xAB, 0xCD, 0xEE
            }, segments[1].Bytes);
        }
    }
}
