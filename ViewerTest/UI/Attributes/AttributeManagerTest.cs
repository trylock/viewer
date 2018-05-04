using System;
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
                .Setup(mock => mock.GetEnumerator())
                .Returns(new List<IEntity>().GetEnumerator());

            _attributes.SetAttribute("test", new Attribute("test", new StringValue("value")));

            _entityManager.Verify(mock => mock.SetEntity(It.IsAny<IEntity>()), Times.Never);
        }

        [TestMethod]
        public void SetAttribute_OneEntity()
        {
            var oldAttr = new Attribute("oldAttr", new IntValue(42));
            var newAttr = new Attribute("attr", new StringValue("value"));

            var entity = new Entity("test").SetAttribute(oldAttr);
            var selectedEntities = new List<IEntity>
            {
                entity
            };
            _selection
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());
            _selection
                .Setup(mock => mock.Count)
                .Returns(selectedEntities.Count);

            _attributes.SetAttribute(oldAttr.Name, newAttr);

            // we have marked the entity as changed
            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.GetAttribute(newAttr.Name).Equals(newAttr) &&
                    item.GetAttribute(oldAttr.Name) == null
                )
            ), Times.Once);
        }

        [TestMethod]
        public void SetAttribute_MultipleEntities()
        {
            var selectedEntities = new List<IEntity>
            {
                new Entity("test1"),
                new Entity("test2"),
            };

            _selection
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());
            _selection
                .Setup(mock => mock.Count)
                .Returns(selectedEntities.Count);

            var newAttr = new Attribute("attr", new StringValue("value"));
            _attributes.SetAttribute("attr", newAttr);

            // we have added the attribute to both entities
            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.Path == "test1" &&
                    item.GetAttribute(newAttr.Name).Equals(newAttr)
                )
            ), Times.Once);

            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.Path == "test2" &&
                    item.GetAttribute(newAttr.Name).Equals(newAttr)
                )
            ), Times.Once);
        }

        [TestMethod]
        public void RemoveAttribute_NonExistentAttribute()
        {
            var selectedEntities = new List<IEntity>
            {
                new Entity("test"),
            };

            _selection
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());
            _selection
                .Setup(mock => mock.Count)
                .Returns(selectedEntities.Count);
            
            _attributes.RemoveAttribute("attr");
        }

        [TestMethod]
        public void RemoveAttribute_MultipleEntitiesWithDifferentValue()
        {
            var entity1 = new Entity("test1").SetAttribute(new Attribute("attr", new StringValue("value")));
            var entity2 = new Entity("test2").SetAttribute(new Attribute("attr", new IntValue(42)));
            
            var selectedEntities = new List<IEntity>
            {
                entity1,
                entity2
            };

            _selection
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());
            _selection
                .Setup(mock => mock.Count)
                .Returns(selectedEntities.Count);

            _attributes.RemoveAttribute("attr");

            // we have removed the attribute
            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.Path == "test1" &&
                    item.GetAttribute("attr") == null
                )
            ), Times.Once);

            _entityManager.Verify(mock => mock.SetEntity(
                It.Is<IEntity>(item =>
                    item.Path == "test2" &&
                    item.GetAttribute("attr") == null
                )
            ), Times.Once);
        }
        
        [TestMethod]
        public void GetSelectedEntities_MissingAttributeOnAnEntity()
        {
            var entity1 = new Entity("test1").SetAttribute(new Attribute("attr", new IntValue(42)));
            var entity2 = new Entity("test2");

            var selectedEntities = new List<IEntity>
            {
                entity1,
                entity2
            };

            _selection
                .Setup(mock => mock.GetEnumerator())
                .Returns(selectedEntities.GetEnumerator());
            _selection
                .Setup(mock => mock.Count)
                .Returns(selectedEntities.Count);

            var attrs = _attributes.GroupAttributesInSelection().ToList();
            Assert.AreEqual(1, attrs.Count);
            Assert.IsFalse(attrs[0].IsMixed);
            Assert.IsFalse(attrs[0].IsGlobal);
        }
    }
}
