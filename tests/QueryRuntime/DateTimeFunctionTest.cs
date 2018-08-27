using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Query;
using Viewer.QueryRuntime;

namespace ViewerTest.QueryRuntime
{
    [TestClass]
    public class DateTimeFunctionTest
    {
        [TestMethod]
        public void Call_NullArgument()
        {
            var function = new DateTimeFunction();
            var result = function.Call(new ArgumentList(new BaseValue[]
            {
                new StringValue(null), 
            }));

            Assert.IsTrue(result.Type == TypeId.DateTime);
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void Call_Now()
        {
            var function = new DateTimeFunction();
            var result = function.Call(new ArgumentList(new BaseValue[]
            {
                new StringValue("nOW"),
            }));

            var now = DateTime.Now;
            var accaptableTimeSpan = new TimeSpan(0, 0, 1, 0);

            Assert.IsTrue(result.Type == TypeId.DateTime);
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue((result as DateTimeValue).Value <= now);
            Assert.IsTrue((result as DateTimeValue).Value >= now - accaptableTimeSpan);
        }

        [TestMethod]
        public void Call_DateTime()
        {
            var function = new DateTimeFunction();
            var result = function.Call(new ArgumentList(new BaseValue[]
            {
                new StringValue("2018-8-2 21:28:00"),
            }));
            
            Assert.IsTrue(result.Type == TypeId.DateTime);
            Assert.IsFalse(result.IsNull);

            var dateTime = (result as DateTimeValue).Value.Value;
            Assert.AreEqual(2018, dateTime.Year);
            Assert.AreEqual(8, dateTime.Month);
            Assert.AreEqual(2, dateTime.Day);
            Assert.AreEqual(21, dateTime.Hour);
            Assert.AreEqual(28, dateTime.Minute);
            Assert.AreEqual(0, dateTime.Second);
        }

        [TestMethod]
        public void Call_LeadingZeroesAreOptional()
        {
            var function = new DateTimeFunction();
            for (var i = 0; i < 32; ++i)
            {
                var stringDate = "2018-" +
                                 ((i & 0x1) != 0 ? "1-" : "01-") +
                                 ((i & 0x2) != 0 ? "2 " : "02 ") +
                                 ((i & 0x4) != 0 ? "3:" : "03:") +
                                 ((i & 0x8) != 0 ? "4:" : "04:") +
                                 ((i & 0x10) != 0 ? "5" : "05");
                var result = function.Call(new ArgumentList(new BaseValue[]
                {
                    new StringValue(stringDate)
                }));

                Assert.IsTrue(result.Type == TypeId.DateTime);
                Assert.IsFalse(result.IsNull);

                var dateTime = (result as DateTimeValue).Value.Value;
                Assert.AreEqual(2018, dateTime.Year);
                Assert.AreEqual(1, dateTime.Month);
                Assert.AreEqual(2, dateTime.Day);
                Assert.AreEqual(3, dateTime.Hour);
                Assert.AreEqual(4, dateTime.Minute);
                Assert.AreEqual(5, dateTime.Second);
            }
        }

        [TestMethod]
        public void Call_TimeIsOptional()
        {
            var function = new DateTimeFunction();
            var result = function.Call(new ArgumentList(new BaseValue[]
            {
                new StringValue("2019-1-26"), 
            }));

            Assert.IsTrue(result.Type == TypeId.DateTime);
            Assert.IsFalse(result.IsNull);

            var dateTime = (result as DateTimeValue).Value.Value;
            Assert.AreEqual(2019, dateTime.Year);
            Assert.AreEqual(1, dateTime.Month);
            Assert.AreEqual(26, dateTime.Day);
            Assert.AreEqual(0, dateTime.Hour);
            Assert.AreEqual(0, dateTime.Minute);
            Assert.AreEqual(0, dateTime.Second);
        }

        [TestMethod]
        public void Call_DateIsAlias()
        {
            var function = new DateFunction();
            var result = function.Call(new ArgumentList(new BaseValue[]
            {
                new StringValue("2018-8-2 21:28:00"),
            }));

            Assert.IsTrue(result.Type == TypeId.DateTime);
            Assert.IsFalse(result.IsNull);

            var dateTime = (result as DateTimeValue).Value.Value;
            Assert.AreEqual(2018, dateTime.Year);
            Assert.AreEqual(8, dateTime.Month);
            Assert.AreEqual(2, dateTime.Day);
            Assert.AreEqual(21, dateTime.Hour);
            Assert.AreEqual(28, dateTime.Minute);
            Assert.AreEqual(0, dateTime.Second);
        }

        [TestMethod]
        public void Call_DateTimeIdentityReturnsItsArgument()
        {
            var value = new DateTimeValue(DateTime.Now);
            var function = new DateTimeIdentityFunciton();
            var result = function.Call(new ArgumentList(new BaseValue[]
            {
                value
            }));
            Assert.AreEqual(result, value);
        }
    }
}
