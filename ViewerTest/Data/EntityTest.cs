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
            IEntity attrs = new Entity("test", DateTime.Now, DateTime.Now);
            Assert.IsNull(attrs.GetAttribute("test"));
            Assert.AreEqual(0, attrs.Count);

            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42)));
            Assert.IsNotNull(attrs.GetAttribute("test"));
            Assert.AreEqual(1, attrs.Count);
            Assert.AreEqual(42, ((IntValue)attrs.GetAttribute("test").Value).Value);
        }

        [TestMethod]
        public void SetAttribute_ExistingAttribute()
        {
            IEntity attrs = new Entity("test", DateTime.Now, DateTime.Now);

            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(24)));
            Assert.IsNotNull(attrs.GetAttribute("test"));
            Assert.AreEqual(1, attrs.Count);

            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42)));
            Assert.IsNotNull(attrs.GetAttribute("test"));
            Assert.AreEqual(1, attrs.Count);
            Assert.AreEqual(42, ((IntValue)attrs.GetAttribute("test").Value).Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetAttribute_NullAttributeName()
        {
            var attrs = new Entity("test", DateTime.Now, DateTime.Now);
            attrs.SetAttribute(new Attribute(null, new IntValue(42)));
        }

        [TestMethod]
        public void GetAttribute_NonExitentAttribute()
        {
            var attrs = new Entity("test", DateTime.Now, DateTime.Now);
            Assert.IsNull(attrs.GetAttribute("test"));
            Assert.IsNull(attrs.GetAttribute(""));
        }

        [TestMethod]
        public void GetAttribute_IntAttribute()
        {
            IEntity attrs = new Entity("test", DateTime.Now, DateTime.Now);
            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42)));

            var attr = (IntValue) attrs.GetAttribute("test").Value;
            Assert.AreEqual(42, attr.Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAttribute_NullAttributeName()
        {
            var attrs = new Entity("test", DateTime.Now, DateTime.Now);
            attrs.GetAttribute(null);
        }

        [TestMethod]
        public void Remove_NonExitentKey()
        {
            IEntity attrs = new Entity("test", DateTime.Now, DateTime.Now);
            var newAttrs = attrs.RemoveAttribute("test");
            Assert.AreEqual(newAttrs, attrs);
        }

        [TestMethod]
        public void Remove_OneKey()
        {
            IEntity attrs = new Entity("test", DateTime.Now, DateTime.Now);
            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42)));
            Assert.IsNotNull(attrs.GetAttribute("test"));

            attrs = attrs.RemoveAttribute("test");
            Assert.IsNull(attrs.GetAttribute("test"));
        }

        [TestMethod]
        public void Indexer_SetValue()
        {
            IEntity attrs = new Entity("test", DateTime.Now, DateTime.Now);
            attrs = attrs.SetAttribute(new Attribute("test", new IntValue(42)));

            attrs = attrs.SetAttribute(new Attribute("test", new StringValue("value")));
            Assert.AreEqual("value", ((StringValue)attrs.GetAttribute("test").Value).Value);
        }
    }
}
