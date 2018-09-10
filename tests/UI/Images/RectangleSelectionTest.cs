using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class RectangleSelectionTest
    {
        [TestMethod]
        public void GetBounds_InactiveSelection()
        {
            var selection = new RectangleSelection<int>(EqualityComparer<int>.Default);
            Assert.IsFalse(selection.IsActive);
            Assert.AreEqual(Rectangle.Empty, selection.GetBounds(new Point(5, 6)));
        }

        [TestMethod]
        public void Selection_Bounds()
        {
            var selection = new RectangleSelection<int>(EqualityComparer<int>.Default);
            Assert.IsFalse(selection.IsActive);

            selection.Begin(new Point(1, 2), ReplaceSelectionStrategy<int>.Default);
            Assert.IsTrue(selection.IsActive);
            Assert.AreEqual(selection.StartPoint, new Point(1, 2));
            Assert.AreEqual(new Rectangle(1, 2, 2, 3), selection.GetBounds(new Point(3, 5)));
            Assert.AreEqual(new Rectangle(0, 0, 1, 2), selection.GetBounds(new Point(0, 0)));
            selection.End();

            Assert.IsFalse(selection.IsActive);
        }

        [TestMethod]
        public void Selection_ReplaceStrategy()
        {
            var selection = new RectangleSelection<int>(EqualityComparer<int>.Default);

            selection.Begin(new Point(0, 0), ReplaceSelectionStrategy<int>.Default);
            var changed = selection.Set(new[] {1, 2}, Enumerable.Range(0, 10).ToList());
            Assert.IsTrue(changed);
            changed = selection.Set(new[] {1, 2}, Enumerable.Range(0, 10).ToList());
            Assert.IsFalse(changed);
            CollectionAssert.AreEqual(new[]{ 1, 2 }, selection.OrderBy(item => item).ToArray());
            selection.End();
        }
    }
}
