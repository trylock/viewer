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
    public class AttributeReaderTest
    {
        [TestMethod]
        public void ReadNext_EmptyInput()
        {
            var input = new MemoryByteReader(new byte[0]);
            var reader = new AttributeReader(input);
            Assert.IsNull(reader.ReadNext());
            Assert.IsNull(reader.ReadNext());
        }

        [TestMethod]
        public void ReadNext_SingleIntAttribute()
        {
            var input = new MemoryByteReader(new byte[]
            {
                0x01, 0x00, // type
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, // name
                0x78, 0x56, 0x34, 0x12 // value
            });
            var reader = new AttributeReader(input);

            var attr = reader.ReadNext();
            Assert.IsInstanceOfType(attr, typeof(IntAttribute));
            Assert.AreEqual("test", attr.Name);
            Assert.AreEqual(0x12345678, ((IntAttribute)attr).Value);

            Assert.IsNull(reader.ReadNext());
        }

        [TestMethod]
        public void ReadNext_SingleDoubleAttribute()
        {
            var bytes = BitConverter.GetBytes(0.32);
            var input = new MemoryByteReader(new byte[]
            {
                0x02, 0x00, // type
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, // name
                bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5], bytes[6], bytes[7] // value
            });
            var reader = new AttributeReader(input);

            var attr = reader.ReadNext();
            Assert.IsInstanceOfType(attr, typeof(DoubleAttribute));
            Assert.AreEqual("test", attr.Name);
            Assert.AreEqual(0.32, ((DoubleAttribute)attr).Value);

            Assert.IsNull(reader.ReadNext());
        }

        [TestMethod]
        public void ReadNext_SingleDateTimeAttribute()
        {
            var valueDateTime = new DateTime(2018, 2, 11, 17, 50, 0);
            var valueString = valueDateTime.ToString(AttributeReader.DateTimeFormat);
            var valueBytes = Encoding.UTF8.GetBytes(valueString);
            var headerBytes = new byte[]
            {
                0x04, 0x00, // type
                (byte) 't', (byte) 'e', (byte) 's', (byte) 't', 0x00, // name
            };
            var inputMemoryStream = new MemoryStream();
            inputMemoryStream.Write(headerBytes, 0, headerBytes.Length);
            inputMemoryStream.Write(valueBytes, 0, valueBytes.Length);
            inputMemoryStream.WriteByte(0);
            inputMemoryStream.Position = 0;
            var input = new MemoryByteReader(inputMemoryStream.ToArray());
            var reader = new AttributeReader(input);

            var attr = reader.ReadNext();
            Assert.IsInstanceOfType(attr, typeof(DateTimeAttribute));
            Assert.AreEqual("test", attr.Name);
            Assert.AreEqual(valueDateTime, ((DateTimeAttribute)attr).Value);

            Assert.IsNull(reader.ReadNext());
        }

        [TestMethod]
        public void ReadNext_TwoAttributes()
        {
            var input = new MemoryByteReader(new byte[]
            {
                0x01, 0x00, // type int
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, // name
                0x78, 0x56, 0x34, 0x12, // value

                0x03, 0x00, // type string
                (byte)'t', (byte)'m', (byte)'p', 0x00, // name
                (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0x00 // value
            });
            var reader = new AttributeReader(input);

            var attr = reader.ReadNext();
            Assert.IsInstanceOfType(attr, typeof(IntAttribute));
            Assert.AreEqual("test", attr.Name);
            Assert.AreEqual(0x12345678, ((IntAttribute)attr).Value);

            attr = reader.ReadNext();
            Assert.IsInstanceOfType(attr, typeof(StringAttribute));
            Assert.AreEqual("tmp", attr.Name);
            Assert.AreEqual("value", ((StringAttribute)attr).Value);

            Assert.IsNull(reader.ReadNext());
        }

        [TestMethod]
        public void ReadNext_UnicodeCharacters()
        {
            var input = new MemoryByteReader(new byte[]
            {
                0x01, 0x00, // type
                0xC4, 0x9B, 0xC5, 0xA1, 0xC4, 0x8D, 0xC5, 0x99, 0xC5, 0xBE, 0xC3, 0xBD, 0xC3, 0xA1, 0xC3, 0xAD, 0xC3, 0xA9, 0x00, // name
                0x78, 0x56, 0x34, 0x12 // value
            });
            var reader = new AttributeReader(input);

            var attr = reader.ReadNext();
            Assert.IsInstanceOfType(attr, typeof(IntAttribute));
            Assert.AreEqual("ěščřžýáíé", attr.Name);
            Assert.AreEqual(0x12345678, ((IntAttribute)attr).Value);

            Assert.IsNull(reader.ReadNext());
        }

        [TestMethod]
        public void ReadNext_4ByteUnicodeCharacter()
        {
            var input = new MemoryByteReader(new byte[]
            {
                0x01, 0x00, // type
                0xF0, 0x90, 0x8D, 0x88, 0x00, // name
                0x78, 0x56, 0x34, 0x12 // value
            });
            var reader = new AttributeReader(input);

            var attr = reader.ReadNext();
            Assert.IsInstanceOfType(attr, typeof(IntAttribute));
            Assert.AreEqual("𐍈", attr.Name);
            Assert.AreEqual(0x12345678, ((IntAttribute)attr).Value);

            Assert.IsNull(reader.ReadNext());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void ReadNext_UnexpectedEndInType()
        {
            var input = new MemoryByteReader(new byte[]
            {
                0x01,
            });
            var reader = new AttributeReader(input);
            reader.ReadNext();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void ReadNext_UnexpectedEndInName()
        {
            var input = new MemoryByteReader(new byte[]
            {
                0x01, 0x00, // type
                (byte)'n'
            });
            var reader = new AttributeReader(input);
            reader.ReadNext();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void ReadNext_InvalidType()
        {
            var input = new MemoryByteReader(new byte[]
            {
                0x00, 0x01, // type
                (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00, // name
                0x78, 0x56, 0x34, 0x12 // value
            });
            var reader = new AttributeReader(input);
            reader.ReadNext();
        }
    }
}
