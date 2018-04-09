using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.UI.Attributes;

namespace ViewerTest.UI.Attributes
{
    [TestClass]
    public class AttributeManagerTest
    {
        private MemoryAttributeStorage _storageMock;
        private EntityManager _entitiesMock;
        private SelectionMock _selectionMock;
        private AttributeManager _attributes;

        [TestInitialize]
        public void Setup()
        {
            _storageMock = new MemoryAttributeStorage();
            _entitiesMock = new EntityManager(_storageMock);
            _selectionMock = new SelectionMock();
            _attributes = new AttributeManager(_entitiesMock, _selectionMock);
        }

        [TestMethod]
        public void SetAttribute_EmptySelection()
        {
            _attributes.SetAttribute("test", new StringAttribute("test", "value"));

            var attrs = _attributes.GetSelectedAttributes().ToList();
            Assert.AreEqual(0, attrs.Count);
        }

        [TestMethod]
        public void SetAttribute_OneEntity()
        {
            _storageMock.Add(new Entity("test"));
            _selectionMock.Replace(new []{ "test" });
            
            var attrs = _attributes.GetSelectedAttributes().ToList();
            Assert.AreEqual(0, attrs.Count);

            _attributes.SetAttribute("attr", new StringAttribute("attr", "value"));
            
            attrs = _attributes.GetSelectedAttributes().ToList();
            Assert.AreEqual(1, attrs.Count);
            Assert.IsFalse(attrs[0].IsMixed);
            Assert.AreEqual(new StringAttribute("attr", "value"), attrs[0].Data);
        }

        [TestMethod]
        public void SetAttribute_MultipleEntities()
        {
            _storageMock.Add(new Entity("test1"));
            _storageMock.Add(new Entity("test2"));
            _selectionMock.Replace(new[] { "test1", "test2" });

            var attrs = _attributes.GetSelectedAttributes().ToList();
            Assert.AreEqual(0, attrs.Count);

            _attributes.SetAttribute("attr", new StringAttribute("attr", "value"));

            attrs = _attributes.GetSelectedAttributes().ToList();
            Assert.AreEqual(1, attrs.Count);
            Assert.IsFalse(attrs[0].IsMixed);
            Assert.AreEqual(new StringAttribute("attr", "value"), attrs[0].Data);
        }

        [TestMethod]
        public void RemoveAttribute_NonExistentAttribute()
        {
            _storageMock.Add(new Entity("test"));
            _selectionMock.Replace(new[] { "test" });

            var attrs = _attributes.GetSelectedAttributes().ToList();
            Assert.AreEqual(0, attrs.Count);

            _attributes.RemoveAttribute("attr");

            attrs = _attributes.GetSelectedAttributes().ToList();
            Assert.AreEqual(0, attrs.Count);
        }

        [TestMethod]
        public void RemoveAttribute_MultipleEntitiesWithDifferentValue()
        {
            var entity1 = new Entity("test1").SetAttribute(new StringAttribute("attr", "value"));
            var entity2 = new Entity("test2").SetAttribute(new IntAttribute("attr", 42));
            _storageMock.Add(entity1);
            _storageMock.Add(entity2);
            _selectionMock.Replace(new[] { "test1", "test2" });

            var attrs = _attributes.GetSelectedAttributes().ToList();
            Assert.AreEqual(1, attrs.Count);
            Assert.IsTrue(attrs[0].IsMixed);

            _attributes.RemoveAttribute("attr");
            
            attrs = _attributes.GetSelectedAttributes().ToList();
            Assert.AreEqual(0, attrs.Count);
            
            var staged = _entitiesMock.ConsumeStaged().ToList();
            Assert.AreEqual(2, staged.Count);
            Assert.AreEqual(0, staged[0].ToList().Count);
            Assert.AreEqual(0, staged[1].ToList().Count);
        }
    }
}
