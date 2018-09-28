using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Query;
using Viewer.Query.Search;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.Query
{
    [TestClass]
    public class StatisticsTest
    {
        private Mock<IAttributeCache> _attributes;
        private Mock<IFileAggregate> _files;

        private List<string> CreateAttribute(params string[] names)
        {
            return names.ToList();
        }

        [TestInitialize]
        public void Setup()
        {
            _files = new Mock<IFileAggregate>();
            _attributes = new Mock<IAttributeCache>();
            _attributes.Setup(mock => mock.CreateFileAggregate()).Returns(_files.Object);
        }

        [TestMethod]
        public void Fetch_NoAttributeNames()
        {
            var statistics = Statistics.Fetch(_attributes.Object, new string[] { });
            var folder = statistics.GetDirectory("");
            Assert.AreEqual(1, folder.Count);
            Assert.AreEqual(0, folder[0]);
        }

        [TestMethod]
        public void Fetch_AggregateStatistics()
        {
            // subset indices (this depends on the order of attributes)
            const int empty = 0;
            const int A1 = 1;
            const int A2 = 2;

            var attributes = new List<AttributeGroup>
            {
                new AttributeGroup{ Attributes = CreateAttribute("a1"), FilePath = "a/a/b/f.jpg" },
                new AttributeGroup{ Attributes = CreateAttribute("a2"), FilePath = "a/a/f.jpg" },
                new AttributeGroup{ Attributes = CreateAttribute("a1"), FilePath = "a/b/c/f.jpg" },
                new AttributeGroup{ Attributes = CreateAttribute("a1"), FilePath = "a/b/f.jpg" },
                new AttributeGroup{ Attributes = CreateAttribute("a1"), FilePath = "a/b/f2.jpg" },
            };
            
            _attributes
                .Setup(mock => mock.GetAttributes(It.Is<IReadOnlyList<string>>(items =>
                    items.SequenceEqual(new[] {"a1", "a2"})
                )))
                .Returns(attributes);

            _files.Setup(mock => mock.Count("a/")).Returns(5);
            _files.Setup(mock => mock.Count("a/a/")).Returns(2);
            _files.Setup(mock => mock.Count("a/a/b/")).Returns(1);
            _files.Setup(mock => mock.Count("a/b/")).Returns(3);
            _files.Setup(mock => mock.Count("a/b/c/")).Returns(1);

            var statistics = Statistics.Fetch(_attributes.Object, new[] {"a1", "a2"});

            var root = statistics.GetDirectory("");
            Assert.AreEqual(3, root.Count);
            Assert.AreEqual(0, root[empty]);
            Assert.AreEqual(4, root[A1]);
            Assert.AreEqual(1, root[A2]);

            var node = statistics.GetDirectory("a");
            Assert.AreEqual(3, node.Count);
            Assert.AreEqual(0, node[empty]);
            Assert.AreEqual(4, node[A1]);
            Assert.AreEqual(1, node[A2]);

            node = statistics.GetDirectory("a/a");
            Assert.AreEqual(3, node.Count);
            Assert.AreEqual(0, node[empty]);
            Assert.AreEqual(1, node[A1]);
            Assert.AreEqual(1, node[A2]);

            node = statistics.GetDirectory("a/a/b");
            Assert.AreEqual(3, node.Count);
            Assert.AreEqual(0, node[empty]);
            Assert.AreEqual(1, node[A1]);
            Assert.AreEqual(0, node[A2]);

            node = statistics.GetDirectory("a/b");
            Assert.AreEqual(3, node.Count);
            Assert.AreEqual(0, node[empty]);
            Assert.AreEqual(3, node[A1]);
            Assert.AreEqual(0, node[A2]);

            node = statistics.GetDirectory("a/b/c");
            Assert.AreEqual(3, node.Count);
            Assert.AreEqual(0, node[empty]);
            Assert.AreEqual(1, node[A1]);
            Assert.AreEqual(0, node[A2]);
        }

        [TestMethod]
        public void Fetch_NotAllFilesHaveAttributes()
        {
            const int empty = 0;
            const int A1 = 1;

            var attributes = new List<AttributeGroup>
            {
                new AttributeGroup{ Attributes = CreateAttribute("a1"), FilePath = "a/a/f.jpg" },
                new AttributeGroup{ Attributes = CreateAttribute("a1"), FilePath = "a/b/f.jpg" },
                new AttributeGroup{ Attributes = CreateAttribute("a1"), FilePath = "a/f.jpg" },
            };
            
            _attributes
                .Setup(mock => mock.GetAttributes(It.Is<IReadOnlyList<string>>(items =>
                    items.SequenceEqual(new[] { "a1" })
                )))
                .Returns(attributes);
            _files.Setup(mock => mock.Count("a/")).Returns(7);
            _files.Setup(mock => mock.Count("a/a/")).Returns(1);
            _files.Setup(mock => mock.Count("a/b/")).Returns(3);

            var statistics = Statistics.Fetch(_attributes.Object, new[] { "a1"});

            var node = statistics.GetDirectory("");
            Assert.AreEqual(2, node.Count);
            Assert.AreEqual(4, node[empty]);
            Assert.AreEqual(3, node[A1]);

            node = statistics.GetDirectory("a");
            Assert.AreEqual(2, node.Count);
            Assert.AreEqual(4, node[empty]);
            Assert.AreEqual(3, node[A1]);

            node = statistics.GetDirectory("a/a");
            Assert.AreEqual(2, node.Count);
            Assert.AreEqual(0, node[empty]);
            Assert.AreEqual(1, node[A1]);

            node = statistics.GetDirectory("a/b");
            Assert.AreEqual(2, node.Count);
            Assert.AreEqual(2, node[empty]);
            Assert.AreEqual(1, node[A1]);
        }

        [TestMethod]
        public void Fetch_MultipleAttributesInOneFile()
        {
            // subset indices
            const int empty = 0;
            const int A1A2 = 1;
            const int A2 = 2;
            const int A1 = 3;

            var attributes = new List<AttributeGroup>
            {
                new AttributeGroup{ Attributes = CreateAttribute("a1", "a2"), FilePath = "a/a/f.jpg" },
                new AttributeGroup{ Attributes = CreateAttribute("a2"), FilePath = "a/b/f.jpg" },
                new AttributeGroup{ Attributes = CreateAttribute("a1"), FilePath = "a/f.jpg" },
            };
            
            _attributes
                .Setup(mock => mock.GetAttributes(It.Is<IReadOnlyList<string>>(items =>
                    items.SequenceEqual(new[] { "a1", "a2" })
                )))
                .Returns(attributes);
            _files.Setup(mock => mock.Count("a/")).Returns(4);
            _files.Setup(mock => mock.Count("a/a/")).Returns(1);
            _files.Setup(mock => mock.Count("a/b/")).Returns(2);

            var statistics = Statistics.Fetch(_attributes.Object, new[] { "a1", "a2" });
            
            var node = statistics.GetDirectory("a/a");
            Assert.AreEqual(4, node.Count);
            Assert.AreEqual(0, node[empty]);
            Assert.AreEqual(0, node[A1]);
            Assert.AreEqual(0, node[A2]);
            Assert.AreEqual(1, node[A1A2]);

            node = statistics.GetDirectory("a/b");
            Assert.AreEqual(4, node.Count);
            Assert.AreEqual(1, node[empty]);
            Assert.AreEqual(0, node[A1]);
            Assert.AreEqual(1, node[A2]);
            Assert.AreEqual(0, node[A1A2]);

            node = statistics.GetDirectory("a");
            Assert.AreEqual(4, node.Count);
            Assert.AreEqual(1, node[empty]);
            Assert.AreEqual(1, node[A1]);
            Assert.AreEqual(1, node[A2]);
            Assert.AreEqual(1, node[A1A2]);
        }
    }
}
