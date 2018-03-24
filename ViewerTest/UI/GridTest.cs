using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.UI;

namespace ViewerTest.UI
{
    [TestClass]
    public class GridTest
    {
        [TestMethod]
        public void CellProperties_EmptyGrid()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.Resize(250);

            Assert.AreEqual(0, grid.CellsCount);
            Assert.AreEqual(2, grid.ColumnsCount);
            Assert.AreEqual(0, grid.RowsCount);
            Assert.AreEqual(125, grid.CellSize.Width);
            Assert.AreEqual(200, grid.CellSize.Height);
        }

        [TestMethod]
        public void CellProperties_NonEmptyGrid()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 2;
            grid.Resize(350);

            Assert.AreEqual(2, grid.CellsCount);
            Assert.AreEqual(3, grid.ColumnsCount);
            Assert.AreEqual(1, grid.RowsCount);
            Assert.AreEqual(116, grid.CellSize.Width);
            Assert.AreEqual(200, grid.CellSize.Height);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Resize_NegativeSize()
        {
            var grid = new Grid();
            grid.Resize(-50);
        }

        [TestMethod]
        public void GetCellAt_EmptyGrid()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 0;
            grid.Resize(250);

            var cell = grid.GetCellAt(new Point(50, 25));
            Assert.AreEqual(-1, cell.Index);
            Assert.AreEqual(-1, cell.Row);
            Assert.AreEqual(-1, cell.Column);
            Assert.AreEqual(125, cell.Size.Width);
            Assert.AreEqual(200, cell.Size.Height);
        }

        [TestMethod]
        public void GetCellAt_NegativeX()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 128;
            grid.Resize(250);

            var cell = grid.GetCellAt(new Point(-50, 25));
            Assert.AreEqual(-1, cell.Index);
            Assert.AreEqual(-1, cell.Row);
            Assert.AreEqual(-1, cell.Column);
            Assert.AreEqual(125, cell.Size.Width);
            Assert.AreEqual(200, cell.Size.Height);
        }
        
        [TestMethod]
        public void GetCellAt_NegativeY()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 128;
            grid.Resize(250);

            var cell = grid.GetCellAt(new Point(50, -25));
            Assert.AreEqual(-1, cell.Index);
            Assert.AreEqual(-1, cell.Row);
            Assert.AreEqual(-1, cell.Column);
            Assert.AreEqual(125, cell.Size.Width);
            Assert.AreEqual(200, cell.Size.Height);
        }

        [TestMethod]
        public void GetCellAt_LocationOutOfBounds()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 128;
            grid.Resize(250);

            var cell = grid.GetCellAt(new Point(260, 55));
            Assert.AreEqual(-1, cell.Index);
            Assert.AreEqual(-1, cell.Row);
            Assert.AreEqual(-1, cell.Column);
            Assert.AreEqual(125, cell.Size.Width);
            Assert.AreEqual(200, cell.Size.Height);
        }

        [TestMethod]
        public void GetCellAt_ValidCell()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 5;
            grid.Resize(375);

            var cell = grid.GetCellAt(new Point(150, 50));
            Assert.AreEqual(1, cell.Index);
            Assert.AreEqual(0, cell.Row);
            Assert.AreEqual(1, cell.Column);
            Assert.AreEqual(125, cell.Size.Width);
            Assert.AreEqual(200, cell.Size.Height);
            Assert.AreEqual(125, cell.Location.X);
            Assert.AreEqual(0, cell.Location.Y);
        }

        [TestMethod]
        public void GetCellsInBounds_EmptyGrid()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 0;
            grid.Resize(250);

            var cells = grid.GetCellsInBounds(
                new Rectangle(new Point(0, 0), grid.GridSize));
            var cellsCount = cells.Count();
            Assert.AreEqual(0, cellsCount);
        }
        
        [TestMethod]
        public void GetCellsInBounds_LastRowIsNotFull()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 3;
            grid.Resize(250);

            var cells = grid.GetCellsInBounds(new Rectangle(
                new Point(-1000, -1000), 
                new Size(2000, 2000)));
            var cellsList = cells.ToArray();
            Assert.AreEqual(3, cellsList.Length);
            Assert.AreEqual(0, cellsList[0].Index);
            Assert.AreEqual(1, cellsList[1].Index);
            Assert.AreEqual(2, cellsList[2].Index);
        }

        [TestMethod]
        public void GetCellsInBounds_NegativeLocation()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 3;
            grid.Resize(250);

            var cells = grid.GetCellsInBounds(new Rectangle(
                new Point(-50, 0),
                new Size(5, 5)));
            Assert.AreEqual(0, cells.Count());
        }

        [TestMethod]
        public void GetCellsInBounds_FirstColumn()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 5;
            grid.Resize(300);

            var cells = grid.GetCellsInBounds(new Rectangle(
                new Point(0, 0),
                new Size(99, 1000)));
            var cellsList = cells.ToArray();
            Assert.AreEqual(2, cellsList.Length);
            Assert.AreEqual(0, cellsList[0].Index);
            Assert.AreEqual(3, cellsList[1].Index);
        }

        [TestMethod]
        public void GetCellsInBounds_SecondColumn()
        {
            var grid = new Grid();
            grid.MinCellWidth = 100;
            grid.CellHeight = 200;
            grid.CellsCount = 5;
            grid.Resize(300);

            var cells = grid.GetCellsInBounds(new Rectangle(
                new Point(100, 0),
                new Size(99, 1000)));
            var cellsList = cells.ToArray();
            Assert.AreEqual(2, cellsList.Length);
            Assert.AreEqual(1, cellsList[0].Index);
            Assert.AreEqual(4, cellsList[1].Index);
        }

        [TestMethod]
        public void GetCellsInBounds_IndivisibleWidth()
        {
            var grid = new Grid();
            grid.MinCellWidth = 200;
            grid.CellHeight = 200;
            grid.CellsCount = 3;
            grid.Resize(419);

            var cells = grid.GetCellsInBounds(new Rectangle(
                new Point(0, 0),
                new Size(1000, 1000)));
            var cellsList = cells.ToArray();
            Assert.AreEqual(0, cellsList[0].Index);
            Assert.AreEqual(0, cellsList[0].Row);
            Assert.AreEqual(0, cellsList[0].Column);

            Assert.AreEqual(1, cellsList[1].Index);
            Assert.AreEqual(0, cellsList[1].Row);
            Assert.AreEqual(1, cellsList[1].Column);

            Assert.AreEqual(2, cellsList[2].Index);
            Assert.AreEqual(1, cellsList[2].Row);
            Assert.AreEqual(0, cellsList[2].Column);
        }
    }
}
