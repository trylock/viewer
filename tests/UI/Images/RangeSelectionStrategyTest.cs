using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class RangeSelectionStrategyTest
    {
        [TestMethod]
        public void Set_PreviousAndCurrentSelectionAreEmpty()
        {
            var selection = new RangeSelectionStrategy<int>();

            var currentSelection = new HashSet<int>();
            var previousSelection = new HashSet<int>();
            var allItems = Enumerable.Range(0, 10).ToList();

            selection.Set(currentSelection, previousSelection, allItems);

            CollectionAssert.AreEqual(new int[] {}, currentSelection.ToArray());
        }

        [TestMethod]
        public void Set_PreviousSelectionIsEmpty()
        {
            var selection = new RangeSelectionStrategy<int>();
            
            var currentSelection = new HashSet<int>(new[]{ 5 });
            var previousSelection = new HashSet<int>();
            var allItems = Enumerable.Range(0, 10).ToList();

            selection.Set(currentSelection, previousSelection, allItems);
            
            CollectionAssert.AreEqual(new[]{ 5 }, currentSelection.ToArray());
        }

        [TestMethod]
        public void Set_CurrentSelectionIsEmpty()
        {
            var selection = new RangeSelectionStrategy<int>();

            var currentSelection = new HashSet<int>();
            var previousSelection = new HashSet<int>(new[]{ 3, 5 });
            var allItems = Enumerable.Range(0, 10).ToList();

            selection.Set(currentSelection, previousSelection, allItems);

            CollectionAssert.AreEqual(new[] { 3, 5 }, currentSelection.ToArray());
        }

        [TestMethod]
        public void Set_CurrentSelectionIsAfterPreviousSelection()
        {
            var selection = new RangeSelectionStrategy<int>();

            var currentSelection = new HashSet<int>(new[]{ 7 });
            var previousSelection = new HashSet<int>(new[] { 3, 5 });
            var allItems = Enumerable.Range(0, 10).ToList();

            selection.Set(currentSelection, previousSelection, allItems);

            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 7 }, currentSelection.ToArray());
        }

        [TestMethod]
        public void Set_CurrentSelectionIsBeforePreviousSelection()
        {
            var selection = new RangeSelectionStrategy<int>();

            var currentSelection = new HashSet<int>(new[] { 1 });
            var previousSelection = new HashSet<int>(new[] { 3, 5 });
            var allItems = Enumerable.Range(0, 10).ToList();

            selection.Set(currentSelection, previousSelection, allItems);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, currentSelection.ToArray());
        }

        [TestMethod]
        public void Set_CurrentSelectionIsBetweenPreviousSelection()
        {
            var selection = new RangeSelectionStrategy<int>();

            var currentSelection = new HashSet<int>(new[] { 6 });
            var previousSelection = new HashSet<int>(new[] { 4, 7 });
            var allItems = Enumerable.Range(0, 10).ToList();

            selection.Set(currentSelection, previousSelection, allItems);

            CollectionAssert.AreEqual(new[] { 4, 5, 6 }, currentSelection.ToArray());
        }
    }
}
