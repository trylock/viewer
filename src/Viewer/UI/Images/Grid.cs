using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;

namespace Viewer.UI.Images
{
    internal struct GridCell
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
                var index = Row * _grid.ColumnCount + Column;
                if (Row < 0 || Column < 0 || index >= _grid.CellCount)
                {
                    index = -1;
                }

                return index;
            }
        }

        /// <summary>
        /// Check whether this grid cell is valid
        /// </summary>
        public bool IsValid => Index >= 0;
        
        /// <summary>
        /// Get location of this cell.
        /// This assumes that IsValid is true
        /// </summary>
        public Point Location => new Point(
            Column * (_grid.CellSize.Width + _grid.CellMargin.Width),
            Row * (_grid.CellSize.Height + _grid.CellMargin.Height)
        );

        /// <summary>
        /// Get size of this cell
        /// </summary>
        public Size Size => _grid.CellSize;

        /// <summary>
        /// Get bounding box of this cell
        /// </summary>
        public Rectangle Bounds => new Rectangle(Location, Size);

        private readonly Grid _grid;
        
        public GridCell(Grid grid, int row, int column)
        {
            _grid = grid;
            Row = row;
            Column = column;
        }
    }

    internal class Grid
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
        public int CellCount { get; set; } = 0;

        /// <summary>
        /// Empty space between grid cells.
        /// Note: this does not apply to the space between grid cells and the grid edge 
        ///       i.e. distance between a grid cell and an adjacent grid edge to the edge will still be 0
        /// </summary>
        public Size CellMargin { get; set; } = new Size(0, 0);

        #endregion
        
        #region Computed Public Properties

        /// <summary>
        /// Number of columns in the grid.
        /// This will always be >= 1.
        /// </summary>
        public int ColumnCount => Math.Max((_width + CellMargin.Width) / (MinCellWidth + CellMargin.Width), 1);

        /// <summary>
        /// Number of rows in the grid.
        /// This will always be >= 0.
        /// </summary>
        public int RowCount => MathUtils.RoundUpDiv(CellCount, ColumnCount);

        /// <summary>
        /// Actual size of each cell in the grid
        /// </summary>
        public Size CellSize => new Size(
            (_width + CellMargin.Width) / ColumnCount - CellMargin.Width,
            CellHeight
        );

        /// <summary>
        /// Size of the entire grid
        /// </summary>
        public Size GridSize => new Size(
            _width,
            RowCount == 0 ? 0 : RowCount * CellHeight + (RowCount - 1) * CellMargin.Height
        );

        #endregion 

        /// <summary>
        /// Current with of the grid
        /// </summary>
        private int _width = 200;
       
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
        /// Determine index of a cell or a gap.
        /// 
        /// The grid along given axis looks like this:
        /// |---- cell ----|- gap -|---- cell ----|- gap -|---- cell ----|
        /// 
        /// This function computes an index of a cell or a gap from a location.
        /// </summary>
        /// <param name="location">Location along an axis</param>
        /// <param name="axisCellSize">Cell size in this axis</param>
        /// <param name="axisGapSize">Gap size in this axis</param>
        /// <returns>
        ///     Index of a cell or a gap.
        ///     Even indices are cells, odd indices are gaps.
        /// </returns>
        private int FindCellInAxis(int location, int axisCellSize, int axisGapSize)
        {
            var index = location / (axisCellSize + axisGapSize);
            var offset = location % (axisCellSize + axisGapSize);
            return index * 2 + (offset > axisCellSize ? 1 : 0);
        }

        private int FindHorizontal(int x)
        {
            return FindCellInAxis(x, CellSize.Width, CellMargin.Width);
        }

        private int FindVertical(int y)
        {
            return FindCellInAxis(y, CellSize.Height, CellMargin.Height);
        }

        /// <summary>
        /// Get grid cell in given row and column.
        /// </summary>
        /// <param name="row">Grid row</param>
        /// <param name="column">Grid column</param>
        /// <returns>
        ///     Grid cell.
        ///     If row or column is out of bounds of this grid, 
        ///     the row and column in the result will be -1.
        /// </returns>
        public GridCell GetCell(int row, int column)
        {
            if (row < 0 || column < 0 ||
                row >= RowCount || column >= ColumnCount)
            {
                return new GridCell(this, -1, -1);
            }
            return new GridCell(this, row, column);
        }

        /// <summary>
        /// Get cell at given index
        /// </summary>
        /// <param name="index">Index of a cell</param>
        /// <returns></returns>
        public GridCell GetCell(int index)
        {
            if (index < 0 || index >= CellCount)
                return GetCell(-1, -1);
            return GetCell(index / ColumnCount, index % ColumnCount);
        }
        
        /// <summary>
        /// Align given rectangle to cell boundaries.
        /// Location of the aligned rectangle will be a top left corner of a cell.
        /// Size of the aligned rectangle will be a multiple of cell size + margin size.
        /// </summary>
        /// <param name="bounds">Rectangle to align</param>
        /// <returns>Aligned rectangle</returns>
        public Rectangle AlignToCellBoundaries(Rectangle bounds)
        {
            var width = CellSize.Width + CellMargin.Width;
            var height = CellSize.Height + CellMargin.Height;
            bounds.X = (bounds.X / width) * width;
            bounds.Y = (bounds.Y / height) * height;
            bounds.Width = (MathUtils.RoundUpDiv(bounds.X, width) * width) - bounds.X;
            bounds.Height = (MathUtils.RoundUpDiv(bounds.Y, height) * height) - bounds.Y;
            return bounds;
        }

        /// <summary>
        /// Given a point in space, find grid cell (row and comlumn)
        /// </summary>
        /// <param name="location">Point in space</param>
        /// <returns>
        ///     Coordinates of a grid cell.
        ///     Row and column will be -1 if there is no grid cell at given location.
        /// </returns>
        public GridCell GetCellAt(Point location)
        {
            if (location.X < 0 || location.Y < 0)
            {
                return GetCell(-1, -1);
            }

            var row = FindVertical(location.Y);
            var column = FindHorizontal(location.X);
            if (row % 2 != 0 || column % 2 != 0)
            {
                // odd indicies are indices of gaps
                return GetCell(-1, -1);
            }

            // we are interested in an index of a cell, not index of a cell or gap
            return GetCell(row / 2, column / 2);
        }
        
        /// <summary>
        /// Iterate over grid cells in given area.
        /// </summary>
        /// <param name="bounds">Area of the grid</param>
        /// <returns>Grid cells in given area</returns>
        public IEnumerable<GridCell> GetCellsInBounds(Rectangle bounds)
        {
            // Compute intersection of bounds with the bounding box of the grid.
            var minX = Math.Max(bounds.X, 0);
            var maxX = Math.Min(bounds.X + bounds.Width - 1, GridSize.Width);
            var minY = Math.Max(bounds.Y, 0);
            var maxY = Math.Min(bounds.Y + bounds.Height - 1, GridSize.Height);
            if (minX > maxX || minY > maxY)
                yield break; // the intersection is empty

            // find first column and column after the last column
            var beginColumn = MathUtils.RoundUpDiv(FindHorizontal(minX), 2);
            var endColumn = Math.Min(
                FindHorizontal(maxX) / 2 + 1,
                ColumnCount
            );

            // find first row and row after the last row
            var beginRow = MathUtils.RoundUpDiv(FindVertical(minY), 2);
            var endRow = Math.Min(
                FindVertical(maxY) / 2 + 1,
                RowCount
            );

            for (var row = beginRow; row < endRow; ++row)
            {
                var lastColumn = endColumn;
                if (row + 1 == RowCount && CellCount % ColumnCount != 0)
                {
                    // this is the last row and it is not full
                    lastColumn = Math.Min(CellCount % ColumnCount, endColumn);
                }
                for (var column = beginColumn; column < lastColumn; ++column)
                {
                    yield return new GridCell(this, row, column);
                }
            }
        }
    }
}
