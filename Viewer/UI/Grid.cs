using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI
{
    public struct GridCell
    {
        /// <summary>
        /// Row of the grid cell
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// Column of the grid cell
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Get index of a cell.
        /// It will be -1 if there is no grid cell at given location.
        /// </summary>
        public int Index
        {
            get
            {
                var index = Row * _grid.ColumnsCount + Column;
                if (Row < 0 || Column < 0 || index >= _grid.CellsCount)
                {
                    index = -1;
                }

                return index;
            }
        }
        
        /// <summary>
        /// Get location of this cell
        /// </summary>
        public Point Location => new Point(
            Column * _grid.CellSize.Width,
            Row * _grid.CellSize.Height
        );

        /// <summary>
        /// Get size of this cell
        /// </summary>
        public Size Size => _grid.CellSize;

        private Grid _grid;
        
        public GridCell(Grid grid, int row, int column)
        {
            _grid = grid;
            Row = row;
            Column = column;
        }
    }
    
    public class Grid
    {
        #region Editable Public Properties

        /// <summary>
        /// Minimal width of a grid cell
        /// </summary>
        public int MinCellWidth { get; set; } = 200;

        /// <summary>
        /// Height of a grid cell
        /// </summary>
        public int CellHeight { get; set; } = 200;

        /// <summary>
        /// Number of cells in the grid
        /// </summary>
        public int CellsCount { get; set; } = 0;

        #endregion
        
        #region Computed Public Properties

        /// <summary>
        /// Number of columns in the grid.
        /// This will always be >= 1.
        /// </summary>
        public int ColumnsCount => Math.Max(_width / MinCellWidth, 1);

        /// <summary>
        /// Number of rows in the grid.
        /// This will always be >= 0.
        /// </summary>
        public int RowsCount => Math.Max(MathUtils.RoundUpDiv(CellsCount, ColumnsCount), 0);

        /// <summary>
        /// Actual size of each cell in the grid
        /// </summary>
        public Size CellSize => new Size(
            _width / ColumnsCount,
            CellHeight
        );

        /// <summary>
        /// Size of the entire grid
        /// </summary>
        public Size GridSize => new Size(_width, RowsCount * CellHeight);

        #endregion 

        /// <summary>
        /// Current with of the grid
        /// </summary>
        private int _width;
       
        /// <summary>
        /// Resize the grid area.
        /// </summary>
        /// <param name="width">New width of the grid</param>
        public void Resize(int width)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            _width = width;
        }

        /// <summary>
        /// Find cell location (row, column) and its index.
        /// </summary>
        /// <param name="location">Point in space</param>
        /// <returns>
        ///     Coordinates of a grid cell.
        ///     Row and column will be -1 if there is no grid cell at given location.
        /// </returns>
        public GridCell GetCellAt(Point location)
        {
            var row = location.Y >= 0 ? location.Y / CellSize.Height : -1;
            var column = location.X >= 0 ? location.X / CellSize.Width : -1;
            if (row < 0 || row >= RowsCount ||
                column < 0 || column >= ColumnsCount)
            {
                // invalid grid cell
                return new GridCell(this, -1, -1);
            }

            return new GridCell(this, row, column);
        }

        /// <summary>
        /// Iterate over grid cells in given area.
        /// </summary>
        /// <param name="bounds">Area of the grid</param>
        /// <returns>Grid cells in given area</returns>
        public IEnumerable<GridCell> GetCellsInBounds(Rectangle bounds)
        {
            // Compute intersection of bounds with the bounding box of the grid.
            // We subtract CellSize from the grid size because we want for each 
            // point P in the grid rectangle to satisfy that P / CellSize is a 
            // valid cell location.
            var minX = Math.Max(bounds.X, 0);
            var maxX = Math.Min(bounds.X + bounds.Width - 1, GridSize.Width - CellSize.Width);
            var minY = Math.Max(bounds.Y, 0);
            var maxY = Math.Min(bounds.Y + bounds.Height - 1, GridSize.Height - CellSize.Height);
            if (minX > maxX || minY > maxY)
                yield break; // the intersection is empty

            // find first column and column after the last column
            var beginColumn = minX / CellSize.Width;
            var endColumn = maxX / CellSize.Width + 1;

            // find first row and row after the last row
            var beginRow = minY / CellSize.Height;
            var endRow = maxY / CellSize.Height + 1;

            for (var row = beginRow; row < endRow; ++row)
            {
                var lastColumn = endColumn;
                if (row + 1 == RowsCount && CellsCount % ColumnsCount != 0)
                {
                    // this is the last row and it is not full
                    lastColumn = Math.Min(CellsCount % ColumnsCount, endColumn);
                }
                for (var column = beginColumn; column < lastColumn; ++column)
                {
                    yield return new GridCell(this, row, column);
                }
            }
        }
    }
}
