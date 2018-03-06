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
        public void CopySegmentData_NoSegments()
        {
            var segments = new List<JpegSegment>
            {
                new JpegSegment(JpegSegmentType.App0, new byte[0], 0),
                new JpegSegment(JpegSegmentType.App1, new byte[] { 0x12, 0x34 }, 0),
            };
            var data = JpegSegmentUtils.CopySegmentData(segments, JpegSegmentType.App1, "Test\0");
            Assert.AreEqual(0, data.Length);
        }

        [TestMethod]
        public void CopySegmentData_OneSegment()
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
            var data = JpegSegmentUtils.CopySegmentData(segments, JpegSegmentType.App1, "Test\0");
            CollectionAssert.AreEqual(new byte[]{ 0x12, 0x34 }, data);
        }

        [TestMethod]
        public void CopySegmentData_MultipleSegments()
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
            var data = JpegSegmentUtils.CopySegmentData(segments, JpegSegmentType.App1, "Test\0");
            CollectionAssert.AreEqual(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A }, data);
        }
    }
}
