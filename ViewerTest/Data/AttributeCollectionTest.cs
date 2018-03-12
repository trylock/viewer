using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;

namespace ViewerTest.Data
{
    [TestClass]
    public class AttributeCollectionTest
    {
        [TestMethod]
        public void SetAttribute_NewAttribute()
        {
            var attrs = new AttributeCollection("test", DateTime.Now, DateTime.Now);
            Assert.IsFalse(attrs.IsDirty);
            Assert.IsFalse(attrs.ContainsKey("test"));
            Assert.AreEqual(0, attrs.Count);

            attrs.SetAttribute(new IntAttribute("test", AttributeSource.Custom, 42));
            Assert.IsTrue(attrs.IsDirty);
            Assert.IsTrue(attrs.ContainsKey("test"));
            Assert.AreEqual(1, attrs.Count);
            Assert.AreEqual(42, ((IntAttribute)attrs["test"]).Value);
        }

        [TestMethod]
        public void SetAttribute_ExistingAttribute()
        {
            var attrs = new AttributeCollection("test", DateTime.Now, DateTime.Now);

            attrs.SetAttribute(new IntAttribute("test", AttributeSource.Custom, 24));
            Assert.IsTrue(attrs.IsDirty);
            Assert.IsTrue(attrs.ContainsKey("test"));
            Assert.AreEqual(1, attrs.Count);

            attrs.SetAttribute(new IntAttribute("test", AttributeSource.Custom, 42));
            Assert.IsTrue(attrs.IsDirty);
            Assert.IsTrue(attrs.ContainsKey("test"));
            Assert.AreEqual(1, attrs.Count);
            Assert.AreEqual(42, ((IntAttribute)attrs["test"]).Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetAttribute_NullAttributeName()
        {
            var attrs = new AttributeCollection("test", DateTime.Now, DateTime.Now);
            attrs.SetAttribute(new IntAttribute(null, AttributeSource.Custom, 42));
        }

        [TestMethod]
        public void GetAttribute_NonExitentAttribute()
        {
            var attrs = new AttributeCollection("test", DateTime.Now, DateTime.Now);
            Assert.IsNull(attrs.GetAttribute("test"));
            Assert.IsNull(attrs.GetAttribute(""));
            Assert.IsFalse(attrs.IsDirty);
        }

        [TestMethod]
        public void GetAttribute_IntAttribute()
        {
            var attrs = new AttributeCollection("test", DateTime.Now, DateTime.Now);
            attrs.SetAttribute(new IntAttribute("test", AttributeSource.Custom, 42));
            attrs.Reset();
            Assert.IsFalse(attrs.IsDirty);

            var attr = (IntAttribute) attrs.GetAttribute("test");
            Assert.IsFalse(attrs.IsDirty);
            Assert.AreEqual(42, attr.Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAttribute_NullAttributeName()
        {
            var attrs = new AttributeCollection("test", DateTime.Now, DateTime.Now);
            attrs.GetAttribute(null);
        }

        [TestMethod]
        public void Remove_NonExitentKey()
        {
            var attrs = new AttributeCollection("test", DateTime.Now, DateTime.Now);
            Assert.IsFalse(attrs.IsDirty);
            var result = attrs.Remove("test");
            Assert.IsFalse(result);
            Assert.IsFalse(attrs.IsDirty);
        }

        [TestMethod]
        public void Remove_OneKey()
        {
            var attrs = new AttributeCollection("test", DateTime.Now, DateTime.Now);
            attrs.SetAttribute(new IntAttribute("test", AttributeSource.Custom, 42));
            attrs.Reset();
            Assert.IsFalse(attrs.IsDirty);

            var result = attrs.Remove("test");
            Assert.IsTrue(result);
            Assert.IsTrue(attrs.IsDirty);
        }

        [TestMethod]
        public void Indexer_SetValue()
        {
            var attrs = new AttributeCollection("test", DateTime.Now, DateTime.Now);
            attrs.SetAttribute(new IntAttribute("test", AttributeSource.Custom, 42));
            attrs.Reset();
            Assert.IsFalse(attrs.IsDirty);

            attrs["test"] = new StringAttribute("test", AttributeSource.Custom, "value");
            Assert.IsTrue(attrs.IsDirty);
            Assert.AreEqual("value", ((StringAttribute)attrs["test"]).Value);
        }
    }
}
