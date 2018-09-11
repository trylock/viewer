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
        private bool ArePathsEqual(string first, string second)
        {
            first = PathUtils.NormalizePath(first);
            second = PathUtils.NormalizePath(second);
            return string.Equals(first, second, StringComparison.CurrentCultureIgnoreCase);
        }

        private bool ArePatternsEqual(string first, string second)
        {
            first = first.Replace('\\', '/').Trim().TrimEnd('/');
            second = second.Replace('\\', '/').Trim().TrimEnd('/');
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
        public void Match_GeneralPatternAtTheEnd()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "a/**");

            Assert.IsTrue(finder.Match("a"));
            Assert.IsTrue(finder.Match("a/"));
            Assert.IsTrue(finder.Match("a/b"));
            Assert.IsTrue(finder.Match("a/b/c"));
            Assert.IsFalse(finder.Match("b/a"));
            Assert.IsFalse(finder.Match("b"));
            Assert.IsFalse(finder.Match(""));
        }

        [TestMethod]
        public void Match_GeneralPatternAtTheStart()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "**/a");

            Assert.IsTrue(finder.Match("a"));
            Assert.IsTrue(finder.Match("a/"));
            Assert.IsFalse(finder.Match("a/b"));
            Assert.IsFalse(finder.Match("a/b/c"));
            Assert.IsTrue(finder.Match("b/a"));
            Assert.IsTrue(finder.Match("c/b/a"));
            Assert.IsFalse(finder.Match("b"));
            Assert.IsFalse(finder.Match(""));
        }

        [TestMethod]
        public void Match_MultipleGeneralPatterns()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "a/**/b/**/c");

            Assert.IsFalse(finder.Match(""));
            Assert.IsFalse(finder.Match("a"));
            Assert.IsFalse(finder.Match("b"));
            Assert.IsFalse(finder.Match("c"));
            Assert.IsFalse(finder.Match("a/b"));
            Assert.IsFalse(finder.Match("a/c"));
            Assert.IsFalse(finder.Match("b/c"));
            Assert.IsTrue(finder.Match("a/b/c"));
            Assert.IsTrue(finder.Match("a/x/b/c"));
            Assert.IsTrue(finder.Match("a/b/x/c"));
            Assert.IsTrue(finder.Match("a/x/b/y/c"));
            Assert.IsTrue(finder.Match("a/x/z/b/y/w/c"));
        }

        [TestMethod]
        public void Match_MultipleGeneralPatternsInSuccession()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "a/**/**/b");

            Assert.IsFalse(finder.Match("a"));
            Assert.IsFalse(finder.Match("b"));
            Assert.IsTrue(finder.Match("a/b"));
            Assert.IsFalse(finder.Match("a//b"));
            Assert.IsFalse(finder.Match(@"a\\b"));
        }

        [TestMethod]
        public void Match_JustTheGeneralPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "**");

            Assert.IsTrue(finder.Match(""));
            Assert.IsTrue(finder.Match("/"));
            Assert.IsTrue(finder.Match("a"));
            Assert.IsTrue(finder.Match("a/b"));
            Assert.IsTrue(finder.Match("a/b/c"));
            Assert.IsTrue(finder.Match("c/a/b"));
        }

        [TestMethod]
        public void Match_JustTheStarPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "*");

            Assert.IsTrue(finder.Match(""));
            Assert.IsTrue(finder.Match("/"));
            Assert.IsTrue(finder.Match("a"));
            Assert.IsTrue(finder.Match("a/"));
            Assert.IsFalse(finder.Match("a/b"));
            Assert.IsFalse(finder.Match("a/b/c"));
            Assert.IsFalse(finder.Match("c/a/b"));
        }
        [TestMethod]
        public void Match_JustTheQuestionMarkPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            var finder = new FileFinder(fileSystem.Object, "?");

            Assert.IsFalse(finder.Match(""));
            Assert.IsFalse(finder.Match("/"));
            Assert.IsTrue(finder.Match("a"));
            Assert.IsTrue(finder.Match("b"));
            Assert.IsTrue(finder.Match("a/"));
            Assert.IsFalse(finder.Match("ab"));
            Assert.IsFalse(finder.Match("a/b"));
            Assert.IsFalse(finder.Match("a/b/c"));
            Assert.IsFalse(finder.Match("c/a/b"));
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
        public void GetBasePatternPath_NullPath()
        {
            Assert.IsNull(FileFinder.GetBasePatternPath(null));
        }

        [TestMethod]
        public void GetBasePatternPath_EmptyPath()
        {
            Assert.IsNull(FileFinder.GetBasePatternPath(""));
        }

        [TestMethod]
        public void GetBasePatternPath_OneDirectory()
        {
            Assert.IsTrue(ArePathsEqual("dir", FileFinder.GetBasePatternPath("dir")));
        }

        [TestMethod]
        public void GetBasePatternPath_OnePattern()
        {
            Assert.IsNull(FileFinder.GetBasePatternPath("d?r"));
        }

        [TestMethod]
        public void GetBasePatternPath_MultipleDirectoriesWithoutPattern()
        {
            Assert.IsTrue(ArePathsEqual("a/b/c", FileFinder.GetBasePatternPath("a/b/c")));
        }
        
        [TestMethod]
        public void GetBasePatternPath_MultipleDirectoriesWithPattern()
        {
            Assert.IsTrue(ArePathsEqual("a/b", FileFinder.GetBasePatternPath("a/b/c*")));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_NullPattern()
        {
            Assert.IsNull(FileFinder.GetParentDirectoryPattern(null));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_RootFolderPattern()
        {
            Assert.IsTrue(ArePatternsEqual("C:/", FileFinder.GetParentDirectoryPattern("C:")));
            Assert.IsTrue(ArePatternsEqual("C:/", FileFinder.GetParentDirectoryPattern("C:/")));
            Assert.IsTrue(ArePatternsEqual("C:/", FileFinder.GetParentDirectoryPattern("C:\\")));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_SimplePath()
        {
            Assert.IsTrue(ArePatternsEqual("a/b", FileFinder.GetParentDirectoryPattern("a/b/c")));
            Assert.IsTrue(ArePatternsEqual("a/b", FileFinder.GetParentDirectoryPattern("a/b/c/")));
            Assert.IsTrue(ArePatternsEqual("a/b", FileFinder.GetParentDirectoryPattern("a/b\\c\\")));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_PatternInTheMiddle()
        {
            Assert.IsTrue(ArePatternsEqual("a/**", FileFinder.GetParentDirectoryPattern("a/**/b")));
            Assert.IsTrue(ArePatternsEqual("a/x*", FileFinder.GetParentDirectoryPattern("a/x*/b")));
            Assert.IsTrue(ArePatternsEqual("a/x?y", FileFinder.GetParentDirectoryPattern("a/x?y/b")));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_NonRecursivePatternAtTheEnd()
        {
            Assert.IsTrue(ArePatternsEqual("a/**", FileFinder.GetParentDirectoryPattern("a/**/x*")));
            Assert.IsTrue(ArePatternsEqual("a/x*", FileFinder.GetParentDirectoryPattern("a/x*/y*")));
            Assert.IsTrue(ArePatternsEqual("a/x?y", FileFinder.GetParentDirectoryPattern("a/x?y/z?")));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_RecursivePatternAtTheEnd()
        {
            Assert.IsTrue(ArePatternsEqual("**/..", FileFinder.GetParentDirectoryPattern("**")));
            Assert.IsTrue(ArePatternsEqual("a/**/..", FileFinder.GetParentDirectoryPattern("a/**")));
            Assert.IsTrue(ArePatternsEqual("a/**/..", FileFinder.GetParentDirectoryPattern("a\\**")));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_ParrentDirectoryAtTheEnd()
        {
            Assert.IsTrue(ArePatternsEqual("**/../..", FileFinder.GetParentDirectoryPattern("**/..")));
            Assert.IsTrue(ArePatternsEqual("**/../..", FileFinder.GetParentDirectoryPattern("**/../")));
            Assert.IsTrue(ArePatternsEqual("**/../..", FileFinder.GetParentDirectoryPattern("**\\..\\")));
        }
    }
}
