using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Query;

namespace ViewerTest.Query
{
    [TestClass]
    public class StatisticsTest
    {
        private Mock<IAttributeCache> _attributes;

        [TestInitialize]
        public void Setup()
        {
            _attributes = new Mock<IAttributeCache>();
        }

        [TestMethod]
        public void Fetch_NoAttributeNames()
        {
            var statistics = Statistics.Fetch(_attributes.Object, new string[] { });
            var folder = statistics.GetDirectory("");
            Assert.AreEqual(0, folder.Count);
        }

        [TestMethod]
        public void Fetch_AggregateStatistics()
        {
            var attributes = new List<AttributeLocation>
            {
                new AttributeLocation{ AttributeName = "a2", FilePath = "a/a/f.jpg" },
                new AttributeLocation{ AttributeName = "a1", FilePath = "a/a/b/f.jpg" },
                new AttributeLocation{ AttributeName = "a1", FilePath = "a/b/c/f.jpg" },
                new AttributeLocation{ AttributeName = "a1", FilePath = "a/b/f.jpg" },
                new AttributeLocation{ AttributeName = "a1", FilePath = "a/b/f2.jpg" },
            };

            var files = attributes.Select(item => item.FilePath).ToList();

            _attributes
                .Setup(mock => mock.GetAttributes(It.Is<IEnumerable<string>>(items =>
                    items.SequenceEqual(new[] {"a1", "a2"})
                )))
                .Returns(attributes);
            _attributes
                .Setup(mock => mock.GetFiles())
                .Returns(files);

            var statistics = Statistics.Fetch(_attributes.Object, new[] {"a1", "a2"});

            var root = statistics.GetDirectory("");
            Assert.AreEqual(5, root.Count);
            Assert.AreEqual(4, root.GetAttributeCount("a1"));
            Assert.AreEqual(1, root.GetAttributeCount("a2"));

            var node = statistics.GetDirectory("a");
            Assert.AreEqual(5, node.Count);
            Assert.AreEqual(4, node.GetAttributeCount("a1"));
            Assert.AreEqual(1, node.GetAttributeCount("a2"));

            node = statistics.GetDirectory("a/a");
            Assert.AreEqual(2, node.Count);
            Assert.AreEqual(1, node.GetAttributeCount("a1"));
            Assert.AreEqual(1, node.GetAttributeCount("a2"));

            node = statistics.GetDirectory("a/a/b");
            Assert.AreEqual(1, node.Count);
            Assert.AreEqual(1, node.GetAttributeCount("a1"));
            Assert.AreEqual(0, node.GetAttributeCount("a2"));

            node = statistics.GetDirectory("a/b");
            Assert.AreEqual(3, node.Count);
            Assert.AreEqual(3, node.GetAttributeCount("a1"));
            Assert.AreEqual(0, node.GetAttributeCount("a2"));

            node = statistics.GetDirectory("a/b/c");
            Assert.AreEqual(1, node.Count);
            Assert.AreEqual(1, node.GetAttributeCount("a1"));
            Assert.AreEqual(0, node.GetAttributeCount("a2"));
        }

        [TestMethod]
        public void Fetch_NotAllFilesHaveAttributes()
        {
            var attributes = new List<AttributeLocation>
            {
                new AttributeLocation{ AttributeName = "a1", FilePath = "a/a/f.jpg" },
                new AttributeLocation{ AttributeName = "a1", FilePath = "a/b/f.jpg" },
                new AttributeLocation{ AttributeName = "a1", FilePath = "a/f.jpg" },
            };

            var files = attributes.Select(item => item.FilePath).ToList();
            files.Add("a/b/f2.jpg");
            files.Add("a/b/f3.jpg");
            files.Add("a/f2.jpg");
            files.Add("a/c/f.jpg");

            _attributes
                .Setup(mock => mock.GetAttributes(It.Is<IEnumerable<string>>(items =>
                    items.SequenceEqual(new[] { "a1" })
                )))
                .Returns(attributes);
            _attributes
                .Setup(mock => mock.GetFiles())
                .Returns(files);

            var statistics = Statistics.Fetch(_attributes.Object, new[] { "a1"});

            var node = statistics.GetDirectory("");
            Assert.AreEqual(7, node.Count);
            Assert.AreEqual(3, node.GetAttributeCount("a1"));

            node = statistics.GetDirectory("a");
            Assert.AreEqual(7, node.Count);
            Assert.AreEqual(3, node.GetAttributeCount("a1"));

            node = statistics.GetDirectory("a/a");
            Assert.AreEqual(1, node.Count);
            Assert.AreEqual(1, node.GetAttributeCount("a1"));

            node = statistics.GetDirectory("a/b");
            Assert.AreEqual(3, node.Count);
            Assert.AreEqual(1, node.GetAttributeCount("a1"));

            node = statistics.GetDirectory("a/c");
            Assert.IsNull(node);
        }
    }
}
