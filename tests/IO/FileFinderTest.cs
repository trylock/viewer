using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private string Normalize(string path)
        {
            if (!path.StartsWith("C:"))
            {
                path = $"C:/{path}";
            }

            return path.Replace('\\', '/').Trim('/');
        }

        private bool ArePathsEqual(string first, string second)
        {
            first = Normalize(first);
            second = Normalize(second);
            return string.Equals(first, second, StringComparison.CurrentCultureIgnoreCase);
        }

        private string ItIsPath(string expectedValue)
        {
            return It.Is<string>(actualValue => ArePathsEqual(actualValue, expectedValue));
        }

        private class Node
        {
            public string Name;
            public string Path;
            public List<Node> Children = new List<Node>();

            public Node(string path)
            {
                Path = path;
                var lastSlash = Path.LastIndexOf('/');
                var length = Path.Length - lastSlash - 1;
                if (lastSlash < 0)
                {
                    length = Path.Length;
                }
                Name = Path.Substring(lastSlash + 1, length);
            }
        }

        /// <summary>
        /// Setup <paramref name="mock"/> so that it contains all directories in
        /// <paramref name="dirs"/>
        /// </summary>
        /// <param name="mock">Filesystem mock</param>
        /// <param name="dirs">Directories in the filesystem</param>
        private void SetupFilesystem(Mock<IFileSystem> mock, params string[] dirs)
        {
            // build directory tree
            var root = new Node("C:");
            foreach (var dir in dirs)
            {
                // split path by directory separators (assume root C:/)
                var parts = Normalize(dir).Split('/').Skip(1).ToArray();
                var node = root;
                foreach (var part in parts)
                {
                    // find this subdirectory in the tree
                    var index = node.Children.FindIndex(val => val.Name == part);
                    if (index < 0)
                    {
                        // if it is not in the tree, create it
                        index = node.Children.Count;
                        node.Children.Add(new Node(node.Path + '/' + part));
                    }

                    // move to the subdirectory
                    node = node.Children[index];
                }
            }

            // setup mock
            var stack = new Stack<Node>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var item = stack.Pop();

                // setup this node
                mock.Setup(system => system.EnumerateDirectories(ItIsPath(item.Path)))
                    .Returns(item.Children.Select(child => child.Path).ToArray());
                mock.Setup(system => system.EnumerateDirectories(ItIsPath(item.Path), "*"))
                    .Returns(item.Children.Select(child => child.Path).ToArray());
                mock.Setup(system => system.DirectoryExists(ItIsPath(item.Path)))
                    .Returns(true);

                // setup attributes 
                mock.Setup(system => system.GetAttributes(ItIsPath(item.Path)))
                    .Returns(FileAttributes.Directory);

                // setup EnumerateDirectories with pattern
                mock.Setup(system => system.EnumerateDirectories(ItIsPath(item.Path), It.IsAny<string>()))
                    .Returns((string path, string pattern) =>
                    {
                        pattern = "^" + pattern
                            .Replace("?", "[^/\\\\]?")
                            .Replace("*", "[^/\\\\]*") + "$";
                        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                        return item.Children
                            .Where(child => regex.IsMatch(child.Name))
                            .Select(child => child.Path)
                            .ToArray();
                    });

                // build all children
                foreach (var child in item.Children)
                {
                    stack.Push(child);
                }
            }
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
            SetupFilesystem(fileSystem, "C:\\directory\\a\\b\\c\\");
            
            var finder = new FileFinder(fileSystem.Object, "C:/directory/a/b/c");
            var directories = finder.GetDirectories().ToArray();
            Assert.AreEqual(1, directories.Length);
            Assert.AreEqual("C:\\directory\\a\\b\\c\\", directories[0]);
        }

        [TestMethod]
        public void GetDirectories_PathWithoutPatternAndNonexistentDirectory()
        {
            var fileSystem = new Mock<IFileSystem>();
            SetupFilesystem(fileSystem, "C:\\directory\\a");

            var finder = new FileFinder(fileSystem.Object, "C:/directory/a/b/c");
            var directories = finder.GetDirectories().ToArray();
            Assert.AreEqual(0, directories.Length);
        }

        [TestMethod]
        public void GetDirectories_PathWithOneAsteriskPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            SetupFilesystem(fileSystem, "C:/a/bba", "C:/a/ba/c");

            var finder = new FileFinder(fileSystem.Object, "C:/a/b*/c");
            var directories = finder.GetDirectories().ToArray();
            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:\\a\\ba\\c\\", directories[0]));
        }

        [TestMethod]
        public void GetDirectories_PathWithOneQuestionMarkPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            SetupFilesystem(fileSystem, "C:/a/ba/c", "C:/a/bc/c");

            var finder = new FileFinder(fileSystem.Object, "C:/a/b?/c");
            var directories = finder.GetDirectories().OrderBy(item => item).ToArray();
            Assert.AreEqual(2, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:/a/ba/c", directories[0]));
            Assert.IsTrue(ArePathsEqual("C:/a/bc/c", directories[1]));
        }

        [TestMethod]
        public void GetDirectories_PathWithGeneralPattern()
        {
            var fileSystem = new Mock<IFileSystem>();
            SetupFilesystem(fileSystem, new[]
            {
                "C:/a/d/e/b/x",
                "C:/a/b/b/x",
                "C:/a/c/b/x",
            });

            var finder = new FileFinder(fileSystem.Object, "C:/a/**/b/*");
            var directories = finder.GetDirectories().OrderBy(item => item).ToArray();
            Assert.AreEqual(4, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:/a/b/b", directories[0]));
            Assert.IsTrue(ArePathsEqual("C:/a/b/b/x", directories[1]));
            Assert.IsTrue(ArePathsEqual("C:/a/c/b/x", directories[2]));
            Assert.IsTrue(ArePathsEqual("C:/a/d/e/b/x", directories[3]));
        }

        [TestMethod]
        public void GetDirectories_GeneralPatternMatchesEmptyString()
        {
            var fileSystem = new Mock<IFileSystem>();
            SetupFilesystem(fileSystem, "C:/a/b/c", "C:/a/x/b/c", "C:/a/x/y/b/c");

            var finder = new FileFinder(fileSystem.Object, "C:/a/**/b/c");
            var directories = finder.GetDirectories().OrderBy(item => item).ToArray();

            Assert.AreEqual(3, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:/a/b/c", directories[0]));
            Assert.IsTrue(ArePathsEqual("C:/a/x/b/c", directories[1]));
            Assert.IsTrue(ArePathsEqual("C:/a/x/y/b/c", directories[2]));
        }

        [TestMethod]
        public void GetDirectories_IgnoreSystemDirectories()
        {
            var fileSystem = new Mock<IFileSystem>();
            SetupFilesystem(fileSystem, "C:/a/x", "C:/a/y");

            // C:/a/x is a system directory => the class should skip it
            fileSystem
                .Setup(mock => mock.GetAttributes(ItIsPath("C:/a/x")))
                .Returns(FileAttributes.Directory | FileAttributes.System);

            var finder = new FileFinder(fileSystem.Object, "C:/a/*");
            var directories = finder.GetDirectories().ToArray();

            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:/a/y", directories[0]));
        }

        [TestMethod]
        public void GetDirectories_ReturnEachDirectoryExactlyOnce()
        {
            var fileSystem = new Mock<IFileSystem>();
            SetupFilesystem(fileSystem, "C:/a/b/b/c");

            var finder = new FileFinder(fileSystem.Object, "C:/a/**/b/**/c");
            var directories = finder.GetDirectories().ToArray();

            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:/a/b/b/c", directories[0]));
        }
        
        [TestMethod]
        public void GetDirectories_SuffixDirectoriesWithPathSeparator()
        {
            var fileSystem = new Mock<IFileSystem>();
            SetupFilesystem(fileSystem, "C:/a");

            var finder = new FileFinder(fileSystem.Object, "C:/*");
            var directories = finder.GetDirectories().ToArray();

            Assert.AreEqual(1, directories.Length);
            Assert.IsTrue(ArePathsEqual("C:/a", directories[0]));

            // Verify that there is a directory separator after "C:" Otherwise, C: would be a relative path
            // EnumerateDirectories can be called with or without the * pattern
            // fileSystem.Verify(mock => mock.EnumerateDirectories("C:/"));
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
            SetupFilesystem(fileSystem, 
                "C:/r/a/a", 
                "C:/r/a/b", 
                "C:/r/a/c", 
                "C:/r/b/a", 
                "C:/r/b/b", 
                "C:/r/b/c");

            var finder = new FileFinder(fileSystem.Object, "C:/r/**");
            var directories = finder.GetDirectories(new DescComparer()).ToArray();

            var expectedDirectories = new[]
            {
                "C:/r/b/c", "C:/r/b/b", "C:/r/b/a",
                "C:/r/b",
                "C:/r/a/c", "C:/r/a/b", "C:/r/a/a",
                "C:/r/a",
                "C:/r",
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
