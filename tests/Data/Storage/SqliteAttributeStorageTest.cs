using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.Data.SQLite;
using Viewer.Data.Storage;
using Viewer.IO;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data.Storage
{
    /// <summary>
    /// Integration test of SqliteAttributeStorage
    /// </summary>
    [TestClass]
    public class SqliteAttributeStorageTest
    {
        private class ConfigurationMock : IStorageConfiguration
        {
            public TimeSpan CacheLifespan { get; set; } = TimeSpan.FromDays(1);
            public int CacheMaxFileCount { get; set; } = int.MaxValue;
        }

        private SqliteAttributeStorage _storage;
        private ConfigurationMock _configuration;

        [TestInitialize]
        public void Setup()
        {
            var factory = new SQLiteConnectionFactory(new FileSystem(), "test.db");
            
            var fileMetadataSerializer = new Mock<IAttributeSerializer>();
            fileMetadataSerializer
                .Setup(mock => mock.Deserialize(It.IsAny<FileInfo>(), It.IsAny<Stream>()))
                .Returns(Enumerable.Empty<Attribute>());

            _configuration = new ConfigurationMock();
            _storage = new SqliteAttributeStorage(
                factory, 
                _configuration, 
                fileMetadataSerializer.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _storage.Dispose();
            SQLiteConnection.ClearAllPools();
            File.Delete("test.db");
        }

        [TestMethod]
        public void Load_MissingEntity()
        {
            var entity = _storage.Load("test");
            Assert.IsNull(entity);
        }

        [TestMethod]
        public void Store_StoringEmptyEntityWillDeleteIt()
        {
            IEntity entity = new FileEntity("test");
            entity = entity.SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var storedEntity = _storage.Load("test");
            Assert.IsNotNull(storedEntity);

            _storage.Store(new FileEntity("test"));
            _storage.ApplyChanges();

            storedEntity = _storage.Load("test");
            Assert.IsNull(storedEntity);
        }

        [TestMethod]
        public void Store_RewriteEntity()
        {
            IEntity entity = new FileEntity("ěščřžýáíé");
            entity = entity.SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var storedEntity = _storage.Load("ěščřžýáíé");
            Assert.AreEqual(1, storedEntity.GetValue<IntValue>("attr").Value);

            IEntity newEntity = new FileEntity("ĚŠČŘŽÝÁÍÉ");
            newEntity = newEntity.SetAttribute(new Attribute("attr2", new IntValue(2), AttributeSource.Custom));
            _storage.Store(newEntity);
            _storage.ApplyChanges();

            storedEntity = _storage.Load("ěščřžýáíé");
            Assert.IsNull(storedEntity.GetAttribute("attr"));
            Assert.AreEqual(2, storedEntity.GetValue<IntValue>("attr2").Value);
        }

        [TestMethod]
        public void StoreThumbnail_NonExistentFile()
        {
            var entity1 = new FileEntity("test1")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));
            var entity2 = new FileEntity("test2")
                .SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] {0x42}), AttributeSource.Metadata));

            _storage.Store(entity1);
            _storage.StoreThumbnail(entity2);
            _storage.ApplyChanges();

            var result = _storage.Load("test1");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);

            result = _storage.Load("test2");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void StoreThumbnail_InsertNewThumbnail()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
            Assert.IsNull(result.GetAttribute("thumbnail"));

            entity = entity.SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] {0x42}),
                AttributeSource.Metadata));

            _storage.StoreThumbnail(entity);
            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
            Assert.AreEqual(1, result.GetValue<ImageValue>("thumbnail").Value.Length);
            Assert.AreEqual(0x42, result.GetValue<ImageValue>("thumbnail").Value[0]);
        }

        [TestMethod]
        public void StoreThumbnail_ReplaceAnExistingThumbnail()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom))
                .SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] {0x21}), AttributeSource.Metadata));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
            Assert.AreEqual(1, result.GetValue<ImageValue>("thumbnail").Value.Length);
            Assert.AreEqual(0x21, result.GetValue<ImageValue>("thumbnail").Value[0]);

            entity = entity.SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] {0x42}),
                AttributeSource.Metadata));

            _storage.StoreThumbnail(entity);
            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
            Assert.AreEqual(1, result.GetValue<ImageValue>("thumbnail").Value.Length);
            Assert.AreEqual(0x42, result.GetValue<ImageValue>("thumbnail").Value[0]);
        }

        [TestMethod]
        public void StoreThumbnail_EntityWithoutThumbnailAttribute()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom))
                .SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] {0x21}), AttributeSource.Metadata));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
            Assert.AreEqual(1, result.GetValue<ImageValue>("thumbnail").Value.Length);
            Assert.AreEqual(0x21, result.GetValue<ImageValue>("thumbnail").Value[0]);

            entity = entity.RemoveAttribute("thumbnail");

            _storage.StoreThumbnail(entity);
            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
            Assert.AreEqual(1, result.GetValue<ImageValue>("thumbnail").Value.Length);
            Assert.AreEqual(0x21, result.GetValue<ImageValue>("thumbnail").Value[0]);
        }

        [TestMethod]
        public void Move_NonExistentEntity()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            _storage.Move(entity, "test2");
            _storage.ApplyChanges();

            var result = _storage.Load(entity.Path);
            Assert.IsNull(result);
            result = _storage.Load("test2");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Move_TargetPathIsNotInCache()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var result = _storage.Load(entity.Path);
            Assert.IsNotNull(result);

            _storage.Move(entity, "test2");
            _storage.ApplyChanges();

            result = _storage.Load(entity.Path);
            Assert.IsNull(result);
            result = _storage.Load("test2");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Move_TargetPathIsInCache()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));
            var entity2 = new FileEntity("test2")
                .SetAttribute(new Attribute("attr", new IntValue(2), AttributeSource.Custom));

            _storage.Store(entity);
            _storage.Store(entity2);
            _storage.ApplyChanges();

            var result = _storage.Load(entity.Path);
            Assert.IsNotNull(result);
            result = _storage.Load(entity2.Path);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.GetValue<IntValue>("attr").Value);

            _storage.Move(entity, "test2");
            _storage.ApplyChanges();

            result = _storage.Load(entity.Path);
            Assert.IsNull(result);
            result = _storage.Load("test2");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
        }

        [TestMethod]
        public void Remove_NonExistentFile()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            var result = _storage.Load(entity.Path);
            Assert.IsNull(result);

            _storage.Delete(entity);
            _storage.ApplyChanges();

            result = _storage.Load(entity.Path);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Remove_Entity()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var result = _storage.Load(entity.Path);
            Assert.IsNotNull(result);

            _storage.Delete(entity);
            _storage.ApplyChanges();

            result = _storage.Load(entity.Path);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Load_ReadIntAttribute()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            var result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.Store(entity);
            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
        }

        [TestMethod]
        public void Load_ReadRealAttribute()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new RealValue(3.1415), AttributeSource.Custom));

            var result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.Store(entity);
            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(3.1415, result.GetValue<RealValue>("attr").Value);
        }

        [TestMethod]
        public void Load_ReadStringAttribute()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new StringValue("hello"), AttributeSource.Custom));

            var result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.Store(entity);
            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual("hello", result.GetValue<StringValue>("attr").Value);
        }

        [TestMethod]
        public void Load_ReadDateTimeAttribute()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new DateTimeValue(new DateTime(2018, 8, 25)),
                    AttributeSource.Custom));

            var result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.Store(entity);
            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(new DateTime(2018, 8, 25), result.GetValue<DateTimeValue>("attr").Value);
        }

        [TestMethod]
        public void Load_ReadImageAttribute()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new ImageValue(new byte[] {0x42}), AttributeSource.Custom));

            var result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.Store(entity);
            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(new byte[] {0x42}, result.GetValue<ImageValue>("attr").Value);
        }

        [TestMethod]
        public void Load_ReadUncommittedWrite()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            var result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.Store(entity);

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
        }

        [TestMethod]
        public void Load_ReadUncommittedDelete()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            var result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.Store(entity);
            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);

            _storage.Delete(entity);

            result = _storage.Load("test");
            Assert.IsNull(result);
        }
        
        [TestMethod]
        public void Store_ReplaceUncommittedDelete()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            var result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.Store(entity);
            _storage.ApplyChanges();
            result = _storage.Load("test");
            Assert.IsNotNull(result);

            _storage.Delete(entity);
            result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.Store(entity);
            result = _storage.Load("test");
            Assert.IsNotNull(result);

            _storage.ApplyChanges();
            result = _storage.Load("test");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Store_ReplaceUncommittedStoreThumbnail()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("value", new IntValue(1), AttributeSource.Custom));
            var entityWithThumbnail = new FileEntity("test")
                .SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] { 0x42 }), AttributeSource.Custom));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var result = _storage.Load("test");
            Assert.IsNotNull(result);

            _storage.StoreThumbnail(entityWithThumbnail);
            _storage.Store(entity);

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("value").Value);
            // This has to be true since the store request could change its thumbnail. We have to
            // replace even the thumbnail.
            Assert.IsNull(result.GetAttribute("thumbnail"));

            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("value").Value);
            Assert.IsNull(result.GetAttribute("thumbnail"));
        }

        [TestMethod]
        public void Remove_ReplaceUncommittedStore()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("value", new IntValue(1), AttributeSource.Custom));

            _storage.Store(entity);
            
            var result = _storage.Load("test");
            Assert.IsNotNull(result);

            _storage.Delete(entity);
            
            result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Remove_ReplaceUncommittedStoreThumbnail()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("value", new IntValue(1), AttributeSource.Custom));
            var entityWithThumbnail = new FileEntity("test")
                .SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] { 0x42 }), AttributeSource.Custom));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var result = _storage.Load("test");
            Assert.IsNotNull(result);

            _storage.StoreThumbnail(entityWithThumbnail);

            result = _storage.Load("test");
            Assert.IsNotNull(result);

            _storage.Delete(entity);

            result = _storage.Load("test");
            Assert.IsNull(result);

            _storage.ApplyChanges();

            result = _storage.Load("test");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Move_DontIgnoreUncommittedDelete()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("value", new IntValue(1), AttributeSource.Custom));

            _storage.Store(entity);
            _storage.ApplyChanges();

            var test1 = _storage.Load("test");
            Assert.IsNotNull(test1);

            _storage.Delete(entity);

            test1 = _storage.Load("test");
            Assert.IsNull(test1);

            _storage.Move(entity, "test2");

            test1 = _storage.Load("test");
            var test2 = _storage.Load("test2");
            Assert.IsNull(test1);
            Assert.IsNull(test2);

            _storage.ApplyChanges();

            test1 = _storage.Load("test");
            test2 = _storage.Load("test2");
            Assert.IsNull(test1);
            Assert.IsNull(test2);
        }

        [TestMethod]
        public async Task ApplyChanges_CleanOutdatedFiles()
        {
            _configuration.CacheLifespan = TimeSpan.FromMilliseconds(1000);

            var entity1 = new FileEntity("test1")
                .SetAttribute(new Attribute("value", new IntValue(1), AttributeSource.Custom));
            var entity2 = new FileEntity("test2")
                .SetAttribute(new Attribute("value", new IntValue(1), AttributeSource.Custom));

            _storage.Store(entity1);
            _storage.Store(entity2);
            _storage.ApplyChanges();

            var result1 = _storage.Load("test1");
            var result2 = _storage.Load("test2");
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);

            await Task.Delay(1100);

            result1 = _storage.Load("test1");
            Assert.IsNotNull(result1);

            // entity2 has not been accessed so it will be deleted
            _storage.ApplyChanges();

            result1 = _storage.Load("test1");
            result2 = _storage.Load("test2");
            Assert.IsNotNull(result1);
            Assert.IsNull(result2);
        }
    }
}
