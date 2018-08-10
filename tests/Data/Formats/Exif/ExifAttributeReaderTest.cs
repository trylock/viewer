using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.Formats.Exif;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data.Formats.Exif
{
    internal class AttributeParserMock : IExifAttributeParser
    {
        private Attribute _attr;

        public AttributeParserMock(Attribute attr)
        {
            _attr = attr;
        }

        public Attribute Parse(IExifMetadata exif)
        {
            return _attr;
        }
    }

    [TestClass]
    public class ExifAttributeReaderTest
    {
        [TestMethod]
        public void ReadNext_EmptyTagList()
        {
            var reader = new ExifAttributeReader(null, new List<IExifAttributeParser>());
            Assert.IsNull(reader.Read());
            Assert.IsNull(reader.Read());
        }

        [TestMethod]
        public void ReadNext_OneTag()
        {
            var reader = new ExifAttributeReader(null, new List<IExifAttributeParser>
            {
                new AttributeParserMock(new Attribute("test", new IntValue(1))),
            });

            var attr = reader.Read();
            Assert.AreEqual("test", attr.Name);
            Assert.AreEqual(AttributeFlags.None, attr.Flags);
            Assert.AreEqual(1, ((IntValue)attr.Value).Value);

            Assert.IsNull(reader.Read());
        }

        [TestMethod]
        public void ReadNext_NullAttributeInTheMiddle()
        {
            var reader = new ExifAttributeReader(null, new List<IExifAttributeParser>
            {
                new AttributeParserMock(new Attribute("test1", new IntValue(1))),
                new AttributeParserMock(null),
                new AttributeParserMock(new Attribute("test3", new IntValue(3))),
            });

            var attr = reader.Read();
            Assert.AreEqual("test1", attr.Name);
            Assert.AreEqual(AttributeFlags.None, attr.Flags);
            Assert.AreEqual(1, ((IntValue)attr.Value).Value);

            // it will skip the null attribute
            attr = reader.Read();
            Assert.AreEqual("test3", attr.Name);
            Assert.AreEqual(AttributeFlags.None, attr.Flags);
            Assert.AreEqual(3, ((IntValue)attr.Value).Value);

            Assert.IsNull(reader.Read());
        }
    }
}
