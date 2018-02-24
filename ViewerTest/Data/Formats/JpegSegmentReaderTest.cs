using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data.Formats;
using JpegSegmentReader = Viewer.Data.Formats.Jpeg.JpegSegmentReader;

namespace ViewerTest.Data.Formats
{
    [TestClass]
    public class JpegSegmentReaderTest
    {
        [TestMethod]
        public void ReadNext_EmptyFile()
        {
            var input = new BinaryReader(new MemoryStream(new byte[0]));
            var reader = new JpegSegmentReader(input);
            Assert.IsNull(reader.ReadNext());
            Assert.IsNull(reader.ReadNext());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void ReadNext_DetectInvalidSegmentHeader()
        {
            var input = new BinaryReader(new MemoryStream(new byte[] { 0xCC }));
            var reader = new JpegSegmentReader(input);

            reader.ReadNext();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void ReadNext_DetectInvalidHeaderTypeAtTheBeginning()
        {
            var input = new BinaryReader(new MemoryStream(new byte[] { 0xFF, 0x00 }));
            var reader = new JpegSegmentReader(input);

            reader.ReadNext();
        }

        [TestMethod]
        public void ReadNext_SegmentWithoutData()
        {
            var input = new BinaryReader(new MemoryStream(new byte[]
            {
                0xFF, 0xD8, // start of input
                0xFF, 0xDA, // start of scan
                0xAB, 0xCD, 0xEF // ... image data
            }));
            var reader = new JpegSegmentReader(input);

            var segment = reader.ReadNext();
            Assert.AreEqual(JpegSegmentType.Soi, segment.Type);
            Assert.AreEqual(2, segment.Offset);
            Assert.AreEqual(0, segment.Bytes.Length);

            segment = reader.ReadNext();
            Assert.AreEqual(JpegSegmentType.Sos, segment.Type);
            Assert.AreEqual(4, segment.Offset);
            Assert.AreEqual(0, segment.Bytes.Length);

            segment = reader.ReadNext();
            Assert.IsNull(segment);
        }

        [TestMethod]
        public void ReadNext_SegmentWithData()
        {
            var input = new BinaryReader(new MemoryStream(new byte[]
            {
                0xFF, 0xD8, // start of input
                0xFF, 0xE1, 0x00, 0x06, 0x12, 0x34, 0x56, 0x78, // app1 block
                0xFF, 0xDA, // start of scan
                0xAB, 0xCD, 0xEF // ... image data
            }));
            var reader = new JpegSegmentReader(input);

            var segment = reader.ReadNext();
            Assert.AreEqual(JpegSegmentType.Soi, segment.Type);
            Assert.AreEqual(2, segment.Offset);
            Assert.AreEqual(0, segment.Bytes.Length);

            segment = reader.ReadNext();
            Assert.AreEqual(JpegSegmentType.App1, segment.Type);
            Assert.AreEqual(6, segment.Offset);
            Assert.AreEqual(4, segment.Bytes.Length);
            Assert.AreEqual(0x12, segment.Bytes[0]);
            Assert.AreEqual(0x34, segment.Bytes[1]);
            Assert.AreEqual(0x56, segment.Bytes[2]);
            Assert.AreEqual(0x78, segment.Bytes[3]);
            
            segment = reader.ReadNext();
            Assert.AreEqual(JpegSegmentType.Sos, segment.Type);
            Assert.AreEqual(12, segment.Offset);
            Assert.AreEqual(0, segment.Bytes.Length);

            segment = reader.ReadNext();
            Assert.IsNull(segment);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void ReadNext_UnexpectedEndOfInputInSegmentHeader()
        {
            var input = new BinaryReader(new MemoryStream(new byte[]
            {
                0xFF, 0xD8, // start of input
                0xFF, 
            }));
            var reader = new JpegSegmentReader(input);
            reader.ReadNext(); // start of input
            reader.ReadNext();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void ReadNext_UnexpectedEndOfInputInSegmentSize()
        {
            var input = new BinaryReader(new MemoryStream(new byte[]
            {
                0xFF, 0xD8, // start of input
                0xFF, 0xE1, 0x12,
            }));
            var reader = new JpegSegmentReader(input);
            reader.ReadNext(); // start of input
            reader.ReadNext();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void ReadNext_UnexpectedEndOfInputInSegmentData()
        {
            var input = new BinaryReader(new MemoryStream(new byte[]
            {
                0xFF, 0xD8, // start of input
                0xFF, 0xE1, 0x00, 0x03,
            }));
            var reader = new JpegSegmentReader(input);
            reader.ReadNext(); // start of input
            reader.ReadNext();
        }
    }
}
