using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.IO;

namespace ViewerTest.IO
{
    [TestClass]
    public class DirectoryUtilsTest
    {
        public string Source = Path.Combine(Environment.CurrentDirectory, "source");

        public string Target = Path.Combine(Environment.CurrentDirectory, "target");

        [TestInitialize]
        [TestCleanup]
        public void Startup()
        {
            if (Directory.Exists(Source))
            {
                Directory.Delete(Source, true);
            }
            if (Directory.Exists(Target))
            {
                Directory.Delete(Target, true);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void Copy_SourceDirectoryDoesNotExist()
        {
            Directory.CreateDirectory(Target);

            DirectoryUtils.Copy(Source, Target, false);
        }

        [TestMethod]
        public void Copy_EmptyDirectory()
        {
            Directory.CreateDirectory(Source);

            Assert.IsFalse(Directory.Exists(Target));

            DirectoryUtils.Copy(Source, Target, false);

            Assert.IsTrue(Directory.Exists(Source));
            Assert.IsTrue(Directory.Exists(Target));
            Assert.AreEqual(0, Directory.GetFileSystemEntries(Source).Length);
            Assert.AreEqual(0, Directory.GetFileSystemEntries(Target).Length);
        }

        [TestMethod]
        public void Copy_DirectoryWithFilesAndNoSubDirectories()
        {
            Directory.CreateDirectory(Source);

            var fileNames = new[] { "a.txt", "b.txt", "c.txt" };
            var text = new[] { "Lorem ipsum", "dolor sit amet", "consequer eulit" };
            var expectedSourceFiles = fileNames.Select(name => Path.Combine(Source, name)).ToArray();
            var expectedTargetFiles = fileNames.Select(name => Path.Combine(Target, name)).ToArray();

            // write files
            int index = 0;
            foreach (var file in expectedSourceFiles)
            {
                File.WriteAllText(file, text[index++]);
            }
            
            Assert.IsFalse(Directory.Exists(Target));

            DirectoryUtils.Copy(Source, Target, false);

            Assert.IsTrue(Directory.Exists(Source));
            Assert.IsTrue(Directory.Exists(Target));

            var sourceFiles = Directory.GetFiles(Source);
            CollectionAssert.AreEqual(expectedSourceFiles, sourceFiles);

            var targetFiles = Directory.GetFiles(Target);
            CollectionAssert.AreEqual(expectedTargetFiles, targetFiles);

            index = 0;
            foreach (var file in expectedTargetFiles)
            {
                var contents = File.ReadAllText(file);
                Assert.AreEqual(text[index++], contents);
            }
        }

        [TestMethod]
        public void Copy_Subdirectories()
        {
            Directory.CreateDirectory(Source);
            Directory.CreateDirectory(Path.Combine(Source, "A"));
            Directory.CreateDirectory(Path.Combine(Source, "B"));
            Directory.CreateDirectory(Path.Combine(Source, "A", "C"));

            Assert.IsFalse(Directory.Exists(Target));
            
            DirectoryUtils.Copy(Source, Target);

            Assert.IsTrue(Directory.Exists(Target));
            Assert.IsTrue(Directory.Exists(Path.Combine(Target, "A")));
            Assert.IsTrue(Directory.Exists(Path.Combine(Target, "B")));
            Assert.IsTrue(Directory.Exists(Path.Combine(Target, "A", "C")));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Copy_SameDirectories()
        {
            Directory.CreateDirectory(Source);
            DirectoryUtils.Copy(Path.Combine(Source, "..", "source"), Source);
        }

        private const int TooLongPathLength = 1000;

        [TestMethod]
        [ExpectedException(typeof(PathTooLongException))]
        public void Copy_TooLongPath()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < TooLongPathLength; ++i)
            {
                sb.Append('a');
            }
            DirectoryUtils.Copy(sb.ToString(), Target);
        }
    }
}
