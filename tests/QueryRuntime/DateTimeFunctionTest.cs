using System;
using System.Collections.Generic;
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
                new StringValue("2018-08-02 21:28"),
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
    }
}
