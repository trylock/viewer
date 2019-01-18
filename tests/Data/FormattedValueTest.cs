using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;

namespace ViewerTest.Data
{
    [TestClass]
    public class FormattedValueTest
    {
        [TestMethod]
        public void CompareTo_ActualValue()
        {
            var proxy = new FormattedStringValue(new StringValue("test"), new Mock<IValueFormatter<StringValue>>().Object);
            var value = new StringValue("test");
            Assert.IsTrue(value.Equals(proxy));
            Assert.IsTrue(proxy.Equals(value));
            Assert.AreEqual(0, value.CompareTo(proxy));
            Assert.AreEqual(0, proxy.CompareTo(value));
        }
    }
}
