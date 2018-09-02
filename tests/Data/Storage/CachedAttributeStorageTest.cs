using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.IO;

namespace ViewerTest.Data.Storage
{
    [TestClass]
    public class CachedAttributeStorageTest
    {
        private Mock<IAttributeStorage> _persistentStorage;
        private Mock<IDeferredAttributeStorage> _cacheStorage;
        private CachedAttributeStorage _storage;

        [TestInitialize]
        public void Setup()
        {
            _persistentStorage = new Mock<IAttributeStorage>();
            _cacheStorage = new Mock<IDeferredAttributeStorage>();
            _storage = new CachedAttributeStorage(_persistentStorage.Object, _cacheStorage.Object);
        }

        [TestMethod]
        public void Load_NonCachedEntity()
        {
            var entity = new FileEntity("test");
            _cacheStorage
                .Setup(mock => mock.Load("test"))
                .Returns((IEntity) null);
            _persistentStorage
                .Setup(mock => mock.Load("test"))
                .Returns(entity);

            var result = _storage.Load("test");
            Assert.AreEqual(result, entity);

            _cacheStorage.Verify(mock => mock.Store(entity), Times.Once);
        }

        [TestMethod]
        public void Load_NonCachedNonExistentEntity()
        {
            _cacheStorage
                .Setup(mock => mock.Load("test"))
                .Returns((IEntity)null);
            _persistentStorage
                .Setup(mock => mock.Load("test"))
                .Returns((IEntity) null);

            var result = _storage.Load("test");
            Assert.AreEqual(null, result);

            _cacheStorage.Verify(mock => mock.Store(It.IsAny<IEntity>()), Times.Never);
        }

        [TestMethod]
        public void Load_CachedEntity()
        {
            var entity = new FileEntity("test");
            _cacheStorage
                .Setup(mock => mock.Load("test"))
                .Returns(entity);

            var result = _storage.Load("test");
            Assert.AreEqual(entity, result);

            _persistentStorage.Verify(mock => mock.Load(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Store_NullEntity()
        {
            _storage.Store(null);
        }

        [TestMethod]
        public void Store_Entity()
        {
            var entity = new FileEntity("test");
            
            _storage.Store(entity);
            
            _cacheStorage.Verify(mock => mock.Store(entity));
            _persistentStorage.Verify(mock => mock.Store(entity));
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void Store_DontStoreEntityInCacheIfPersistentStorageFails()
        {
            var entity = new FileEntity("test");

            _persistentStorage
                .Setup(mock => mock.Store(entity))
                .Throws(new IOException());

            try
            {
                _storage.Store(entity);
            }
            finally
            {
                _cacheStorage.Verify(mock => mock.Store(It.IsAny<IEntity>()), Times.Never);
                _persistentStorage.Verify(mock => mock.Store(entity));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StoreThumbnail_NullEntity()
        {
            _storage.StoreThumbnail(null);
        }

        [TestMethod]
        public void StoreThumbnail_Entity()
        {
            var entity = new FileEntity("test");

            _storage.StoreThumbnail(entity);

            _cacheStorage.Verify(mock => mock.StoreThumbnail(entity), Times.Once);
            _persistentStorage.Verify(mock => mock.StoreThumbnail(It.IsAny<IEntity>()), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Remove_NullEntity()
        {
            _storage.Delete(null);
        }

        [TestMethod]
        public void Remove_Entity()
        {
            var entity = new FileEntity("test");
            _storage.Delete(entity);

            _cacheStorage.Verify(mock => mock.Delete(entity), Times.Once);
            _persistentStorage.Verify(mock => mock.Delete(entity), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void Remove_DontRemoveEntityFromCacheIfPersistentStorageFails()
        {
            var entity = new FileEntity("test");

            _persistentStorage
                .Setup(mock => mock.Delete(entity))
                .Throws(new IOException());

            try
            {
                _storage.Delete(entity);
            }
            finally
            {
                _cacheStorage.Verify(mock => mock.Delete(It.IsAny<IEntity>()), Times.Never);
                _persistentStorage.Verify(mock => mock.Delete(entity));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Move_NullEntity()
        {
            _storage.Move(null, "path");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Move_NullPath()
        {
            _storage.Move(new FileEntity("test"), null);
        }

        [TestMethod]
        public void Move_Entity()
        {
            var entity = new FileEntity("test");

            _storage.Move(entity, "test2");
            
            _cacheStorage.Verify(mock => mock.Move(entity, "test2"), Times.Once);
            _persistentStorage.Verify(mock => mock.Move(entity, "test2"), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void Move_DontMoveEntityInCacheIfPersistentStorageFails()
        {
            var entity = new FileEntity("test");

            _persistentStorage
                .Setup(mock => mock.Move(entity, "test2"))
                .Throws(new IOException());

            try
            {
                _storage.Move(entity, "test2");
            }
            finally
            {
                _cacheStorage.Verify(mock => mock.Move(entity, It.IsAny<string>()), Times.Never);
                _persistentStorage.Verify(mock => mock.Move(entity, "test2"), Times.Once);
            }
        }

        [TestMethod]
        public void Dispose_ApplyPendingChanges()
        {
            _storage.Dispose();

            _cacheStorage.Verify(mock => mock.ApplyChanges(), Times.Once);
        }
    }
}
