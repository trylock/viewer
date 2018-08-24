using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.SQLite;
using Viewer.Data.Storage;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Data.Storage
{
    /// <summary>
    /// Integration test of SqliteAttributeStorage
    /// </summary>
    [TestClass]
    public class SqliteAttributeStorageTest
    {
        private SQLiteConnection _connection;
        private SqliteAttributeStorage _storage;

        [TestInitialize]
        public void Setup()
        {
            var factory = new SQliteConnectionFactory();
            _connection = factory.Create(":memory:");
            _storage = new SqliteAttributeStorage(_connection);
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

            var storedEntity = _storage.Load("test");
            Assert.IsNotNull(storedEntity);

            _storage.Store(new FileEntity("test"));

            storedEntity = _storage.Load("test");
            Assert.IsNull(storedEntity);
        }

        [TestMethod]
        public void Store_RewriteEntity()
        {
            IEntity entity = new FileEntity("ěščřžýáíé");
            entity = entity.SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));
            
            _storage.Store(entity);

            var storedEntity = _storage.Load("ěščřžýáíé");
            Assert.AreEqual(1, storedEntity.GetValue<IntValue>("attr").Value);

            IEntity newEntity = new FileEntity("ĚŠČŘŽÝÁÍÉ");
            newEntity = newEntity.SetAttribute(new Attribute("attr2", new IntValue(2), AttributeSource.Custom));
            _storage.Store(newEntity);

            storedEntity = _storage.Load("ěščřžýáíé");
            Assert.IsNull(storedEntity.GetAttribute("attr"));
            Assert.AreEqual(2, storedEntity.GetValue<IntValue>("attr2").Value);
        }

        [TestMethod]
        public void BeginBatch_RollbackNestedTransaction()
        {
            var entity1 = new FileEntity("test1")
                .SetAttribute(new Attribute("attr1", new IntValue(1), AttributeSource.Custom));
            var entity2 = new FileEntity("test2")
                .SetAttribute(new Attribute("attr2", new IntValue(2), AttributeSource.Custom));

            using (var transation = _storage.BeginBatch())
            {
                _storage.Store(entity1);

                using (var nested = _storage.BeginBatch())
                {
                    _storage.Store(entity2);
                }

                transation.Commit();
            }

            var result = _storage.Load("test1");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr1").Value);

            result = _storage.Load("test2");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void BeginBatch_RollbackCommittedNestedTransaction()
        {
            var entity1 = new FileEntity("test1")
                .SetAttribute(new Attribute("attr1", new IntValue(1), AttributeSource.Custom));
            var entity2 = new FileEntity("test2")
                .SetAttribute(new Attribute("attr2", new IntValue(2), AttributeSource.Custom));

            using (var transation = _storage.BeginBatch())
            {
                _storage.Store(entity1);

                using (var nested = _storage.BeginBatch())
                {
                    _storage.Store(entity2);
                    nested.Commit();
                }
            }

            var result = _storage.Load("test1");
            Assert.IsNull(result);

            result = _storage.Load("test2");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void BeginBatch_CommitNestedTransaction()
        {
            var entity1 = new FileEntity("test1")
                .SetAttribute(new Attribute("attr1", new IntValue(1), AttributeSource.Custom));
            var entity2 = new FileEntity("test2")
                .SetAttribute(new Attribute("attr2", new IntValue(2), AttributeSource.Custom));

            using (var transation = _storage.BeginBatch())
            {
                _storage.Store(entity1);

                using (var nested = _storage.BeginBatch())
                {
                    _storage.Store(entity2);
                    nested.Commit();
                }

                transation.Commit();
            }

            var result = _storage.Load("test1");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr1").Value);

            result = _storage.Load("test2");
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.GetValue<IntValue>("attr2").Value);
        }

        [TestMethod]
        public void StoreThumbnail_NonExistentFile()
        {
            var entity1 = new FileEntity("test1")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));
            var entity2 = new FileEntity("test2")
                .SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[]{ 0x42 }), AttributeSource.Metadata));
            using (var transaction = _storage.BeginBatch())
            {
                _storage.Store(entity1);
                _storage.StoreThumbnail(entity2);
                transaction.Commit();
            }

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

            var result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
            Assert.IsNull(result.GetAttribute("thumbnail"));

            entity = entity.SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] { 0x42 }), AttributeSource.Metadata));
            
            _storage.StoreThumbnail(entity);

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
                .SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] { 0x21 }), AttributeSource.Metadata));

            _storage.Store(entity);

            var result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
            Assert.AreEqual(1, result.GetValue<ImageValue>("thumbnail").Value.Length);
            Assert.AreEqual(0x21, result.GetValue<ImageValue>("thumbnail").Value[0]);

            entity = entity.SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] { 0x42 }), AttributeSource.Metadata));

            _storage.StoreThumbnail(entity);

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
                .SetAttribute(new Attribute("thumbnail", new ImageValue(new byte[] { 0x21 }), AttributeSource.Metadata));

            _storage.Store(entity);

            var result = _storage.Load("test");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.GetValue<IntValue>("attr").Value);
            Assert.AreEqual(1, result.GetValue<ImageValue>("thumbnail").Value.Length);
            Assert.AreEqual(0x21, result.GetValue<ImageValue>("thumbnail").Value[0]);

            entity = entity.RemoveAttribute("thumbnail");

            _storage.StoreThumbnail(entity);

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

            var result = _storage.Load(entity.Path);
            Assert.IsNotNull(result);

            _storage.Move(entity, "test2");

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

            var result = _storage.Load(entity.Path);
            Assert.IsNotNull(result);
            result = _storage.Load(entity2.Path);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.GetValue<IntValue>("attr").Value);

            _storage.Move(entity, "test2");

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
            
            _storage.Remove(entity);

            result = _storage.Load(entity.Path);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Remove_Entity()
        {
            var entity = new FileEntity("test")
                .SetAttribute(new Attribute("attr", new IntValue(1), AttributeSource.Custom));

            _storage.Store(entity);

            var result = _storage.Load(entity.Path);
            Assert.IsNotNull(result);

            _storage.Remove(entity);

            result = _storage.Load(entity.Path);
            Assert.IsNull(result);
        }
    }
}
