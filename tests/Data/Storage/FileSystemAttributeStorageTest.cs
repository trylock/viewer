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
        private Mock<IAttributeSerializer> _serializer;
        private Mock<IFileSystem> _fileSystem;
        private FileSystemAttributeStorage _storage;
        private MemoryStream _input;
        private MemoryStream _output;
        
        [TestInitialize]
        public void Setup()
        {
            _input = new MemoryStream();
            _output = new MemoryStream();
            _fileSystem = new Mock<IFileSystem>();
            _fileSystem
                .Setup(mock => mock.OpenRead(It.IsAny<string>()))
                .Returns(_input);
            _fileSystem
                .Setup(mock => mock.CreateTemporaryFile(It.IsAny<string>(), out _tmpFileName))
                .Returns(_output);

            _serializer = new Mock<IAttributeSerializer>();
            _serializer
                .Setup(mock => mock.CanRead(It.IsAny<FileInfo>()))
                .Returns(true);
            _serializer
                .Setup(mock => mock.CanWrite(It.IsAny<FileInfo>()))
                .Returns(true);

            _storage = new FileSystemAttributeStorage(
                _fileSystem.Object, 
                new[]{ _serializer.Object });
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
            _serializer
                .Setup(mock => mock.Deserialize(It.IsAny<FileInfo>(), It.IsAny<Stream>()))
                .Throws(new FileNotFoundException());

            var entity = _storage.Load("test");
            Assert.IsNull(entity);
        }

        [TestMethod]
        public void Load_NonExistentDirectory()
        {
            _serializer
                .Setup(mock => mock.Deserialize(It.IsAny<FileInfo>(), It.IsAny<Stream>()))
                .Throws(new DirectoryNotFoundException());

            var entity = _storage.Load("test");
            Assert.IsNull(entity);
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void Load_UnauthorizedAccess()
        {
            _serializer
                .Setup(mock => mock.Deserialize(It.IsAny<FileInfo>(), It.IsAny<Stream>()))
                .Throws(new UnauthorizedAccessException());
            _storage.Load("test");
        }

        [TestMethod]
        public void Load_NoData()
        {
            _serializer
                .Setup(mock => mock.Deserialize(It.IsAny<FileInfo>(), It.IsAny<Stream>()))
                .Returns(Enumerable.Empty<Attribute>());

            var entity = _storage.Load("test");
            Assert.IsNotNull(entity);
            CollectionAssert.AreEqual(new Attribute[0], entity.ToArray());
        }

        [TestMethod]
        public void Load_Attributes()
        {
            _serializer
                .Setup(mock => mock.Deserialize(It.IsAny<FileInfo>(), It.IsAny<Stream>()))
                .Returns(new[]
                {
                    new Attribute("attr", new IntValue(4), AttributeSource.Custom)
                });


            var entity = _storage.Load("test");
            Assert.IsNotNull(entity);
            Assert.AreEqual("attr", entity.GetAttribute("attr").Name);
            Assert.AreEqual(4, entity.GetValue<IntValue>("attr").Value);
            Assert.AreEqual(AttributeSource.Custom, entity.GetAttribute("attr").Source);
            Assert.AreEqual(1, entity.Count());
        }

        [TestMethod]
        public void Remove_DeleteFile()
        {
            _storage.Delete(new FileEntity("test"));
            
            _fileSystem.Verify(mock => mock.DeleteFile(PathUtils.NormalizePath("test")), Times.Once);
        }

        [TestMethod]
        public void Remove_DeleteDirectroy()
        {
            _storage.Delete(new DirectoryEntity("test"));

            _fileSystem.Verify(mock => mock.DeleteDirectory(PathUtils.NormalizePath("test"), true), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataFormatException))]
        public void Store_InvalidFileFormat()
        {
            _serializer
                .Setup(mock => mock.Serialize(
                    It.IsAny<FileInfo>(),
                    It.IsAny<Stream>(),
                    It.IsAny<Stream>(),
                    It.IsAny<IEnumerable<Attribute>>()))
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
            _serializer
                .Setup(mock => mock.Serialize(
                    It.IsAny<FileInfo>(), 
                    It.IsAny<Stream>(), 
                    It.IsAny<Stream>(), 
                    It.IsAny<IEnumerable<Attribute>>()))
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
    }
}
