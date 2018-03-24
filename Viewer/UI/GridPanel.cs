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
        #region Public Events

        public class CellEventArgs
        {
            /// <summary>
            /// Grid cell
            /// </summary>
            public GridCell GridCell { get; }

            public CellEventArgs(GridCell gridCell)
            {
                GridCell = gridCell;
            }
        }

        public class CellRedrawEventArgs : CellEventArgs
        {
            /// <summary>
            /// Bounds of the grid cell
            /// </summary>
            public Rectangle Bounds { get; }

            /// <summary>
            /// Current graphics object of the control
            /// </summary>
            public Graphics Graphics { get; }

            public CellRedrawEventArgs(GridCell gridCell, Rectangle bounds, Graphics g) : base(gridCell)
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
        public event EventHandler<CellEventArgs> CellMouseEnter;

        /// <summary>
        /// Event called when mouse cursors leaves a grid cell.
        /// Event arguments describe grid cell which is no longer active 
        /// (i.e. the mouse cursor is not above this cell)
        /// </summary>
        public event EventHandler<CellEventArgs> CellMouseLeave;
        
        #endregion

        /// <summary>
        /// Grid structure
        /// </summary>
        public Grid Grid { get; } = new Grid();

        /// <summary>
        /// Invalid grid cell
        /// </summary>
        public GridCell InvalidCell => new GridCell(Grid, -1, -1);

        /// <summary>
        /// Index of a grid cell above which is the mouse cursor.
        /// -1 if the mouse cursor is not above any grid cell
        /// </summary>
        private GridCell _activeGridCell;

        public GridPanel()
        {
            InitializeComponent();

            _activeGridCell = InvalidCell;
        }

        /// <summary>
        /// Invalidate single grid cell (i.e. only this cell will be redrawn)
        /// </summary>
        /// <param name="cell">Cell to invalidate</param>
        public void Invalidate(GridCell cell)
        {
            var location = ProjectLocation(cell.Location);
            Invalidate(new Rectangle(location, cell.Size));
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
        
        #endregion

        #region Event Handlers
        
        private void GridPanel_Resize(object sender, EventArgs e)
        {
            // resize the grid
            Grid.Resize(ClientSize.Width);

            // resize the scrollable area
            AutoScrollMinSize = new Size(
                0, // we don't want to have horizontal scroll bar
                Grid.RowsCount * Grid.CellSize.Height
            );

            // redraw the whole control
            Invalidate();
        }

        private void GridPanel_Paint(object sender, PaintEventArgs e)
        {
            var bounds = new Rectangle(
                UnprojectLocation(e.ClipRectangle.Location), 
                e.ClipRectangle.Size);
            foreach (var cell in Grid.GetCellsInBounds(bounds))
            {
                InvokeCellRedraw(e.Graphics, cell);
            }
        }

        private void GridPanel_MouseMove(object sender, MouseEventArgs e)
        {
            var cell = Grid.GetCellAt(UnprojectLocation(e.Location));
            if (cell.Index != _activeGridCell.Index)
            {
                // trigger mouse leave
                if (_activeGridCell.Index != -1 && CellMouseLeave != null)
                {
                    CellMouseLeave(this, new CellEventArgs(_activeGridCell));
                }

                // trigger mouse enter
                if (cell.Index != -1 && CellMouseEnter != null)
                {
                    CellMouseEnter(this, new CellEventArgs(cell));
                }
            }

            // update active cell index
            _activeGridCell = cell;
        }

        #endregion

        private void InvokeCellRedraw(Graphics g, GridCell gridCell)
        {
            if (CellRedraw == null)
            {
                return;
            }
            
            var bounds = new Rectangle(ProjectLocation(gridCell.Location), gridCell.Size);
            var args = new CellRedrawEventArgs(gridCell, bounds, g);
            CellRedraw(this, args);
        }
    }
}
