using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.UI.Attributes;
using ViewerTest.Data;

namespace ViewerTest.UI.Attributes
{
    [TestClass]
    public class AttributeManagerTest
    {
        private EntityManagerMock _entities;
        private SelectionMock _selectionMock;
        private AttributeManager _attributes;

        [TestInitialize]
        public void Setup()
        {
            _entities = new EntityManagerMock();
            _selectionMock = new SelectionMock();
            _attributes = new AttributeManager(_selectionMock);
        }

        [TestMethod]
        public void SetAttribute_EmptySelection()
        {
            _attributes.SetAttribute("test", new StringAttribute("test", "value"));

            var attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(0, attrs.Count);
        }

        [TestMethod]
        public void SetAttribute_OneEntity()
        {
            _entities.Add(new Entity("test"));
            _selectionMock.Replace(_entities, new[]{ 0 });
            
            var attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(0, attrs.Count);

            _attributes.SetAttribute("attr", new StringAttribute("attr", "value"));
            
            attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(1, attrs.Count);
            Assert.IsFalse(attrs[0].IsMixed);
            Assert.IsTrue(attrs[0].IsGlobal);
            Assert.AreEqual(new StringAttribute("attr", "value"), attrs[0].Data);
        }

        [TestMethod]
        public void SetAttribute_MultipleEntities()
        {
            _entities.Add(new Entity("test1"));
            _entities.Add(new Entity("test2"));
            _selectionMock.Replace(_entities, new[] { 0, 1 });

            var attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(0, attrs.Count);

            _attributes.SetAttribute("attr", new StringAttribute("attr", "value"));

            attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(1, attrs.Count);
            Assert.IsFalse(attrs[0].IsMixed);
            Assert.IsTrue(attrs[0].IsGlobal);
            Assert.AreEqual(new StringAttribute("attr", "value"), attrs[0].Data);
        }

        [TestMethod]
        public void RemoveAttribute_NonExistentAttribute()
        {
            _entities.Add(new Entity("test"));
            _selectionMock.Replace(_entities, new[] { 0 });

            var attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(0, attrs.Count);

            _attributes.RemoveAttribute("attr");

            attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(0, attrs.Count);
        }

        [TestMethod]
        public void RemoveAttribute_MultipleEntitiesWithDifferentValue()
        {
            var entity1 = new Entity("test1").SetAttribute(new StringAttribute("attr", "value"));
            var entity2 = new Entity("test2").SetAttribute(new IntAttribute("attr", 42));
            _entities.Add(entity1);
            _entities.Add(entity2);
            _selectionMock.Replace(_entities, new[] { 0, 1 });

            var attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(1, attrs.Count);
            Assert.IsTrue(attrs[0].IsMixed);
            Assert.IsTrue(attrs[0].IsGlobal);

            _attributes.RemoveAttribute("attr");
            
            attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(0, attrs.Count);
        }

        [TestMethod]
        public void GetSelectedEntities_MissingAttributeOnAnEntity()
        {
            var entity1 = new Entity("test1").SetAttribute(new IntAttribute("attr", 42));
            var entity2 = new Entity("test2");

            _entities.Add(entity1);
            _entities.Add(entity2);
            _selectionMock.Replace(_entities, new[]{ 0, 1 });

            var attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(1, attrs.Count);
            Assert.IsFalse(attrs[0].IsMixed);
            Assert.IsFalse(attrs[0].IsGlobal);
        }
    }
}
