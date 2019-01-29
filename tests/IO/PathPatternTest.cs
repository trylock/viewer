using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.IO;

namespace ViewerTest.IO
{
    [TestClass]
    public class PathPatternTest
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

        [TestMethod]
        public void PathPattern_NormalizeDirectorySeparators()
        {
            var pattern = new PathPattern("a/b\\c\\d/e\\f/g");
            Assert.AreEqual("a/b/c/d/e/f/g", pattern.Text);
        }

        [TestMethod]
        public void Match_EmptyPattern()
        {
            var pattern = new PathPattern("");

            Assert.IsTrue(pattern.Match(""));
            Assert.IsFalse(pattern.Match("a"));
        }

        [TestMethod]
        public void Match_PathWithoutPattern()
        {
            var pattern = new PathPattern("C:/directory/a/b/c");

            Assert.IsFalse(pattern.Match("C:/"));
            Assert.IsFalse(pattern.Match("C:/directory"));
            Assert.IsFalse(pattern.Match("C:/directory/a"));
            Assert.IsFalse(pattern.Match("C:/directory/a/b"));
            Assert.IsTrue(pattern.Match("C:/directory/a/b/c"));
            Assert.IsTrue(pattern.Match("C:/directory/a/b/c/"));
            Assert.IsTrue(pattern.Match("C:\\directory\\a\\b\\c"));
            Assert.IsTrue(pattern.Match("C:\\directory\\a\\b\\c\\"));
        }

        [TestMethod]
        public void Match_PathWithOneAsteriskPattern()
        {
            var pattern = new PathPattern("C:/a/b*/c");

            Assert.IsFalse(pattern.Match("C:/a"));
            Assert.IsFalse(pattern.Match("C:/a/c"));
            Assert.IsFalse(pattern.Match("C:/a/x/c"));
            Assert.IsTrue(pattern.Match("C:/a/b/c"));
            Assert.IsTrue(pattern.Match("C:/a\\b/c\\"));
            Assert.IsTrue(pattern.Match("C:/a/bx/c"));
            Assert.IsTrue(pattern.Match("C:/a/bxy/c"));
            Assert.IsTrue(pattern.Match("C:/a/bxyz/c"));
        }

        [TestMethod]
        public void Match_PathWithOneQuestionMarkPattern()
        {
            var pattern = new PathPattern("C:/a/b?/c");

            Assert.IsFalse(pattern.Match("C:/a"));
            Assert.IsFalse(pattern.Match("C:/a/c"));
            Assert.IsFalse(pattern.Match("C:/a/x/c"));
            Assert.IsFalse(pattern.Match("C:/a/b/c"));
            Assert.IsTrue(pattern.Match("C:/a/bx/c"));
            Assert.IsTrue(pattern.Match("C:\\a\\bx/c\\"));
            Assert.IsFalse(pattern.Match("C:/a/bxy/c"));
            Assert.IsFalse(pattern.Match("C:/a/bxyz/c"));
        }

        [TestMethod]
        public void Match_PathWithGeneralPattern()
        {
            var pattern = new PathPattern("C:/a/**/c");

            Assert.IsFalse(pattern.Match("C:/a"));
            Assert.IsTrue(pattern.Match("C:/a/c"));
            Assert.IsFalse(pattern.Match("C:/ac"));
            Assert.IsFalse(pattern.Match("C:/a/bc"));
            Assert.IsFalse(pattern.Match("C:/ab/c"));
            Assert.IsTrue(pattern.Match("C:/a/x/c"));
            Assert.IsTrue(pattern.Match("C:/a/x/y/c"));
            Assert.IsFalse(pattern.Match("C:/a/x/y"));
        }

        [TestMethod]
        public void Match_CombinedPattern()
        {
            var pattern = new PathPattern("C:/a/**/b*/c");

            Assert.IsFalse(pattern.Match("C:/a"));
            Assert.IsFalse(pattern.Match("C:/a/c"));
            Assert.IsTrue(pattern.Match("C:/a/b/c"));
            Assert.IsTrue(pattern.Match("C:/a/bx/c"));
            Assert.IsTrue(pattern.Match("C:/a/bxy/c"));
            Assert.IsTrue(pattern.Match("C:/a/x/b/c"));
            Assert.IsFalse(pattern.Match("C:/a/x/b"));
        }

        [TestMethod]
        public void Match_GeneralPatternAtTheEnd()
        {
            var pattern = new PathPattern("a/**");

            Assert.IsTrue(pattern.Match("a"));
            Assert.IsTrue(pattern.Match("a/"));
            Assert.IsTrue(pattern.Match("a/b"));
            Assert.IsTrue(pattern.Match("a/b/c"));
            Assert.IsFalse(pattern.Match("b/a"));
            Assert.IsFalse(pattern.Match("b"));
            Assert.IsFalse(pattern.Match(""));
        }

        [TestMethod]
        public void Match_GeneralPatternAtTheStart()
        {
            var pattern = new PathPattern("**/a");

            Assert.IsTrue(pattern.Match("a"));
            Assert.IsTrue(pattern.Match("a/"));
            Assert.IsFalse(pattern.Match("a/b"));
            Assert.IsFalse(pattern.Match("a/b/c"));
            Assert.IsTrue(pattern.Match("b/a"));
            Assert.IsTrue(pattern.Match("c/b/a"));
            Assert.IsFalse(pattern.Match("b"));
            Assert.IsFalse(pattern.Match(""));
        }

        [TestMethod]
        public void Match_MultipleGeneralPatterns()
        {
            var pattern = new PathPattern("a/**/b/**/c");

            Assert.IsFalse(pattern.Match(""));
            Assert.IsFalse(pattern.Match("a"));
            Assert.IsFalse(pattern.Match("b"));
            Assert.IsFalse(pattern.Match("c"));
            Assert.IsFalse(pattern.Match("a/b"));
            Assert.IsFalse(pattern.Match("a/c"));
            Assert.IsFalse(pattern.Match("b/c"));
            Assert.IsTrue(pattern.Match("a/b/c"));
            Assert.IsTrue(pattern.Match("a/x/b/c"));
            Assert.IsTrue(pattern.Match("a/b/x/c"));
            Assert.IsTrue(pattern.Match("a/x/b/y/c"));
            Assert.IsTrue(pattern.Match("a/x/z/b/y/w/c"));
        }

        [TestMethod]
        public void Match_MultipleGeneralPatternsInSuccession()
        {
            var pattern = new PathPattern("a/**/**/b");

            Assert.IsFalse(pattern.Match("a"));
            Assert.IsFalse(pattern.Match("b"));
            Assert.IsTrue(pattern.Match("a/b"));
            Assert.IsFalse(pattern.Match("a//b"));
            Assert.IsFalse(pattern.Match("a///b"));
            Assert.IsFalse(pattern.Match(@"a\\b"));
        }

        [TestMethod]
        public void Match_RecursiveGeneralPatternWithNonRecursivePattern()
        {
            var pattern = new PathPattern("**/*");

            Assert.IsFalse(pattern.Match(""));
            Assert.IsTrue(pattern.Match("a"));
            Assert.IsTrue(pattern.Match("abcd"));
            Assert.IsTrue(pattern.Match("ab/cd"));
            Assert.IsTrue(pattern.Match("ab/cd/efg"));
            Assert.IsFalse(pattern.Match("a//b"));
        }

        [TestMethod]
        public void Match_DifferentOrderOfRecursiveGeneralPatternWithNonRecursivePattern()
        {
            var pattern = new PathPattern("*/**");

            Assert.IsFalse(pattern.Match(""));
            Assert.IsTrue(pattern.Match("a"));
            Assert.IsTrue(pattern.Match("abcd"));
            Assert.IsTrue(pattern.Match("ab/cd"));
            Assert.IsTrue(pattern.Match("ab/cd/efg"));
        }

        [TestMethod]
        public void Match_StartsWithPattern()
        {
            var pattern = new PathPattern("a?b*c/**");

            Assert.IsFalse(pattern.Match(""));
            Assert.IsFalse(pattern.Match("a"));
            Assert.IsFalse(pattern.Match("ab"));
            Assert.IsFalse(pattern.Match("abc"));
            Assert.IsTrue(pattern.Match("axbc"));
            Assert.IsTrue(pattern.Match("axbc/yz"));
            Assert.IsFalse(pattern.Match("abc/yz/uv"));
        }

        [TestMethod]
        public void Match_EndsWithPattern()
        {
            var pattern = new PathPattern("**/a?b*c");

            Assert.IsFalse(pattern.Match(""));
            Assert.IsFalse(pattern.Match("a"));
            Assert.IsFalse(pattern.Match("ab"));
            Assert.IsFalse(pattern.Match("abc"));
            Assert.IsTrue(pattern.Match("axbc"));
            Assert.IsTrue(pattern.Match("yz/axbc"));
            Assert.IsTrue(pattern.Match("yz/uv/axbc"));
            Assert.IsFalse(pattern.Match("axbc/uv"));
        }

        [TestMethod]
        public void Match_JustTheGeneralPattern()
        {
            var pattern = new PathPattern("**");

            Assert.IsTrue(pattern.Match(""));
            Assert.IsTrue(pattern.Match("/"));
            Assert.IsTrue(pattern.Match("a"));
            Assert.IsTrue(pattern.Match("a/b"));
            Assert.IsTrue(pattern.Match("a/b/c"));
            Assert.IsTrue(pattern.Match("c/a/b"));
            Assert.IsTrue(pattern.Match("//"));
            Assert.IsTrue(pattern.Match("/\\"));
            Assert.IsTrue(pattern.Match("/\\/"));
            Assert.IsFalse(pattern.Match("c//a/b"));
        }

        [TestMethod]
        public void Match_JustTheStarPattern()
        {
            var pattern = new PathPattern("*");

            Assert.IsFalse(pattern.Match(""));
            Assert.IsFalse(pattern.Match("/"));
            Assert.IsTrue(pattern.Match("a"));
            Assert.IsTrue(pattern.Match("a/"));
            Assert.IsFalse(pattern.Match("a/b"));
            Assert.IsFalse(pattern.Match("a/b/c"));
            Assert.IsFalse(pattern.Match("c/a/b"));
        }

        [TestMethod]
        public void Match_StarPatternWontMatchEmptyString()
        {
            var pattern = new PathPattern("a/*/b");

            Assert.IsFalse(pattern.Match("a"));
            Assert.IsFalse(pattern.Match("a/"));
            Assert.IsFalse(pattern.Match("a/b"));
            Assert.IsFalse(pattern.Match("a//b"));
            Assert.IsFalse(pattern.Match("a\\\\b"));
            Assert.IsTrue(pattern.Match("a/c/b"));
        }

        [TestMethod]
        public void Match_JustTheQuestionMarkPattern()
        {
            var pattern = new PathPattern("?");

            Assert.IsFalse(pattern.Match(""));
            Assert.IsFalse(pattern.Match("/"));
            Assert.IsTrue(pattern.Match("a"));
            Assert.IsTrue(pattern.Match("b"));
            Assert.IsTrue(pattern.Match("a/"));
            Assert.IsFalse(pattern.Match("ab"));
            Assert.IsFalse(pattern.Match("a/b"));
            Assert.IsFalse(pattern.Match("a/b/c"));
            Assert.IsFalse(pattern.Match("c/a/b"));
        }
        
        [TestMethod]
        public void GetBasePath_EmptyPath()
        {
            Assert.IsNull(new PathPattern("").GetBasePath());
        }

        [TestMethod]
        public void GetBasePath_OneDirectory()
        {
            Assert.IsTrue(ArePathsEqual("dir", new PathPattern("dir").GetBasePath()));
        }

        [TestMethod]
        public void GetBasePath_OnePattern()
        {
            Assert.IsNull(new PathPattern("d?r").GetBasePath());
        }

        [TestMethod]
        public void GetBasePath_MultipleDirectoriesWithoutPattern()
        {
            Assert.IsTrue(ArePathsEqual("a/b/c", new PathPattern("a/b/c").GetBasePath()));
        }

        [TestMethod]
        public void GetBasePath_MultipleDirectoriesWithPattern()
        {
            Assert.IsTrue(ArePathsEqual("a/b", new PathPattern("a/b/c*").GetBasePath()));
        }
        
        [TestMethod]
        public void GetParent_RootFolderPattern()
        {
            Assert.IsTrue(ArePatternsEqual("C:/", new PathPattern("C:").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("C:/", new PathPattern("C:/").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("C:/", new PathPattern("C:\\").GetParent().Text));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_SimplePath()
        {
            Assert.IsTrue(ArePatternsEqual("a/b", new PathPattern("a/b/c").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("a/b", new PathPattern("a/b/c/").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("a/b", new PathPattern("a/b\\c\\").GetParent().Text));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_PatternInTheMiddle()
        {
            Assert.IsTrue(ArePatternsEqual("a/**", new PathPattern("a/**/b").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("a/x*", new PathPattern("a/x*/b").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("a/x?y", new PathPattern("a/x?y/b").GetParent().Text));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_NonRecursivePatternAtTheEnd()
        {
            Assert.IsTrue(ArePatternsEqual("a/**", new PathPattern("a/**/x*").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("a/x*", new PathPattern("a/x*/y*").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("a/x?y", new PathPattern("a/x?y/z?").GetParent().Text));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_PathWithSpecialCharacters()
        {
            Assert.IsTrue(ArePatternsEqual("a", new PathPattern("a/**/x*/../b/../././../d").GetParent().Text));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_RecursivePatternAtTheEnd()
        {
            Assert.IsTrue(ArePatternsEqual("**", new PathPattern("**").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("a", new PathPattern("a/**").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("a", new PathPattern("a\\**").GetParent().Text));
        }

        [TestMethod]
        public void GetParentDirectoryPattern_ParrentDirectoryAtTheEnd()
        {
            Assert.IsTrue(ArePatternsEqual("**", new PathPattern("**/..").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("**", new PathPattern("**/../").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("**", new PathPattern("**/../..").GetParent().Text));
            Assert.IsTrue(ArePatternsEqual("**", new PathPattern("**\\..\\").GetParent().Text));
        }
    }
}
