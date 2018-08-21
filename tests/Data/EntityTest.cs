using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data
{
    [TestClass]
    public class EntityTest
    {
        [TestMethod]
        public void SetAttribute_NewAttribute()
        {
            IEntity attrs = new FileEntity("test");
            Assert.IsNull(attrs.GetAttribute("test"));
            Assert.AreEqual(0, attrs.Count);

            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42), AttributeSource.Custom));
            Assert.IsNotNull(attrs.GetAttribute("test"));
            Assert.AreEqual(1, attrs.Count);
            Assert.AreEqual(42, attrs.GetValue<IntValue>("test").Value);
        }

        [TestMethod]
        public void SetAttribute_ExistingAttribute()
        {
            IEntity attrs = new FileEntity("test");

            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(24), AttributeSource.Custom));
            Assert.IsNotNull(attrs.GetAttribute("test"));
            Assert.AreEqual(1, attrs.Count);

            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42), AttributeSource.Custom));
            Assert.IsNotNull(attrs.GetAttribute("test"));
            Assert.AreEqual(1, attrs.Count);
            Assert.AreEqual(42, attrs.GetValue<IntValue>("test").Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetAttribute_NullAttributeName()
        {
            var attrs = new FileEntity("test");
            attrs.SetAttribute(new Attribute(null, new IntValue(42), AttributeSource.Custom));
        }

        [TestMethod]
        public void GetAttribute_NonExitentAttribute()
        {
            var attrs = new FileEntity("test");
            Assert.IsNull(attrs.GetAttribute("test"));
            Assert.IsNull(attrs.GetAttribute(""));
        }

        [TestMethod]
        public void GetAttribute_IntAttribute()
        {
            IEntity attrs = new FileEntity("test");
            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42), AttributeSource.Custom));

            var attr = attrs.GetValue<IntValue>("test").Value;
            Assert.AreEqual(42, attr.Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAttribute_NullAttributeName()
        {
            var attrs = new FileEntity("test");
            attrs.GetAttribute(null);
        }

        [TestMethod]
        public void Remove_NonExitentKey()
        {
            IEntity attrs = new FileEntity("test");
            var newAttrs = attrs.RemoveAttribute("test");
            Assert.AreEqual(newAttrs, attrs);
        }

        [TestMethod]
        public void Remove_OneKey()
        {
            IEntity attrs = new FileEntity("test");
            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42), AttributeSource.Custom));
            Assert.IsNotNull(attrs.GetAttribute("test"));

            attrs = attrs.RemoveAttribute("test");
            Assert.IsNull(attrs.GetAttribute("test"));
        }

        [TestMethod]
        public void Indexer_SetValue()
        {
            IEntity attrs = new FileEntity("test");
            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42), AttributeSource.Custom));

            attrs = attrs.SetAttribute(new Attribute("test", new StringValue("value"), AttributeSource.Custom));
            Assert.AreEqual("value", attrs.GetValue<StringValue>("test").Value);
        }
    }
}
