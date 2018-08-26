using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.Data.Formats.Jpeg;
using Viewer.Data.Storage;
using Viewer.IO;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data.Storage
{
    [TestClass]
    public class FileSystemAttributeStorageTest
    {
        private string _tmpFileName = "tmpTest";
        private Mock<IJpegSegmentReader> _segmentReader;
        private Mock<IJpegSegmentReaderFactory> _segmentReaderFactory;
        private Mock<IJpegSegmentWriter> _segmentWriter;
        private Mock<IJpegSegmentWriterFactory> _segmentWriterFactory;
        private Mock<IAttributeReader> _attrReader;
        private Mock<IAttributeReaderFactory> _attrReaderFactory;
        private Mock<IAttributeWriter> _attrWriter;
        private Mock<IAttributeWriterFactory> _attrWriterFactory;
        private Mock<IFileSystem> _fileSystem;
        private FileSystemAttributeStorage _storage;
        
        [TestInitialize]
        public void Setup()
        {
            _segmentReader = new Mock<IJpegSegmentReader>();
            _segmentWriter = new Mock<IJpegSegmentWriter>();
            _attrReader = new Mock<IAttributeReader>();
            _attrWriter = new Mock<IAttributeWriter>();
            _fileSystem = new Mock<IFileSystem>();

            _segmentReaderFactory = new Mock<IJpegSegmentReaderFactory>();
            _segmentReaderFactory
                .Setup(mock => mock.CreateFromPath(It.IsAny<string>()))
                .Returns(_segmentReader.Object);

            _segmentWriterFactory = new Mock<IJpegSegmentWriterFactory>();
            _segmentWriterFactory
                .Setup(mock => mock.CreateFromPath(It.IsAny<string>(), out _tmpFileName))
                .Returns(_segmentWriter.Object);

            _attrWriterFactory = new Mock<IAttributeWriterFactory>();
            _attrWriterFactory
                .Setup(mock => mock.Create(It.IsAny<Stream>()))
                .Returns(_attrWriter.Object);

            _attrReaderFactory = new Mock<IAttributeReaderFactory>();
            _attrReaderFactory
                .Setup(mock => mock.CreateFromSegments(It.IsAny<FileInfo>(), It.IsAny<IEnumerable<JpegSegment>>()))
                .Returns(_attrReader.Object);

            _storage = new FileSystemAttributeStorage(
                _fileSystem.Object, 
                _segmentReaderFactory.Object, 
                _segmentWriterFactory.Object,
                _attrWriterFactory.Object, 
                new[]{ _attrReaderFactory.Object });
        }

        [TestMethod]
        public void Load_DirectoryEntity()
        {
            _fileSystem
                .Setup(mock => mock.DirectoryExists("test"))
                .Returns(true);

            var entity = _storage.Load("test");
            Assert.IsInstanceOfType(entity, typeof(DirectoryEntity));
            Assert.AreEqual(PathUtils.NormalizePath("test"), entity.Path);
        }

        [TestMethod]
        public void Load_NonExistentFile()
        {
            _segmentReader
                .Setup(mock => mock.GetEnumerator())
                .Throws(new FileNotFoundException());

            var entity = _storage.Load("test");
            Assert.IsNull(entity);
        }

        [TestMethod]
        public void Load_NonExistentDirectory()
        {
            _segmentReader
                .Setup(mock => mock.GetEnumerator())
                .Throws(new DirectoryNotFoundException());

            var entity = _storage.Load("test");
            Assert.IsNull(entity);
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void Load_UnauthorizedAccess()
        {
            _segmentReader
                .Setup(mock => mock.GetEnumerator())
                .Throws(new UnauthorizedAccessException());
            _storage.Load("test");
        }

        [TestMethod]
        public void Load_NoSegments()
        {
            _segmentReader
                .Setup(mock => mock.GetEnumerator())
                .Returns(Enumerable.Empty<JpegSegment>().GetEnumerator());

            _attrReader
                .Setup(mock => mock.Read())
                .Returns<Attribute>(null);

            var entity = _storage.Load("test");
            Assert.IsNotNull(entity);
            CollectionAssert.AreEqual(new Attribute[0], entity.ToArray());

            _segmentReaderFactory.Verify(mock => mock.CreateFromPath("test"), Times.Once);
            _attrReaderFactory.Verify(mock => mock.CreateFromSegments(
                It.IsAny<FileInfo>(), 
                It.Is<IEnumerable<JpegSegment>>(segments => !segments.Any())
            ), Times.Once);
        }

        [TestMethod]
        public void Load_Attributes()
        {
            var segments = new List<JpegSegment>
            {
                new JpegSegment(JpegSegmentType.App1, new byte[]{ 0x42 }, 1)
            };

            _segmentReader
                .Setup(mock => mock.GetEnumerator())
                .Returns(segments.GetEnumerator());
            _attrReader
                .SetupSequence(mock => mock.Read())
                .Returns(new Attribute("attr", new IntValue(4), AttributeSource.Custom))
                .Returns((Attribute) null);

            var entity = _storage.Load("test");
            Assert.IsNotNull(entity);
            Assert.AreEqual("attr", entity.GetAttribute("attr").Name);
            Assert.AreEqual(4, entity.GetValue<IntValue>("attr").Value);
            Assert.AreEqual(AttributeSource.Custom, entity.GetAttribute("attr").Source);
            Assert.AreEqual(1, entity.Count());

            _segmentReaderFactory.Verify(mock => mock.CreateFromPath("test"), Times.Once);
            _attrReaderFactory.Verify(mock => mock.CreateFromSegments(
                It.IsAny<FileInfo>(),
                It.Is<IEnumerable<JpegSegment>>(seg => seg.SequenceEqual(segments))
            ), Times.Once);
        }

        [TestMethod]
        public void Remove_DeleteFile()
        {
            _storage.Remove(new FileEntity("test"));
            
            _fileSystem.Verify(mock => mock.DeleteFile(PathUtils.NormalizePath("test")), Times.Once);
        }

        [TestMethod]
        public void Remove_DeleteDirectroy()
        {
            _storage.Remove(new DirectoryEntity("test"));

            _fileSystem.Verify(mock => mock.DeleteDirectory(PathUtils.NormalizePath("test"), true), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void Store_InvalidFileFormat()
        {
            _segmentReader
                .Setup(mock => mock.ReadSegment())
                .Throws(new InvalidDataFormatException(1, "error"));

            try
            {
                _storage.Store(new FileEntity("test"));
            }
            finally
            {
                _fileSystem.Verify(mock => mock.ReplaceFile(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>()
                ), Times.Never);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void Store_FailureInCopying()
        {
            _segmentReader
                .Setup(mock => mock.ReadSegment())
                .Returns((JpegSegment) null);
            _segmentWriter
                .Setup(mock => mock.Finish(It.IsAny<Stream>()))
                .Throws(new IOException());

            try
            {
                _storage.Store(new FileEntity("test"));
            }
            finally
            {
                _fileSystem.Verify(mock => mock.ReplaceFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ), Times.Never);
            }
        }

        [TestMethod]
        public void Store_EmptyEntityAndNoAttributeSegments()
        {
            var segments = new[] 
            { 
                new JpegSegment(JpegSegmentType.App1, new byte[] {0x1}, 0),
                new JpegSegment(JpegSegmentType.App1, new byte[] {0x2}, 1),
                new JpegSegment(JpegSegmentType.Sos, new byte[] { }, 2),
            };

            _segmentReader
                .SetupSequence(mock => mock.ReadSegment())
                .Returns(segments[0])
                .Returns(segments[1])
                .Returns(segments[2])
                .Returns((JpegSegment) null);
            
            _storage.Store(new FileEntity("test"));

            _segmentReader.Verify(mock => mock.ReadSegment(), Times.Exactly(4));
            _segmentReader.Verify(mock => mock.Dispose(), Times.Once);
            _segmentReader.Verify(mock => mock.BaseStream, Times.AtLeastOnce);
            _segmentReader.VerifyNoOtherCalls();

            _segmentWriter.Verify(mock => mock.WriteSegment(segments[0]), Times.Once);
            _segmentWriter.Verify(mock => mock.WriteSegment(segments[1]), Times.Once);
            _segmentWriter.Verify(mock => mock.WriteSegment(segments[2]), Times.Never);
            _segmentWriter.Verify(mock => mock.Finish(It.IsAny<Stream>()), Times.Once);
            _segmentWriter.Verify(mock => mock.Dispose(), Times.Once);
            _segmentWriter.VerifyNoOtherCalls();

            _fileSystem.Verify(mock => mock.ReplaceFile(_tmpFileName, PathUtils.NormalizePath("test"), null), Times.Once);
        }

        [TestMethod]
        public void Store_EmptyEntityAndOneAttributeSegment()
        {
            var segments = new[]
            {
                new JpegSegment(JpegSegmentType.App1, new byte[] { 0x41, 0x74, 0x74, 0x72, 0x0, 0x0, 0x2 }, 0),
                new JpegSegment(JpegSegmentType.App1, new byte[] {0x1}, 7),
                new JpegSegment(JpegSegmentType.Sos, new byte[] { }, 8),
            };
            
            _segmentReader
                .SetupSequence(mock => mock.ReadSegment())
                .Returns(segments[0])
                .Returns(segments[1])
                .Returns(segments[2])
                .Returns((JpegSegment)null);

            _storage.Store(new FileEntity("test"));

            _segmentReader.Verify(mock => mock.ReadSegment(), Times.Exactly(4));
            _segmentReader.Verify(mock => mock.Dispose(), Times.Once);
            _segmentReader.Verify(mock => mock.BaseStream, Times.AtLeastOnce);
            _segmentReader.VerifyNoOtherCalls();

            _segmentWriter.Verify(mock => mock.WriteSegment(segments[0]), Times.Never);
            _segmentWriter.Verify(mock => mock.WriteSegment(segments[1]), Times.Once);
            _segmentWriter.Verify(mock => mock.WriteSegment(segments[2]), Times.Never);
            _segmentWriter.Verify(mock => mock.WriteSegment(segments[2]), Times.Never);
            _segmentWriter.Verify(mock => mock.Finish(It.IsAny<Stream>()), Times.Once);
            _segmentWriter.Verify(mock => mock.Dispose(), Times.Once);
            _segmentWriter.VerifyNoOtherCalls();

            _fileSystem.Verify(mock => mock.ReplaceFile(_tmpFileName, PathUtils.NormalizePath("test"), null), Times.Once);
        }

        [TestMethod]
        public void Store_NonEmptyEntityAndOneAttributeSegment()
        {
            var segments = new[]
            {
                new JpegSegment(JpegSegmentType.App1, new byte[] { 0x41, 0x74, 0x74, 0x72, 0x0, 0x1, 0x2, 0x3 }, 0),
                new JpegSegment(JpegSegmentType.App1, new byte[] {0x1}, 8),
                new JpegSegment(JpegSegmentType.Sos, new byte[] { }, 9),
            };

            _segmentReader
                .SetupSequence(mock => mock.ReadSegment())
                .Returns(segments[0])
                .Returns(segments[1])
                .Returns(segments[2])
                .Returns((JpegSegment)null);
            
            _attrWriterFactory
                .Setup(mock => mock.Create(It.IsAny<Stream>()))
                .Callback<Stream>(stream =>
                {
                    // write data to the stream 
                    stream.WriteByte(0x24);
                    stream.WriteByte(0x42);
                })
                .Returns(_attrWriter.Object);

            var attr = new Attribute("attr", new IntValue(1), AttributeSource.Custom);
            _storage.Store(new FileEntity("test").SetAttribute(attr));

            _attrWriter.Verify(mock => mock.Write(attr), Times.Once);
            _attrWriter.VerifyNoOtherCalls();

            _segmentReader.Verify(mock => mock.ReadSegment(), Times.Exactly(4));
            _segmentReader.Verify(mock => mock.Dispose(), Times.Once);
            _segmentReader.Verify(mock => mock.BaseStream, Times.AtLeastOnce);
            _segmentReader.VerifyNoOtherCalls();

            _segmentWriter.Verify(mock => mock.WriteSegment(segments[0]), Times.Never);
            _segmentWriter.Verify(mock => mock.WriteSegment(segments[1]), Times.Once);
            _segmentWriter.Verify(mock => mock.WriteSegment(segments[2]), Times.Never);
            _segmentWriter.Verify(mock => mock.WriteSegment(segments[2]), Times.Never);
            _segmentWriter.Verify(mock => mock.WriteSegment(It.Is<JpegSegment>(segment => 
                segment.Type == JpegSegmentType.App1 &&
                segment.Bytes.SequenceEqual(new byte[] { 0x41, 0x74, 0x74, 0x72, 0x0, 0x24, 0x42 })
            )), Times.Once);
            _segmentWriter.Verify(mock => mock.Finish(It.IsAny<Stream>()), Times.Once);
            _segmentWriter.Verify(mock => mock.Dispose(), Times.Once);
            _segmentWriter.VerifyNoOtherCalls();

            _fileSystem.Verify(mock => mock.ReplaceFile(_tmpFileName, PathUtils.NormalizePath("test"), null), Times.Once);
        }
    }
}
