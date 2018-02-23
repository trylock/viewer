using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;

namespace ViewerTest.Data
{
    [TestClass]
    public class MemoryByteReaderTest
    {
        [TestMethod]
        [ExpectedException(typeof(EndOfStreamException))]
        public void ReadByte_EmptyInput()
        {
            var data = new MemoryByteReader(new byte[0]);
            data.ReadByte();
        }

        [TestMethod]
        public void ReadByte_OneByte()
        {
            var data = new MemoryByteReader(new byte[] { 0x12 });
            var value = data.ReadByte();
            Assert.AreEqual(0x12, value);
            Assert.IsTrue(data.IsEnd);
        }

        [TestMethod]
        public void ReadInt16_LE()
        {
            var data = new MemoryByteReader(new byte[] { 0x34, 0x12 });
            var value = data.ReadInt16();
            Assert.AreEqual(0x1234, value);
            Assert.IsTrue(data.IsEnd);
        }

        [TestMethod]
        public void ReadInt32_LE()
        {
            var data = new MemoryByteReader(new byte[] { 0x78, 0x56, 0x34, 0x12 });
            var value = data.ReadInt32();
            Assert.AreEqual(0x12345678, value);
            Assert.IsTrue(data.IsEnd);
        }

        [TestMethod]
        public void ReadDouble_One()
        {
            var data = new MemoryByteReader(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F
            });
            var value = data.ReadDouble();
            Assert.AreEqual(1.0, value);
            Assert.IsTrue(data.IsEnd);
        }

        [TestMethod]
        public void ReadDouble_NegativeInfinity()
        {
            var data = new MemoryByteReader(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0xFF
            });
            var value = data.ReadDouble();
            Assert.AreEqual(double.NegativeInfinity, value);
            Assert.IsTrue(data.IsEnd);
        }
    }
}
