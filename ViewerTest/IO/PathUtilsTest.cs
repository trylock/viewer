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
    }
}
