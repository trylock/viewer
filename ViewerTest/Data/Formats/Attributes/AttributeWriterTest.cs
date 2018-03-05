using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.Data.Formats.Attributes;

namespace ViewerTest.Data.Formats.Attributes
{
    [TestClass]
    public class AttributeWriterTest
    {
        [TestMethod]
        public void Write_IntAttriubte()
        {
            var header = new byte[] { 0xBE, 0xEF };
            var attrWriter = new AttributeWriter(new JpegSegmentByteWriter(header));
            attrWriter.Write(new IntAttribute("test", AttributeSource.Custom, 0x12345678));

            var segments = attrWriter.Finish();
            Assert.AreEqual(1, segments.Count);

            var segment = segments[0];

            Assert.AreEqual(JpegSegmentType.App1, segment.Type);
            
            CollectionAssert.AreEqual(new byte[]
            {
                // segment header
                0xBE, 0xEF, 
                // type
                0x01, 0x00, 
                // name
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, 
                // value
                0x78, 0x56, 0x34, 0x12, 
            }, segment.Bytes);
        }

        [TestMethod]
        public void Write_DoubleAttriubte()
        {
            var header = new byte[] { 0xBE, 0xEF };
            var attrWriter = new AttributeWriter(new JpegSegmentByteWriter(header));
            attrWriter.Write(new DoubleAttribute("test", AttributeSource.Custom, 0.64));

            var segments = attrWriter.Finish();
            Assert.AreEqual(1, segments.Count);

            var segment = segments[0];

            Assert.AreEqual(JpegSegmentType.App1, segment.Type);
            
            var bytes = BitConverter.GetBytes(0.64);
            CollectionAssert.AreEqual(new byte[]
            {
                // segment header
                0xBE, 0xEF, 
                // type
                0x02, 0x00, 
                // name
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, 
                // value
                bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5], bytes[6], bytes[7]
            }, segment.Bytes);
        }

        [TestMethod]
        public void Write_StringAttriubte()
        {
            var header = new byte[] { 0xBE, 0xEF };
            var attrWriter = new AttributeWriter(new JpegSegmentByteWriter(header));
            attrWriter.Write(new StringAttribute("test", AttributeSource.Custom, "value"));

            var segments = attrWriter.Finish();
            Assert.AreEqual(1, segments.Count);

            var segment = segments[0];
            Assert.AreEqual(JpegSegmentType.App1, segment.Type);
            
            CollectionAssert.AreEqual(new byte[]
            {
                // segment header
                0xBE, 0xEF, 
                // type
                0x03, 0x00, 
                // name
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, 
                // value
                (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0x00,
            }, segment.Bytes);
        }

        [TestMethod]
        public void Write_DateTimeAttriubte()
        {
            var header = new byte[] { 0xBE, 0xEF };
            var attrWriter = new AttributeWriter(new JpegSegmentByteWriter(header));
            var attr = new DateTimeAttribute("test", AttributeSource.Custom, new DateTime(2018, 2, 11, 21, 20, 30));
            attrWriter.Write(attr);

            var segments = attrWriter.Finish();
            Assert.AreEqual(1, segments.Count);

            var segment = segments[0];
            Assert.AreEqual(JpegSegmentType.App1, segment.Type);
            
            var expectedValue = new List<byte>
            {
                // segment header
                0xBE, 0xEF, 
                // type
                0x04, 0x00, 
                // name
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00,
            };
            var dateTimeValue = attr.Value.ToString(DateTimeAttribute.Format);
            foreach (var val in dateTimeValue)
            {
                expectedValue.Add((byte)val);
            }
            expectedValue.Add(0x00);

            CollectionAssert.AreEqual(expectedValue, segment.Bytes);
        }
    }
}
