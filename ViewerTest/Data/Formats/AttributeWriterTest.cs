using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.Formats;

namespace ViewerTest.Data.Formats
{
    [TestClass]
    public class AttributeWriterTest
    {
        [TestMethod]
        public void Write_IntAttriubte()
        {
            var output = new MemoryStream();
            var writer = new BinaryWriter(output);
            var attrWriter = new AttributeWriter(writer);
            attrWriter.Write(new IntAttribute("test", AttributeSource.Custom, 0x12345678));

            var data = output.ToArray();
            Assert.AreEqual(11, data.Length);

            // type
            Assert.AreEqual(0x01, data[0]);
            Assert.AreEqual(0x00, data[1]);

            // name
            Assert.AreEqual((byte)'t', data[2]);
            Assert.AreEqual((byte)'e', data[3]);
            Assert.AreEqual((byte)'s', data[4]);
            Assert.AreEqual((byte)'t', data[5]);
            Assert.AreEqual(0, data[6]);

            // value
            Assert.AreEqual(0x78, data[7]);
            Assert.AreEqual(0x56, data[8]);
            Assert.AreEqual(0x34, data[9]);
            Assert.AreEqual(0x12, data[10]);
        }

        [TestMethod]
        public void Write_DoubleAttriubte()
        {
            var output = new MemoryStream();
            var writer = new BinaryWriter(output);
            var attrWriter = new AttributeWriter(writer);
            attrWriter.Write(new DoubleAttribute("test", AttributeSource.Custom, 0.64));

            var data = output.ToArray();
            Assert.AreEqual(15, data.Length);

            // type
            Assert.AreEqual(0x02, data[0]);
            Assert.AreEqual(0x00, data[1]);

            // name
            Assert.AreEqual((byte)'t', data[2]);
            Assert.AreEqual((byte)'e', data[3]);
            Assert.AreEqual((byte)'s', data[4]);
            Assert.AreEqual((byte)'t', data[5]);
            Assert.AreEqual(0, data[6]);

            // value
            var bytes = BitConverter.GetBytes(0.64);
            for (int i = 0; i < 8; ++i)
            {
                Assert.AreEqual(bytes[i], data[7 + i]);
            }
        }

        [TestMethod]
        public void Write_StringAttriubte()
        {
            var output = new MemoryStream();
            var writer = new BinaryWriter(output);
            var attrWriter = new AttributeWriter(writer);
            attrWriter.Write(new StringAttribute("test", AttributeSource.Custom, "value"));

            var data = output.ToArray();
            Assert.AreEqual(13, data.Length);

            // type
            Assert.AreEqual(0x03, data[0]);
            Assert.AreEqual(0x00, data[1]);

            // name
            Assert.AreEqual((byte)'t', data[2]);
            Assert.AreEqual((byte)'e', data[3]);
            Assert.AreEqual((byte)'s', data[4]);
            Assert.AreEqual((byte)'t', data[5]);
            Assert.AreEqual(0, data[6]);

            // value
            Assert.AreEqual((byte)'v', data[7]);
            Assert.AreEqual((byte)'a', data[8]);
            Assert.AreEqual((byte)'l', data[9]);
            Assert.AreEqual((byte)'u', data[10]);
            Assert.AreEqual((byte)'e', data[11]);
            Assert.AreEqual((byte)0, data[12]);
        }

        [TestMethod]
        public void Write_DateTimeAttriubte()
        {
            var output = new MemoryStream();
            var writer = new BinaryWriter(output);
            var attrWriter = new AttributeWriter(writer);
            var attr = new DateTimeAttribute("test", AttributeSource.Custom, new DateTime(2018, 2, 11, 21, 20, 30));
            attrWriter.Write(attr);

            var data = output.ToArray();
            Assert.AreEqual(36, data.Length);

            // type
            Assert.AreEqual(0x03, data[0]);
            Assert.AreEqual(0x00, data[1]);

            // name
            Assert.AreEqual((byte)'t', data[2]);
            Assert.AreEqual((byte)'e', data[3]);
            Assert.AreEqual((byte)'s', data[4]);
            Assert.AreEqual((byte)'t', data[5]);
            Assert.AreEqual(0, data[6]);

            // value
            var value = attr.Value.ToString(AttributeReader.DateTimeFormat);
            for (int i = 0; i < value.Length; ++i)
            {
                Assert.AreEqual((byte)value[i], data[7 + i]);
            }
            Assert.AreEqual(0, data[data.Length - 1]);
        }
    }
}
