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
            var output = new MemoryStream();
            var attrWriter = new AttributeWriter(new BinaryWriter(output));
            attrWriter.Write(new IntAttribute("test", AttributeSource.Custom, 0x12345678));

            var actualData = output.ToArray();
            CollectionAssert.AreEqual(new byte[]
            {
                // type
                0x01, 0x00, 
                // name
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, 
                // value
                0x78, 0x56, 0x34, 0x12, 
            }, actualData);
        }

        [TestMethod]
        public void Write_DoubleAttriubte()
        {
            const double value = 0.64;

            var output = new MemoryStream();
            var attrWriter = new AttributeWriter(new BinaryWriter(output));
            attrWriter.Write(new DoubleAttribute("test", AttributeSource.Custom, value));

            var bytes = BitConverter.GetBytes(value);
            var actualData = output.ToArray();
            CollectionAssert.AreEqual(new byte[]
            {
                // type
                0x02, 0x00, 
                // name
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, 
                // value
                bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5], bytes[6], bytes[7]
            }, actualData);
        }

        [TestMethod]
        public void Write_StringAttriubte()
        {
            var output = new MemoryStream();
            var attrWriter = new AttributeWriter(new BinaryWriter(output));
            attrWriter.Write(new StringAttribute("test", AttributeSource.Custom, "value"));

            var actualData = output.ToArray();
            CollectionAssert.AreEqual(new byte[]
            {
                // type
                0x03, 0x00, 
                // name
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, 
                // value
                (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0x00,
            }, actualData);
        }

        [TestMethod]
        public void Write_DateTimeAttriubte()
        {
            var value = new DateTime(2018, 2, 11, 21, 20, 30);

            var output = new MemoryStream();
            var attrWriter = new AttributeWriter(new BinaryWriter(output));
            attrWriter.Write(new DateTimeAttribute("test", AttributeSource.Custom, value));

            var actualData = output.ToArray();
            var expectedValue = new List<byte>
            {
                // type
                0x04, 0x00, 
                // name
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00,
            };
            var dateTimeValue = value.ToString(DateTimeAttribute.Format);
            foreach (var val in dateTimeValue)
            {
                expectedValue.Add((byte)val);
            }
            expectedValue.Add(0x00);

            CollectionAssert.AreEqual(expectedValue, actualData);
        }

        private class StreamMock : Stream
        {
            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override bool CanRead { get; }
            public override bool CanSeek { get; }
            public override bool CanWrite => true;
            public override long Length { get; }
            public override long Position { get; set; }
            public bool IsDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                IsDisposed = true;
            }
        }
        
        [TestMethod]
        public void Dispose_WriterIsDisposed()
        {
            var stream = new StreamMock();
            var writer = new AttributeWriter(new BinaryWriter(stream));
            Assert.IsFalse(stream.IsDisposed);
            writer.Dispose();
            Assert.IsTrue(stream.IsDisposed);
        }
    }
}
