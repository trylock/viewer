using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data.Formats.Jpeg;

namespace ViewerTest.Data.Formats.Jpeg
{
    [TestClass]
    public class JpegSegmentUtilsTest
    {
        [TestMethod]
        public void MatchSegment_InvalidType()
        {
            var segment = new JpegSegment(JpegSegmentType.App0, new byte[0], 0);
            Assert.IsFalse(JpegSegmentUtils.MatchSegment(segment, JpegSegmentType.App1, "Test\0"));
        }

        [TestMethod]
        public void MatchSegment_ShortHeader()
        {
            var segment = new JpegSegment(JpegSegmentType.App1, new byte[0], 0);
            Assert.IsFalse(JpegSegmentUtils.MatchSegment(segment, JpegSegmentType.App1, "Test\0"));
        }

        [TestMethod]
        public void MatchSegment_InvalidHeader()
        {
            var segment = new JpegSegment(JpegSegmentType.App1, new byte[]
            {
                (byte) 't', (byte) 'e', (byte) 's', (byte) 't', 0x00, 0x12, 0x34
            }, 0);
            Assert.IsFalse(JpegSegmentUtils.MatchSegment(segment, JpegSegmentType.App1, "Test\0"));
        }

        [TestMethod]
        public void MatchSegment_ValidHeaderAndType()
        {
            var segment = new JpegSegment(JpegSegmentType.App1, new byte[]
            {
                (byte) 'T', (byte) 'e', (byte) 's', (byte) 't', 0x00, 0x12, 0x34
            }, 0);
            Assert.IsTrue(JpegSegmentUtils.MatchSegment(segment, JpegSegmentType.App1, "Test\0"));
        }

        [TestMethod]
        public void JoinSegmentData_NoSegments()
        {
            var segments = new List<JpegSegment>
            {
                new JpegSegment(JpegSegmentType.App0, new byte[0], 0),
                new JpegSegment(JpegSegmentType.App1, new byte[] { 0x12, 0x34 }, 0),
            };
            var data = JpegSegmentUtils.JoinSegmentData(segments, JpegSegmentType.App1, "Test\0");
            Assert.AreEqual(0, data.Length);
        }

        [TestMethod]
        public void JoinSegmentData_OneSegment()
        {
            var segments = new List<JpegSegment>
            {
                new JpegSegment(JpegSegmentType.App0, new byte[0], 0),
                new JpegSegment(JpegSegmentType.App1, new byte[]
                {
                    (byte) 'T', (byte) 'e', (byte) 's', (byte) 't', 0x00, 0x12, 0x34
                }, 0),
                new JpegSegment(JpegSegmentType.App1, new byte[] { 0x12, 0x34 }, 0),
            };
            var data = JpegSegmentUtils.JoinSegmentData(segments, JpegSegmentType.App1, "Test\0");
            CollectionAssert.AreEqual(new byte[]{ 0x12, 0x34 }, data);
        }

        [TestMethod]
        public void JoinSegmentData_MultipleSegments()
        {
            var segments = new List<JpegSegment>
            {
                new JpegSegment(JpegSegmentType.App0, new byte[0], 0),
                new JpegSegment(JpegSegmentType.App1, new byte[]
                {
                    (byte) 'T', (byte) 'e', (byte) 's', (byte) 't', 0x00, 0x12, 0x34
                }, 0),
                new JpegSegment(JpegSegmentType.App1, new byte[] { 0x12, 0x34 }, 0),
                new JpegSegment(JpegSegmentType.App1, new byte[]
                {
                    (byte) 'T', (byte) 'e', (byte) 's', (byte) 't', 0x00, 0x56, 0x78, 0x9A
                }, 0),
            };
            var data = JpegSegmentUtils.JoinSegmentData(segments, JpegSegmentType.App1, "Test\0");
            CollectionAssert.AreEqual(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A }, data);
        }

        [TestMethod]
        public void SplitSegmentData_NoSegments()
        {
            var data = new byte[] { };
            var segments = JpegSegmentUtils.SplitSegmentData(data, JpegSegmentType.App1, "T").ToList();
            Assert.AreEqual(0, segments.Count);
        }

        [TestMethod]
        public void SplitSegmentData_OneSegment()
        {
            var data = new byte[] { 0x12, 0x34 };
            var segments = JpegSegmentUtils.SplitSegmentData(data, JpegSegmentType.App1, "T", 5).ToList();
            Assert.AreEqual(1, segments.Count);

            Assert.AreEqual(JpegSegmentType.App1, segments[0].Type);
            CollectionAssert.AreEqual(new byte[]
            {
                (byte)'T', 0x12, 0x34
            }, segments[0].Bytes);
        }

        [TestMethod]
        public void SplitSegmentData_MultipleSegments()
        {
            var data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A };
            var segments = JpegSegmentUtils.SplitSegmentData(data, JpegSegmentType.App1, "T", 5).ToList();

            Assert.AreEqual(2, segments.Count);

            Assert.AreEqual(JpegSegmentType.App1, segments[0].Type);
            CollectionAssert.AreEqual(new byte[]
            {
                (byte)'T', 0x12, 0x34, 0x56, 0x78
            }, segments[0].Bytes);

            Assert.AreEqual(JpegSegmentType.App1, segments[1].Type);
            CollectionAssert.AreEqual(new byte[]
            {
                (byte)'T', 0x9A
            }, segments[1].Bytes);
        }

        [TestMethod]
        public void SplitSegmentData_MultipleSegmentsWithTheActialMaximalSegmentSize()
        {
            var data = new byte[0xFFFF - 1];
            data[data.Length - 1] = 0xFF;
            data[data.Length - 2] = 0xEE;
            
            var segments = JpegSegmentUtils.SplitSegmentData(data, JpegSegmentType.App1, "T").ToList();

            Assert.AreEqual(2, segments.Count);
            Assert.AreEqual(0xFFFF - 2, segments[0].Bytes.Length);
            Assert.AreEqual(3, segments[1].Bytes.Length);
            Assert.AreEqual((byte) 'T', segments[1].Bytes[0]);
            Assert.AreEqual(0xEE, segments[1].Bytes[1]);
            Assert.AreEqual(0xFF, segments[1].Bytes[2]);
        }
    }
}
