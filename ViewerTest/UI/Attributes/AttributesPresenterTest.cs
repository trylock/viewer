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
    public class AttributesPresenterTest
    {
        private IEntityManager _entitiesMock;
        private SelectionMock _selectionMock;
        private AttributeViewMock _attrViewMock;
        private AttributeManagerMock _attributeManagerMock;
        private AttributesPresenter _presenter;

        [TestInitialize]
        public void Setup()
        {
            var storage = new MemoryAttributeStorage();
            storage.Add(new Entity("test"));
            _entitiesMock = new EntityManager(storage);
            _selectionMock = new SelectionMock();
            _attrViewMock = new AttributeViewMock();
            _attributeManagerMock = new AttributeManagerMock();
            _presenter = new AttributesPresenter(_attrViewMock, null, _selectionMock, _entitiesMock, _attributeManagerMock);
        }

        [TestMethod]
        public void AttributesChanged_DontAddAttributeIfItsNameIsEmpty()
        {
            // add an entity to selection
            _selectionMock.Replace(new[] { "test" });
            _selectionMock.TriggerChanged();

            // add a new attribute
            var oldValue = new AttributeView
            {
                Data = new StringAttribute("", ""),
                IsMixed = false
            };
            var newValue = new AttributeView
            {
                Data = new StringAttribute("", "value")
            };
            _attrViewMock.TriggerAttributeChanges(0, oldValue, newValue);

            Assert.IsTrue(_attrViewMock.Updated.Contains(0));
            Assert.IsNull(_attrViewMock.LastError);
            Assert.AreEqual(1, _attrViewMock.Attributes.Count);
            Assert.AreEqual(
                new StringAttribute("", ""),
                _attrViewMock.Attributes[0].Data);
        }

        [TestMethod]
        public void AttributesChanged_AddANewAttribute()
        {
            // add an entity to selection
            _selectionMock.Replace(new[]{ "test" });
            _selectionMock.TriggerChanged();

            // add a new attribute
            var oldValue = new AttributeView
            {
                Data = new StringAttribute("",""),
                IsMixed = false
            };
            var newValue = new AttributeView
            {
                Data = new StringAttribute("test", "value")
            };
            _attrViewMock.TriggerAttributeChanges(0, oldValue, newValue);

            Assert.IsTrue(_attrViewMock.Updated.Contains(0));
            Assert.IsTrue(_attrViewMock.Updated.Contains(1));
            Assert.IsNull(_attrViewMock.LastError);
            Assert.AreEqual(2, _attrViewMock.Attributes.Count);
            Assert.AreEqual(
                new StringAttribute("test", "value"), 
                _attrViewMock.Attributes[0].Data);
            Assert.AreEqual(
                new StringAttribute("", ""), 
                _attrViewMock.Attributes[1].Data);
        }

        [TestMethod]
        public void AttributesChanged_NonUniqueName()
        {
            var attr1 = new AttributeView
            {
                Data = new StringAttribute("test", "value"),
                IsMixed = false
            };
            var attr2 = new AttributeView
            {
                Data = new IntAttribute("test2", 42),
                IsMixed = false
            };
            var attr3 = new AttributeView
            {
                Data = new StringAttribute("", ""),
                IsMixed = false
            };

            // add some default attributes
            _attributeManagerMock.SetAttribute(attr1.Data.Name, attr1.Data);
            _attributeManagerMock.SetAttribute(attr2.Data.Name, attr2.Data);
            _attrViewMock.Attributes.Add(attr1);
            _attrViewMock.Attributes.Add(attr2);
            _attrViewMock.Attributes.Add(attr3);

            // add attribute with an existing name
            _attrViewMock.TriggerAttributeChanges(2, _attrViewMock.Attributes[2], new AttributeView
            {
                Data = new StringAttribute("test2", "value2"),
                IsMixed = false
            });
            
            Assert.IsTrue(_attrViewMock.Updated.Contains(2));
            Assert.AreEqual("not unique: test2", _attrViewMock.LastError);
            CollectionAssert.AreEqual(new[]{ attr1, attr2, attr3 }, _attrViewMock.Attributes.ToArray());
        }
    }
}
