using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.IO;

namespace ViewerTest.IO
{
    [TestClass]
    public class FileFinderTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FileFinder_InvalidCharactersInPathPattern()
        {
            var finder = new FileFinder(new Mock<IFileSystem>().Object, "C:/test <");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FileFinder_NullPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetDirectories_NullFileSystem()
        {
            var finder = new FileFinder(null, "");
        }

        [TestMethod]
        public void Match_EmptyPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "");

            Assert.IsTrue(finder.Match(""));
            Assert.IsFalse(finder.Match("a"));
        }

        [TestMethod]
        public void Match_PathWithoutPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "C:/directory/a/b/c");

            Assert.IsFalse(finder.Match("C:/"));
            Assert.IsFalse(finder.Match("C:/directory"));
            Assert.IsFalse(finder.Match("C:/directory/a"));
            Assert.IsFalse(finder.Match("C:/directory/a/b"));
            Assert.IsTrue(finder.Match("C:/directory/a/b/c"));
            Assert.IsTrue(finder.Match("C:/directory/a/b/c/"));
            Assert.IsTrue(finder.Match("C:\\directory\\a\\b\\c"));
            Assert.IsTrue(finder.Match("C:\\directory\\a\\b\\c\\"));
        }

        [TestMethod]
        public void Match_PathWithOneAsteriskPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "C:/a/b*/c");

            Assert.IsFalse(finder.Match("C:/a"));
            Assert.IsFalse(finder.Match("C:/a/c"));
            Assert.IsFalse(finder.Match("C:/a/x/c"));
            Assert.IsTrue(finder.Match("C:/a/b/c"));
            Assert.IsTrue(finder.Match("C:/a\\b/c\\"));
            Assert.IsTrue(finder.Match("C:/a/bx/c"));
            Assert.IsTrue(finder.Match("C:/a/bxy/c"));
            Assert.IsTrue(finder.Match("C:/a/bxyz/c"));
        }

        [TestMethod]
        public void Match_PathWithOneQuestionMarkPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "C:/a/b?/c");

            Assert.IsFalse(finder.Match("C:/a"));
            Assert.IsFalse(finder.Match("C:/a/c"));
            Assert.IsFalse(finder.Match("C:/a/x/c"));
            Assert.IsFalse(finder.Match("C:/a/b/c"));
            Assert.IsTrue(finder.Match("C:/a/bx/c"));
            Assert.IsTrue(finder.Match("C:\\a\\bx/c\\"));
            Assert.IsFalse(finder.Match("C:/a/bxy/c"));
            Assert.IsFalse(finder.Match("C:/a/bxyz/c"));
        }

        [TestMethod]
        public void Match_PathWithGeneralPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "C:/a/**/c");

            Assert.IsFalse(finder.Match("C:/a"));
            Assert.IsTrue(finder.Match("C:/a/c"));
            Assert.IsFalse(finder.Match("C:/ac"));
            Assert.IsFalse(finder.Match("C:/a/bc"));
            Assert.IsFalse(finder.Match("C:/ab/c"));
            Assert.IsTrue(finder.Match("C:/a/x/c"));
            Assert.IsTrue(finder.Match("C:/a/x/y/c"));
            Assert.IsFalse(finder.Match("C:/a/x/y"));
        }

        [TestMethod]
        public void Match_CombinedPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "C:/a/**/b*/c");

            Assert.IsFalse(finder.Match("C:/a"));
            Assert.IsFalse(finder.Match("C:/a/c"));
            Assert.IsTrue(finder.Match("C:/a/b/c"));
            Assert.IsTrue(finder.Match("C:/a/bx/c"));
            Assert.IsTrue(finder.Match("C:/a/bxy/c"));
            Assert.IsTrue(finder.Match("C:/a/x/b/c"));
            Assert.IsFalse(finder.Match("C:/a/x/b"));
        }

        [TestMethod]
        public void GetDirectories_EmptyPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "");
            var directories = finder.GetDirectories().ToArray();
            Assert.AreEqual(0, directories.Length);
        }

        [TestMethod]
        public void GetDirectories_PathWithoutPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\directory\\a\\b\\c\\")).Returns(true);
            var finder = new FileFinder(fileSystem.Object, "C:/directory/a/b/c");
            var directories = finder.GetDirectories().ToArray();
            Assert.AreEqual(1, directories.Length);
            Assert.AreEqual("C:\\directory\\a\\b\\c\\", directories[0]);
        }

        [TestMethod]
        public void GetDirectories_PathWithoutPatternAndNonexistentDirectory()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\directory\\a\\b\\c")).Returns(false);

            var finder = new FileFinder(fileSystem.Object, "C:/directory/a/b/c");
            var directories = finder.GetDirectories().ToArray();
            Assert.AreEqual(0, directories.Length);
        }

        [TestMethod]
        public void GetDirectories_PathWithOneAsteriskPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\")).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateDirectories("C:\\a\\", "b*")).Returns(new[]{ "C:\\a\\ba", "C:\\a\\bba" });
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\bba\\c\\")).Returns(false);
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\ba\\c\\")).Returns(true);

            var finder = new FileFinder(fileSystem.Object, "C:/a/b*/c");
            var directories = finder.GetDirectories().ToArray();
            Assert.AreEqual(1, directories.Length);
            Assert.AreEqual("C:\\a\\ba\\c\\", directories[0]);
        }

        [TestMethod]
        public void GetDirectories_PathWithOneQuestionMarkPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\")).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateDirectories("C:\\a\\", "b?")).Returns(new[] { "C:\\a\\ba", "C:\\a\\bc" });
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\ba\\c\\")).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\bc\\c\\")).Returns(true);

            var finder = new FileFinder(fileSystem.Object, "C:/a/b?/c");
            var directories = finder.GetDirectories().OrderBy(item => item).ToArray();
            Assert.AreEqual(2, directories.Length);
            Assert.AreEqual("C:\\a\\ba\\c\\", directories[0]);
            Assert.AreEqual("C:\\a\\bc\\c\\", directories[1]);
        }

        [TestMethod]
        public void GetDirectories_PathWithGeneralPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\")).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateDirectories("C:\\a\\")).Returns(new[] { "C:\\a\\b", "C:\\a\\c", "C:\\a\\d" });
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\b\\b\\")).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\c\\b\\")).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\d\\b\\")).Returns(false);
            fileSystem.Setup(mock => mock.EnumerateDirectories("C:\\a\\b\\b\\", "*")).Returns(new[] { "C:\\a\\b\\b\\x" });
            fileSystem.Setup(mock => mock.EnumerateDirectories("C:\\a\\c\\b\\", "*")).Returns(new[] { "C:\\a\\c\\b\\x" });
            fileSystem.Setup(mock => mock.EnumerateDirectories("C:\\a\\d")).Returns(new[] { "C:\\a\\d\\e" });
            fileSystem.Setup(mock => mock.DirectoryExists("C:\\a\\d\\e\\b\\")).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateDirectories("C:\\a\\d\\e\\b\\", "*")).Returns(new[] { "C:\\a\\d\\e\\b\\x" });

            var finder = new FileFinder(fileSystem.Object, "C:/a/**/b/*");
            var directories = finder.GetDirectories().OrderBy(item => item).ToArray();
            Assert.AreEqual(3, directories.Length);
            Assert.AreEqual("C:\\a\\b\\b\\x", directories[0]);
            Assert.AreEqual("C:\\a\\c\\b\\x", directories[1]);
            Assert.AreEqual("C:\\a\\d\\e\\b\\x", directories[2]);
        }

        [TestMethod]
        public void GetDirectories_GeneralPatternMatchesEmptyString()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists("a\\")).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists("a\\b\\c\\")).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists("a\\x\\b\\c\\")).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists("a\\x\\y\\b\\c\\")).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateDirectories("a\\")).Returns(new[]{ "a\\b", "a\\x" });
            fileSystem.Setup(mock => mock.EnumerateDirectories("a\\x")).Returns(new[] { "a\\x\\y" });

            var finder = new FileFinder(fileSystem.Object, "a/**/b/c");
            var directories = finder.GetDirectories().OrderBy(item => item).ToArray();

            Assert.AreEqual(3, directories.Length);
            Assert.AreEqual("a\\b\\c\\", directories[0]);
            Assert.AreEqual("a\\x\\b\\c\\", directories[1]);
            Assert.AreEqual("a\\x\\y\\b\\c\\", directories[2]);
        }
    }
}
