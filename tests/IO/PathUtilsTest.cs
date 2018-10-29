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
    public class PathUtilsTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetLastPart_NullPath()
        {
            PathUtils.GetLastPart(null);
        }

        [TestMethod]
        public void GetLastPart_EmptyPath()
        {
            Assert.AreEqual("", PathUtils.GetLastPart(""));
        }

        [TestMethod]
        public void GetLastPart_DirectoryWithPathSeparatorAtTheEnd()
        {
            Assert.AreEqual("C:", PathUtils.GetLastPart("C:" + Path.DirectorySeparatorChar));
            Assert.AreEqual("lorem", PathUtils.GetLastPart(Path.Combine("C:/", "lorem") + Path.AltDirectorySeparatorChar));
            Assert.AreEqual("..", PathUtils.GetLastPart(Path.Combine("C:/", "ipsum", "..") + Path.AltDirectorySeparatorChar));
            Assert.AreEqual(".", PathUtils.GetLastPart(Path.Combine("C:/", "dolor", ".") + Path.DirectorySeparatorChar));
        }

        [TestMethod]
        public void GetLastPart_DirectoryWithoutPathSeparatorAtTheEnd()
        {
            Assert.AreEqual("C:", PathUtils.GetLastPart("C:"));
            Assert.AreEqual("lorem", PathUtils.GetLastPart(Path.Combine("C:/", "lorem")));
            Assert.AreEqual("..", PathUtils.GetLastPart(Path.Combine("C:/", "ipsum", "..")));
            Assert.AreEqual(".", PathUtils.GetLastPart(Path.Combine("C:/", "dolor", ".")));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Split_NullPath()
        {
            PathUtils.Split(null);
        }

        [TestMethod]
        public void Split_EmptyPath()
        {
            var parts = PathUtils.Split("");
            CollectionAssert.AreEqual(new[]{ "" }, parts.ToArray());
        }

        [TestMethod]
        public void Split_OnePartPath()
        {
            var parts = PathUtils.Split("C:");
            CollectionAssert.AreEqual(new[] { "C:" }, parts.ToArray());

            parts = PathUtils.Split("C:/");
            CollectionAssert.AreEqual(new[] { "C:" }, parts.ToArray());

            parts = PathUtils.Split("C:\\");
            CollectionAssert.AreEqual(new[] { "C:" }, parts.ToArray());

            parts = PathUtils.Split("C://");
            CollectionAssert.AreEqual(new[] { "C:" }, parts.ToArray());
        }

        [TestMethod]
        public void Split_MultipleParts()
        {
            var parts = PathUtils.Split("abc/x/d");
            CollectionAssert.AreEqual(new[] { "abc", "x", "d" }, parts.ToArray());

            parts = PathUtils.Split("abc/x\\d");
            CollectionAssert.AreEqual(new[] { "abc", "x", "d" }, parts.ToArray());
        }

        [TestMethod]
        public void Split_PathSeparatorAtTheStart()
        {
            var parts = PathUtils.Split("\\\\NAS\\Photos");
            CollectionAssert.AreEqual(new[]{ "\\\\NAS", "Photos" }, parts.ToArray());

            parts = PathUtils.Split("\\\\NAS\\Photos\\");
            CollectionAssert.AreEqual(new[] { "\\\\NAS", "Photos" }, parts.ToArray());
        }

        [TestMethod]
        public void Normalize_UNCPathWithJustTheFirstSlashes()
        {
            var path = PathUtils.NormalizePath("//");
            Assert.IsNull(path);
            
            path = PathUtils.NormalizePath("\\\\");
            Assert.IsNull(path);
        }

        [TestMethod]
        public void Normalize_UNCPathWithJustTheServerName()
        {
            var path = PathUtils.NormalizePath("//NAS");
            Assert.IsNull(path);

            path = PathUtils.NormalizePath("\\\\NAS");
            Assert.IsNull(path);
        }

        [TestMethod]
        public void Normalize_MinimalUNCPath()
        {
            var path = PathUtils.NormalizePath("//NAS/fotky");
            Assert.AreEqual("//NAS/fotky", path);

            path = PathUtils.NormalizePath("\\\\NAS\\fotky");
            Assert.AreEqual("//NAS/fotky", path);
        }

        [TestMethod]
        public void Normalize_InvalidCharactersInPath()
        {
            var path = PathUtils.NormalizePath("C:/a < b");
            Assert.IsNull(path);
        }

        [TestMethod]
        public void Normalize_EmptyPath()
        {
            var path = PathUtils.NormalizePath("");
            Assert.IsNull(path);
            path = PathUtils.NormalizePath("  \t   \n");
            Assert.IsNull(path);
        }
    }
}
