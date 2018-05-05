using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Query;

namespace ViewerTest.Query
{
    [TestClass]
    public class ValueConverterTest
    {
        private ValueConvertor _converter;

        [TestInitialize]
        public void Setup()
        {
            _converter = new ValueConvertor();
        }

        [TestMethod]
        public void ConvertTo_FromNullValueToIntValue()
        {
            var value = _converter.ConvertTo(new IntValue(null), TypeId.Integer);
            Assert.IsNull((value as IntValue).Value);

            value = _converter.ConvertTo(new RealValue(null), TypeId.Integer);
            Assert.IsNull((value as IntValue).Value);

            value = _converter.ConvertTo(new StringValue(null), TypeId.Integer);
            Assert.IsNull((value as IntValue).Value);

            value = _converter.ConvertTo(new DateTimeValue(null), TypeId.Integer);
            Assert.IsNull((value as IntValue).Value);
        }

        [TestMethod]
        public void ConvertTo_FromNullValueToRealValue()
        {
            var value = _converter.ConvertTo(new IntValue(null), TypeId.Real);
            Assert.IsNull((value as RealValue).Value);

            value = _converter.ConvertTo(new RealValue(null), TypeId.Real);
            Assert.IsNull((value as RealValue).Value);

            value = _converter.ConvertTo(new StringValue(null), TypeId.Real);
            Assert.IsNull((value as RealValue).Value);

            value = _converter.ConvertTo(new DateTimeValue(null), TypeId.Real);
            Assert.IsNull((value as RealValue).Value);
        }

        [TestMethod]
        public void ConvertTo_Int()
        {
            var value = _converter.ConvertTo(new IntValue(42), TypeId.Integer);
            Assert.AreEqual(42, (value as IntValue).Value);

            value = _converter.ConvertTo(new RealValue(42.0), TypeId.Integer);
            Assert.IsNull((value as IntValue).Value);

            value = _converter.ConvertTo(new StringValue("42"), TypeId.Integer);
            Assert.IsNull((value as IntValue).Value);

            value = _converter.ConvertTo(new DateTimeValue(DateTime.Today), TypeId.Integer);
            Assert.IsNull((value as IntValue).Value);
        }

        [TestMethod]
        public void ConvertTo_Real()
        {
            var value = _converter.ConvertTo(new IntValue(42), TypeId.Real);
            Assert.AreEqual(42.0, (value as RealValue).Value);

            value = _converter.ConvertTo(new RealValue(42.0), TypeId.Real);
            Assert.AreEqual(42.0, (value as RealValue).Value);

            value = _converter.ConvertTo(new StringValue("42.0"), TypeId.Real);
            Assert.IsNull((value as RealValue).Value);

            value = _converter.ConvertTo(new DateTimeValue(DateTime.Today), TypeId.Real);
            Assert.IsNull((value as RealValue).Value);
        }

        [TestMethod]
        public void ConvertTo_String()
        {
            var value = _converter.ConvertTo(new IntValue(42), TypeId.String);
            Assert.AreEqual("42", (value as StringValue).Value);

            value = _converter.ConvertTo(new RealValue(42.0), TypeId.String);
            Assert.AreEqual((42.0).ToString(), (value as StringValue).Value);

            value = _converter.ConvertTo(new StringValue("42.0"), TypeId.String);
            Assert.AreEqual("42.0", (value as StringValue).Value);

            var dateTime = DateTime.Now;
            value = _converter.ConvertTo(new DateTimeValue(dateTime), TypeId.String);
            Assert.AreEqual(dateTime.ToString(), (value as StringValue).Value);
        }

        [TestMethod]
        public void ConvertTo_DateTime()
        {
            var value = _converter.ConvertTo(new IntValue(42), TypeId.DateTime);
            Assert.IsNull((value as DateTimeValue).Value);

            value = _converter.ConvertTo(new RealValue(42.0), TypeId.DateTime);
            Assert.IsNull((value as DateTimeValue).Value);

            value = _converter.ConvertTo(new StringValue("42.0"), TypeId.DateTime);
            Assert.IsNull((value as DateTimeValue).Value);

            var dateString = "04/05/2018 16:19";
            value = _converter.ConvertTo(new StringValue(dateString), TypeId.DateTime);
            Assert.AreEqual(DateTime.Parse(dateString), (value as DateTimeValue).Value);

            var dateTime = DateTime.Now;
            value = _converter.ConvertTo(new DateTimeValue(dateTime), TypeId.DateTime);
            Assert.AreEqual(dateTime, (value as DateTimeValue).Value);
        }
        
        [TestMethod]
        public void ComputeConversionCost_FromInt()
        {
            Assert.AreEqual(0, _converter.ComputeConversionCost(TypeId.Integer, TypeId.Integer));
            Assert.AreEqual(1, _converter.ComputeConversionCost(TypeId.Integer, TypeId.Real));
            Assert.AreEqual(2, _converter.ComputeConversionCost(TypeId.Integer, TypeId.String));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.Integer, TypeId.DateTime));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.Integer, TypeId.Image));
        }

        [TestMethod]
        public void ComputeConversionCost_FromReal()
        {
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.Real, TypeId.Integer));
            Assert.AreEqual(0, _converter.ComputeConversionCost(TypeId.Real, TypeId.Real));
            Assert.AreEqual(1, _converter.ComputeConversionCost(TypeId.Real, TypeId.String));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.Real, TypeId.DateTime));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.Real, TypeId.Image));
        }

        [TestMethod]
        public void ComputeConversionCost_FromString()
        {
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.String, TypeId.Integer));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.String, TypeId.Real));
            Assert.AreEqual(0, _converter.ComputeConversionCost(TypeId.String, TypeId.String));
            Assert.AreEqual(1, _converter.ComputeConversionCost(TypeId.String, TypeId.DateTime));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.String, TypeId.Image));
        }

        [TestMethod]
        public void ComputeConversionCost_FromDateTime()
        {
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.DateTime, TypeId.Integer));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.DateTime, TypeId.Real));
            Assert.AreEqual(1, _converter.ComputeConversionCost(TypeId.DateTime, TypeId.String));
            Assert.AreEqual(0, _converter.ComputeConversionCost(TypeId.DateTime, TypeId.DateTime));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.DateTime, TypeId.Image));
        }

        [TestMethod]
        public void ComputeConversionCost_FromImage()
        {
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.Image, TypeId.Integer));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.Image, TypeId.Real));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.Image, TypeId.String));
            Assert.AreEqual(int.MaxValue, _converter.ComputeConversionCost(TypeId.Image, TypeId.DateTime));
            Assert.AreEqual(0, _converter.ComputeConversionCost(TypeId.Image, TypeId.Image));
        }
    }
}
