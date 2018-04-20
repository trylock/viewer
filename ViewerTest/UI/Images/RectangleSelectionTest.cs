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
            var selection = new RectangleSelection();
            Assert.IsFalse(selection.IsActive);
            Assert.AreEqual(Rectangle.Empty, selection.GetBounds(new Point(5, 6)));
        }

        [TestMethod]
        public void Selection_Bounds()
        {
            var selection = new RectangleSelection();
            Assert.IsFalse(selection.IsActive);

            selection.StartSelection(new Point(1, 2));
            Assert.IsTrue(selection.IsActive);
            Assert.AreEqual(selection.StartPoint, new Point(1, 2));
            Assert.AreEqual(new Rectangle(1, 2, 2, 3), selection.GetBounds(new Point(3, 5)));
            Assert.AreEqual(new Rectangle(0, 0, 1, 2), selection.GetBounds(new Point(0, 0)));
            selection.EndSelection();

            Assert.IsFalse(selection.IsActive);
        }
    }
}
