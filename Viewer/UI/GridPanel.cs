using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI
{
    public partial class GridPanel : UserControl
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
        public int ColumnsCount => Math.Max(ClientSize.Width / MinCellWidth, 1);

        /// <summary>
        /// Number of rows in the grid.
        /// This will always be >= 1.
        /// </summary>
        public int RowsCount => Math.Max(MathUtils.RoundUpDiv(CellsCount, ColumnsCount), 1);

        /// <summary>
        /// Actual size of each cell in the grid
        /// </summary>
        public Size CellSize => new Size(
            ClientSize.Width / ColumnsCount,
            CellHeight
        );

        #endregion 

        #region Public Events

        public class GridCellEventArgs
        {
            /// <summary>
            /// Index of an item to redraw
            /// </summary>
            public int Index { get; }

            /// <summary>
            /// Row of the cell
            /// </summary>
            public int Row { get; }

            /// <summary>
            /// Column of the cell
            /// </summary>
            public int Column { get; }

            public GridCellEventArgs(int index, int row, int column)
            {
                Index = index;
                Row = row;
                Column = column;
            }
        }

        public class CellRedrawEventArgs : GridCellEventArgs
        {
            /// <summary>
            /// Bounds of the grid cell
            /// </summary>
            public Rectangle Bounds { get; }

            /// <summary>
            /// Current graphics object of the control
            /// </summary>
            public Graphics Graphics { get; }

            public CellRedrawEventArgs(int index, int row, int column, Rectangle bounds, Graphics g) : 
                base(index, row, column)
            {
                Graphics = g;
                Bounds = bounds;
            }
        }

        /// <summary>
        /// Event called when an item has to be redrawn
        /// </summary>
        public event EventHandler<CellRedrawEventArgs> CellRedraw;

        /// <summary>
        /// Event called when mouse cursors enters a grid cell.
        /// Event arguments describe grid cell which is currently active 
        /// (i.e. mouse cursor is above this cell)
        /// </summary>
        public event EventHandler<GridCellEventArgs> CellMouseEnter;

        /// <summary>
        /// Event called when mouse cursors leaves a grid cell.
        /// Event arguments describe grid cell which is no longer active 
        /// (i.e. the mouse cursor is not above this cell)
        /// </summary>
        public event EventHandler<GridCellEventArgs> CellMouseLeave;
        
        #endregion

        /// <summary>
        /// Index of a grid cell above which is the mouse cursor.
        /// -1 if the mouse cursor is not above any grid cell
        /// </summary>
        private int _activeCellIndex = -1;

        public GridPanel()
        {
            InitializeComponent();
        }

        #region Utility conversion functions

        /// <summary>
        /// Project UI location to visible area coordinates
        /// </summary>
        /// <param name="uiLocation">UI location</param>
        /// <returns>
        ///     Location in visible area:
        ///     [0, 0] is the top left corner of the visible area,
        ///     [ClientSize.Width, ClientSize.Height] is the bottom right corner of the visible area
        /// </returns>
        private Point ProjectLocation(Point uiLocation)
        {
            return new Point(
                uiLocation.X + AutoScrollPosition.X,
                uiLocation.Y + AutoScrollPosition.Y);
        }

        /// <summary>
        /// Compute inverse of ProjectLocation
        /// </summary>
        /// <param name="visibleAreaLocation">Point in visible area coordinates</param>
        /// <returns>Point in UI coordinates.</returns>
        private Point UnprojectLocation(Point visibleAreaLocation)
        {
            return new Point(
                visibleAreaLocation.X - AutoScrollPosition.X, 
                visibleAreaLocation.Y - AutoScrollPosition.Y);
        }

        private struct CellIndex
        {
            public int Row;
            public int Column;

            /// <summary>
            /// Linearized (Row, Column) for convenience
            /// </summary>
            public int Index;
        }

        /// <summary>
        /// Get cell index (row, column) at given location.
        /// </summary>
        /// <param name="visibleAreaLocation">Visible area location</param>
        /// <returns>Cell index at this location</returns>
        private CellIndex GetCellAt(Point visibleAreaLocation)
        {
            var uiLocation = UnprojectLocation(visibleAreaLocation);
            var row = uiLocation.Y / CellSize.Height;
            var column = uiLocation.X / CellSize.Width;
            return new CellIndex
            {
                Row = row,
                Column = column,
                Index = row * ColumnsCount + column
            };
        }

        #endregion

        #region Event Handlers
        
        private void GridPanel_Resize(object sender, EventArgs e)
        {
            // resize the scrollable area
            AutoScrollMinSize = new Size(
                0, // we don't want to have horizontal scroll bar
                RowsCount * CellSize.Height
            );

            // redraw the whole control
            Invalidate();
        }

        private void GridPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);

            // iterate over visible grid cells
            var row = -AutoScrollPosition.Y / CellSize.Height;
            var lastRow = MathUtils.RoundUpDiv(-AutoScrollPosition.Y + ClientSize.Height, CellSize.Height);
            for (; row < lastRow; ++row)
            {
                // find number of valid columns in this row
                var columnsCount = ColumnsCount;
                if (row + 1 >= RowsCount && CellsCount % ColumnsCount != 0)
                {
                    columnsCount = CellsCount % ColumnsCount;
                }

                // draw all the items in this row
                for (var column = 0; column < columnsCount; ++column)
                {
                    InvokeCellRedraw(e.Graphics, row, column);
                }
            }
        }

        private void GridPanel_MouseMove(object sender, MouseEventArgs e)
        {
            var cellIndex = GetCellAt(e.Location);

            // find index of a new active cell
            var index = -1;
            if (cellIndex.Row >= 0 && cellIndex.Column >= 0)
            {
                if (cellIndex.Index >= CellsCount)
                {
                    index = -1; // there is no cell
                }
            }

            if (index != _activeCellIndex)
            {
                // trigger mouse leave
                if (_activeCellIndex != -1 && CellMouseLeave != null)
                {
                    CellMouseLeave(this, new GridCellEventArgs(
                        _activeCellIndex, 
                        _activeCellIndex / ColumnsCount,
                        _activeCellIndex % ColumnsCount));
                }

                // trigger mouse enter
                if (index != -1 && CellMouseEnter != null)
                {
                    CellMouseEnter(this, new GridCellEventArgs(index, cellIndex.Row, cellIndex.Column));
                }
            }
        }

        #endregion

        private void InvokeCellRedraw(Graphics g, int row, int column)
        {
            if (CellRedraw == null)
            {
                return;
            }

            var bounds = new Rectangle(
                ProjectLocation(new Point(
                    column * CellSize.Width,
                    row * CellSize.Height
                )),
                CellSize
            );
            var args = new CellRedrawEventArgs(row * ColumnsCount + column, row, column, bounds, g);
            CellRedraw(this, args);
        }
    }
}
