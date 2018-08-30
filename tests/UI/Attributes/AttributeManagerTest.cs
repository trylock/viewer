using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.UI;
using Viewer.UI.Attributes;
using ViewerTest.Data;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.UI.Attributes
{
    [TestClass]
    public class AttributeManagerTest
    {
        private Mock<ISelection> _selection;
        private Mock<IEntityManager> _entityManager;
        private AttributeManager _attributes;

        [TestInitialize]
        public void Setup()
        {
            _selection = new Mock<ISelection>();
            _entityManager = new Mock<IEntityManager>();
            _attributes = new AttributeManager(_selection.Object, _entityManager.Object);
        }

        [TestMethod]
        public void SetAttribute_EmptySelection()
        {
            _selection
                .As<IEnumerable>()
                .Setup(mock => mock.GetEnumerator())
                .Returns(new List<IEntity>().GetEnumerator());

            _attributes.SetAttribute("test", new Attribute("test", new StringValue("value"), AttributeSource.Custom));

            _entityManager.Verify(mock => mock.SetEntity(It.IsAny<IEntity>(), true), Times.Never);
        }

        [TestMethod]
        public void SetAttribute_OneEntity()
        {
            var oldAttr = new Attribute("oldAttr", new IntValue(42), AttributeSource.Custom);
            var newAttr = new Attribute("attr", new StringValue("value"), AttributeSource.Custom);

            var entity = new FileEntity("test").SetAttribute(oldAttr);
            var selectedEntities = new List<IEntity>
            {
                entity
            };
            _selection
                .As<IEnumerable>()
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());

            _attributes.SetAttribute(oldAttr.Name, newAttr);

            // we have marked the entity as changed
            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.GetAttribute(newAttr.Name).Equals(newAttr) &&
                    item.GetAttribute(oldAttr.Name) == null
                ), true
            ), Times.Once);
        }

        [TestMethod]
        public void SetAttribute_MultipleEntities()
        {
            var selectedEntities = new List<IEntity>
            {
                new FileEntity("test1"),
                new FileEntity("test2"),
            };

            _selection
                .As<IEnumerable>()
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());

            var newAttr = new Attribute("attr", new StringValue("value"), AttributeSource.Custom);
            _attributes.SetAttribute("attr", newAttr);

            // we have added the attribute to both entities
            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.Path == selectedEntities[0].Path &&
                    item.GetAttribute(newAttr.Name).Equals(newAttr)
                ), true
            ), Times.Once);

            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.Path == selectedEntities[1].Path &&
                    item.GetAttribute(newAttr.Name).Equals(newAttr)
                ), true
            ), Times.Once);
        }

        [TestMethod]
        public void RemoveAttribute_NonExistentAttribute()
        {
            var selectedEntities = new List<IEntity>
            {
                new FileEntity("test"),
            };

            _selection
                .As<IEnumerable>()
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());
            
            _attributes.RemoveAttribute("attr");
        }

        [TestMethod]
        public void RemoveAttribute_MultipleEntitiesWithDifferentValue()
        {
            var entity1 = new FileEntity("test1").SetAttribute(new Attribute("attr", new StringValue("value"), AttributeSource.Custom));
            var entity2 = new FileEntity("test2").SetAttribute(new Attribute("attr", new IntValue(42), AttributeSource.Custom));
            var entity1Path = entity1.Path;
            var entity2Path = entity2.Path;

            var selectedEntities = new List<IEntity>
            {
                entity1,
                entity2
            };

            _selection
                .As<IEnumerable>()
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());

            _attributes.RemoveAttribute("attr");

            // we have removed the attribute
            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.Path == entity1Path &&
                    item.GetAttribute("attr") == null
                ), true
            ), Times.Once);

            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.Path == entity2Path &&
                    item.GetAttribute("attr") == null
                ), true
            ), Times.Once);
        }
        
        [TestMethod]
        public void GetSelectedEntities_MissingAttributeOnAnEntity()
        {
            var entity1 = new FileEntity("test1").SetAttribute(new Attribute("attr", new IntValue(42), AttributeSource.Custom));
            var entity2 = new FileEntity("test2");

            var selectedEntities = new List<IEntity>
            {
                entity1,
                entity2
            };

            _selection
                .As<IEnumerable>()
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());

            var attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(1, attrs.Count);
            Assert.IsFalse(attrs[0].HasMultipleValues);
            Assert.IsFalse(attrs[0].IsInAllEntities);
        }
    }
}
