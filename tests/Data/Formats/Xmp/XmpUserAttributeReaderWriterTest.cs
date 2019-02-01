using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.Formats.Xmp;
using XmpCore;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data.Formats.Xmp
{
    [TestClass]
    public class XmpUserAttributeReaderWriterTest
    {
        [TestMethod]
        public void Read_NullXmpTree()
        {
            var data = new XmpUserAttributeReader(null);
            var attributes = data.ToList();
            Assert.AreEqual(0, attributes.Count);
        }

        [TestMethod]
        public void Read_ActualXmpData()
        {
            var buffer = File.ReadAllBytes(Path.Combine(
                Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory)),
                "MiscTestData/test.xmp"));
            
            var data = new XmpUserAttributeReader(XmpMetaFactory.ParseFromBuffer(buffer));
            var attributes = data.ToList();
            Assert.AreEqual(4, attributes.Count);
            Assert.AreEqual("test", attributes[0].Name);
            Assert.AreEqual(AttributeSource.Custom, attributes[0].Source);
            Assert.AreEqual(3.14159, ((RealValue)attributes[0].Value).Value);

            Assert.AreEqual("testInt", attributes[1].Name);
            Assert.AreEqual(AttributeSource.Custom, attributes[1].Source);
            Assert.AreEqual(123456789, ((IntValue)attributes[1].Value).Value);

            Assert.AreEqual("testString", attributes[2].Name);
            Assert.AreEqual(AttributeSource.Custom, attributes[2].Source);
            Assert.AreEqual("hello world", ((StringValue)attributes[2].Value).Value);

            Assert.AreEqual("testDate", attributes[3].Name);
            Assert.AreEqual(AttributeSource.Custom, attributes[3].Source);
            Assert.AreEqual(new DateTime(2019, 1, 31, 16, 36, 0), ((DateTimeValue)attributes[3].Value).Value);
        }

        [TestMethod]
        public void Read_WrittenData()
        {
            var xmp = XmpMetaFactory.Create();
            var writer = new XmpUserAttributeWriter(xmp);
            writer.Write(new []
            {
                new Attribute("this should be replaced", new IntValue(42), AttributeSource.Custom)
            });

            writer.Write(new []
            {
                new Attribute("test", new RealValue(3.14159), AttributeSource.Custom),
                new Attribute("testInt", new IntValue(123456789), AttributeSource.Custom),
                new Attribute("testString", new StringValue("hello world"), AttributeSource.Custom),
                new Attribute("testDate", new DateTimeValue(new DateTime(2019, 1, 31, 16, 36, 0)), AttributeSource.Custom),
                new Attribute("null", new RealValue(null), AttributeSource.Custom),
            });

            var reader = new XmpUserAttributeReader(xmp);
            var attributes = reader.ToList();
            Assert.AreEqual(4, attributes.Count);
            Assert.AreEqual("test", attributes[0].Name);
            Assert.AreEqual(AttributeSource.Custom, attributes[0].Source);
            Assert.AreEqual(3.14159, ((RealValue)attributes[0].Value).Value);

            Assert.AreEqual("testInt", attributes[1].Name);
            Assert.AreEqual(AttributeSource.Custom, attributes[1].Source);
            Assert.AreEqual(123456789, ((IntValue)attributes[1].Value).Value);

            Assert.AreEqual("testString", attributes[2].Name);
            Assert.AreEqual(AttributeSource.Custom, attributes[2].Source);
            Assert.AreEqual("hello world", ((StringValue)attributes[2].Value).Value);

            Assert.AreEqual("testDate", attributes[3].Name);
            Assert.AreEqual(AttributeSource.Custom, attributes[3].Source);
            Assert.AreEqual(new DateTime(2019, 1, 31, 16, 36, 0), ((DateTimeValue)attributes[3].Value).Value);

        }
    }
}