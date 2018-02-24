using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data.Formats;
using Viewer.Data.Formats.Jpeg;

namespace ViewerTest.Data.Formats
{
    [TestClass]
    public class JpegSegmentWriterTest
    {
        [TestMethod]
        public void WriteSegment_SegmentWithoutData()
        {
            var output = new MemoryStream();
            var writer = new JpegSegmentWriter(new BinaryWriter(output));

            var segment = new JpegSegment(JpegSegmentType.Soi, new byte[0], 0);
            writer.WriteSegment(segment);

            var result = output.ToArray();
            CollectionAssert.AreEqual(
                new byte[]{ 0xFF, 0xD8 }, 
                result);
        }

        [TestMethod]
        public void WriteSegment_SegmentWithData()
        {
            var output = new MemoryStream();
            var writer = new JpegSegmentWriter(new BinaryWriter(output));

            var segment = new JpegSegment(JpegSegmentType.App1, new byte[]{ 0x12, 0x34, 0x56 }, 0);
            writer.WriteSegment(segment);

            var result = output.ToArray();
            CollectionAssert.AreEqual(
                new byte[] { 0xFF, 0xE1, 0x00, 0x05, 0x12, 0x34, 0x56 },
                result);
        }

        [TestMethod]
        public void Finish_CopyDataIn2Blocks()
        {
            var output = new MemoryStream();
            var writer = new JpegSegmentWriter(new BinaryWriter(output), 3);

            var data = new MemoryStream(new byte[]
            {
                0x12, 0x34, 0x56, 0x78, 
            });

            writer.Finish(data);

            var result = output.ToArray();
            CollectionAssert.AreEqual(
                new byte[]{
                    0xFF, 0xDA, // start of scan
                    0x12, 0x34, 0x56, 0x78,
                },
                result);
        }
    }
}
