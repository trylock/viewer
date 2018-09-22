using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.IO;

namespace ViewerTest.IO
{
    [TestClass]
    public class FileFinderTest
    {
        private bool ArePathsEqual(string first, string second)
        {
            first = PathUtils.NormalizePath(first);
            second = PathUtils.NormalizePath(second);
            return string.Equals(first, second, StringComparison.CurrentCultureIgnoreCase);
        }

        private string ItIsPath(string expectedValue)
        {
            return It.Is<string>(actualValue => ArePathsEqual(actualValue, expectedValue));
        }
        
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
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:\\a\\"))).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateDirectories(ItIsPath("C:\\a\\"), "b*")).Returns(new[]{ "C:\\a\\ba", "C:\\a\\bba" });
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:\\a\\bba\\c\\"))).Returns(false);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:\\a\\ba\\c\\"))).Returns(true);

            var finder = new FileFinder(fileSystem.Object, "C:/a/b*/c");
            var directories = finder.GetDirectories().ToArray();
            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:\\a\\ba\\c\\", directories[0]));
        }

        [TestMethod]
        public void GetDirectories_PathWithOneQuestionMarkPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:\\a\\"))).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateDirectories(ItIsPath("C:\\a\\"), "b?")).Returns(new[] { "C:\\a\\ba", "C:\\a\\bc" });
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:\\a\\ba\\c\\"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:\\a\\bc\\c\\"))).Returns(true);

            var finder = new FileFinder(fileSystem.Object, "C:/a/b?/c");
            var directories = finder.GetDirectories().OrderBy(item => item).ToArray();
            Assert.AreEqual(2, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:\\a\\ba\\c\\", directories[0]));
            Assert.IsTrue(ArePathsEqual("C:\\a\\bc\\c\\", directories[1]));
        }

        [TestMethod]
        public void GetDirectories_PathWithGeneralPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:/a"))).Returns(true);
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("C:/a")))
                .Returns(new[] { "C:/a/b", "C:/a/c", "C:/a/d" });
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:/a/b/b"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:/a/c/b"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:/a/d/b"))).Returns(false);
            fileSystem.Setup(mock => mock.EnumerateDirectories(ItIsPath("C:/a/b/b"), "*")).Returns(new[] { "C:/a/b/b/x" });
            fileSystem.Setup(mock => mock.EnumerateDirectories(ItIsPath("C:/a/c/b"), "*")).Returns(new[] { "C:/a/c/b/x" });
            fileSystem.Setup(mock => mock.EnumerateDirectories(ItIsPath("C:/a/d"))).Returns(new[] { "C:/a/d/e" });
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:/a/d/e/b"))).Returns(true);
            fileSystem.Setup(mock => mock.EnumerateDirectories(ItIsPath("C:/a/d/e/b"), "*")).Returns(new[] { "C:/a/d/e/b/x" });

            var finder = new FileFinder(fileSystem.Object, "C:/a/**/b/*");
            var directories = finder.GetDirectories().OrderBy(item => item).ToArray();
            Assert.AreEqual(3, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:/a/b/b/x", directories[0]));
            Assert.IsTrue(ArePathsEqual("C:/a/c/b/x", directories[1]));
            Assert.IsTrue(ArePathsEqual("C:/a/d/e/b/x", directories[2]));
        }

        [TestMethod]
        public void GetDirectories_GeneralPatternMatchesEmptyString()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a/b/c"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a/x/b/c"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a/x/y/b/c"))).Returns(true);
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("a")))
                .Returns(new[]{ "a/b", "a/x" });
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("a/x")))
                .Returns(new[] { "a/x/y" });

            var finder = new FileFinder(fileSystem.Object, "a/**/b/c");
            var directories = finder.GetDirectories().OrderBy(item => item).ToArray();

            Assert.AreEqual(3, directories.Length);
            Assert.IsTrue(ArePathsEqual("a/b/c", directories[0]));
            Assert.IsTrue(ArePathsEqual("a/x/b/c", directories[1]));
            Assert.IsTrue(ArePathsEqual("a/x/y/b/c", directories[2]));
        }

        [TestMethod]
        public void GetDirectories_IgnoreSystemDirectories()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a/x"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a/y"))).Returns(true);
            fileSystem.Setup(mock => mock
                .EnumerateDirectories(ItIsPath("a"), "*"))
                .Returns(new[] { "a/x", "a/y" });
            fileSystem
                .Setup(mock => mock.GetAttributes(ItIsPath("a")))
                .Returns(FileAttributes.Directory);
            fileSystem
                .Setup(mock => mock.GetAttributes(ItIsPath("a/x")))
                .Returns(FileAttributes.Directory | FileAttributes.System);
            fileSystem
                .Setup(mock => mock.GetAttributes(ItIsPath("a/y")))
                .Returns(FileAttributes.Directory);

            var finder = new FileFinder(fileSystem.Object, "a/*");
            var directories = finder.GetDirectories().ToArray();

            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(ArePathsEqual("a/y", directories[0]));
        }

        [TestMethod]
        public void GetDirectories_ReturnEachDirectoryExactlyOnce()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a/b"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a/b/b"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("a/b/b/c"))).Returns(true);
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("a")))
                .Returns(new[]{ "a/b" });
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("a/b")))
                .Returns(new[] { "a/b/b" });
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("a/b/b")))
                .Returns(new[] { "a/b/b/c" });
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("a/b/b/c")))
                .Returns(new string[] {});

            var finder = new FileFinder(fileSystem.Object, "a/**/b/**/c");
            var directories = finder.GetDirectories().ToArray();

            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(ArePathsEqual("a/b/b/c", directories[0]));
        }
        
        [TestMethod]
        public void GetDirectories_SuffixDirectoriesWithPathSeparator()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("C:/"))).Returns(true);
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("C:/"), "*"))
                .Returns(new[] { "C:/a" });

            var finder = new FileFinder(fileSystem.Object, "C:/*");
            var directories = finder.GetDirectories().ToArray();

            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:/a", directories[0]));

            // Verify that there is a directory separator after "C:" Otherwise, C: would be a relative path
            fileSystem.Verify(mock => mock.EnumerateDirectories("C:/", "*"));
        }

        private class DescComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return -StringComparer.CurrentCultureIgnoreCase.Compare(x, y);
            }
        }

        [TestMethod]
        public void GetDirectories_TraverseFilesInCorrectOrder()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("r"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("r/a"))).Returns(true);
            fileSystem.Setup(mock => mock.DirectoryExists(ItIsPath("r/b"))).Returns(true);
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("r")))
                .Returns(new[] { "r/a", "r/b" });
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("r/a")))
                .Returns(new[] { "r/a/a", "r/a/b", "r/a/c" });
            fileSystem
                .Setup(mock => mock.EnumerateDirectories(ItIsPath("r/b")))
                .Returns(new[] { "r/b/a", "r/b/b", "r/b/c" });

            var finder = new FileFinder(fileSystem.Object, "r/**");
            var directories = finder.GetDirectories(CancellationToken.None, new DescComparer()).ToArray();

            var expectedDirectories = new[]
            {
                "r",
                "r/b", "r/a",
                "r/b/c", "r/b/b", "r/b/a",
                "r/a/c", "r/a/b", "r/a/a",
            };
            Assert.AreEqual(expectedDirectories.Length, directories.Length);

            for (var i = 0; i < expectedDirectories.Length; ++i)
            {
                var expectedPath = expectedDirectories[i];
                var actualPath = directories[i];
                var areEqual = ArePathsEqual(expectedPath, actualPath);
                Assert.IsTrue(areEqual);
            }
        }
    }
}
