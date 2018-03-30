using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    public partial class ThumbnailGridControl : DockContent, IQueryResultView
    {
        #region Graphics settings

        private readonly Brush _highlightedItemFill;
        private readonly Pen _highlightedItemStroke;
        private readonly int _highlightedItemBorderSize = 1;

        private readonly Brush _selectedItemFill;
        private readonly Pen _selectedItemStroke;
        private readonly int _selectedItemBorderSize = 1;

        private readonly Brush _rangeSelectionFill;
        private readonly Pen _rangeSelectionStroke;

        #endregion

        /// <summary>
        /// Height of the area in a grid cell reserved for file name
        /// </summary>
        public int NameHeight { get; } = 25;

        /// <summary>
        /// Minimal distance between cell border and cell content (thumbnail + file name label)
        /// </summary>
        public Size GridCellPadding { get; } = new Size(5, 5);

        #region Control state

        private List<ResultItemView> _items = new List<ResultItemView>();
        private ISet<int> _selection = new HashSet<int>();
        private RangeSelection _rangeSelection = new RangeSelection();

        #endregion

        private class RangeSelection
        {
            /// <summary>
            /// Start point of the selection
            /// </summary>
            public Point StartPoint { get; private set; }

            /// <summary>
            /// Current end point of the selection
            /// </summary>
            public Point EndPoint { get; private set; }

            /// <summary>
            /// true iff user is currently selecting itmes 
            /// </summary>
            public bool IsActive { get; private set; } = false;

            /// <summary>
            /// Get current bounding rectangle
            /// </summary>
            public Rectangle Bounds => GetBounds(EndPoint);

            /// <summary>
            /// Start a new selection
            /// </summary>
            /// <param name="point">Point</param>
            public void Start(Point point)
            {
                IsActive = true;
                StartPoint = point;
                EndPoint = point;
            }

            /// <summary>
            /// Move current end to <paramref name="endPoint"/>
            /// </summary>
            /// <param name="endPoint">New endpoint</param>
            public void MoveTo(Point endPoint)
            {
                EndPoint = endPoint;
            }

            /// <summary>
            /// End range selection
            /// </summary>
            public void End()
            {
                IsActive = false;
            }

            /// <summary>
            /// Get selection bounds
            /// </summary>
            /// <param name="endPoint">End point of the range selection</param>
            /// <returns>Bounds of the selection</returns>
            public Rectangle GetBounds(Point endPoint)
            {
                var minX = Math.Min(StartPoint.X, endPoint.X);
                var maxX = Math.Max(StartPoint.X, endPoint.X);
                var minY = Math.Min(StartPoint.Y, endPoint.Y);
                var maxY = Math.Max(StartPoint.Y, endPoint.Y);
                return new Rectangle(minX, minY, maxX - minX, maxY - minY);
            }
        }
        
        public ThumbnailGridControl()
        {
            InitializeComponent();

            GridPanel.Grid.CellMargin = new Size(8, 8);
            
            _highlightedItemFill = new SolidBrush(Color.FromArgb(226, 241, 255));
            _highlightedItemStroke = new Pen(Color.FromArgb(221, 232, 248), _highlightedItemBorderSize);

            _selectedItemFill = new SolidBrush(Color.FromArgb(221, 232, 248));
            _selectedItemStroke = new Pen(Color.FromArgb(210, 220, 236), _selectedItemBorderSize);

            _rangeSelectionFill = new SolidBrush(Color.FromArgb(150, 79, 143, 247));
            _rangeSelectionStroke = new Pen(Color.FromArgb(255, 79, 143, 247));

            GridPanel.CellRedraw += GridPanel_CellRedraw;
            GridPanel.CellMouseEnter += GridPanel_CellMouseEnter;
            GridPanel.CellMouseLeave += GridPanel_CellMouseLeave;
        }

        #region View interface 
        
        public event EventHandler CloseView;
        public event EventHandler<SelectionEventArgs> SelectionStart;
        public event EventHandler<SelectionEventArgs> SelectionChanged;
        public event EventHandler<ItemEventArgs> OpenItem;
        public event EventHandler<RenameItemEventArgs> RenameItem;
        public event EventHandler<SelectionEventArgs> DeleteItems;
        public event KeyEventHandler HandleKeyDown
        {
            add => GridPanel.KeyDown += value;
            remove => GridPanel.KeyDown -= value;
        }
        public event KeyEventHandler HandleKeyUp
        {
            add => GridPanel.KeyUp += value;
            remove => GridPanel.KeyUp -= value;
        }

        public void LoadItems(IEnumerable<ResultItemView> items)
        {
            // update view data
            ClearItems();
            _items.AddRange(items);

            // update UI
            GridPanel.Grid.CellCount = _items.Count;
            Update();
        }

        public void SetItemSize(Size newSize)
        {
            GridPanel.Grid.MinCellWidth = newSize.Width + GridCellPadding.Width * 2;
            GridPanel.Grid.CellHeight = newSize.Height + NameHeight + GridCellPadding.Height * 2;
            Update();
        }

        public void SetItemsInSelection(IEnumerable<int> items)
        {
            // invalidate old selection
            foreach (var item in _selection)
            {
                GridPanel.Invalidate(item);
            }

            _selection.Clear();
            _selection.UnionWith(items);

            // invalidate new selection
            foreach (var item in _selection)
            {
                GridPanel.Invalidate(item);
            }
        }
        
        #endregion

        private IEnumerable<int> GetRangeSelection()
        {
            return GridPanel.Grid.GetCellsInBounds(_rangeSelection.Bounds).Select(cell => cell.Index);
        }
        
        private void ClearItems()
        {
            foreach (var item in _items)
            {
                item.Dispose();
            }
            _items.Clear();
            _selection.Clear();
        }
        
        private void MoveFilesInSelection()
        {
            var files = _selection.Select(index => _items[index].Path).ToArray();
            var data = new DataObject(DataFormats.FileDrop, files);
            DoDragDrop(data, DragDropEffects.Copy);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearItems();
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        
        private Point GetThumbnailLocation(Rectangle cellBounds, Size thumbnailSize)
        {
            return new Point(
                // align the thumbnail to the center horizontally
                cellBounds.Location.X + (cellBounds.Width - thumbnailSize.Width) / 2,
                // align the thumbnail to the bottom vertically
                cellBounds.Location.Y + (cellBounds.Height - NameHeight - thumbnailSize.Height) / 2
            );
        }
        
        private void GridPanel_CellRedraw(object sender, GridPanel.CellRedrawEventArgs e)
        {
            // find thumbnail
            var item = _items[e.GridCell.Index];

            // clear grid cell area
            e.Graphics.FillRectangle(Brushes.White, e.Bounds);

            if (_selection.Contains(e.GridCell.Index))
            {
                // draw selection 
                e.Graphics.FillRectangle(_selectedItemFill, e.Bounds.Shrink(_selectedItemBorderSize));
                e.Graphics.DrawRectangle(_selectedItemStroke, e.Bounds.Shrink(_selectedItemBorderSize));
            }
            else if (e.GridCell.Index == GridPanel.ActiveCell.Index)
            {
                // draw highlight
                e.Graphics.FillRectangle(_highlightedItemFill, e.Bounds.Shrink(_highlightedItemBorderSize));
                e.Graphics.DrawRectangle(_highlightedItemStroke, e.Bounds.Shrink(_highlightedItemBorderSize));
            }
            
            // draw the thumbnail
            var thumbnailSize = item.Thumbnail.Size;
            var thumbnailLocation = GetThumbnailLocation(e.Bounds, thumbnailSize);
            // we don't really need an interpolation as we are drawing the image in its original size
            e.Graphics.InterpolationMode = InterpolationMode.Low;
            e.Graphics.DrawImage(item.Thumbnail, new Rectangle(thumbnailLocation, thumbnailSize));

            // draw name
            var nameLocation = new Point(
                e.Bounds.X + GridCellPadding.Width,
                e.Bounds.Y + (e.Bounds.Height - NameHeight) - GridCellPadding.Height
            );
            var nameSize = new Size(
                e.Bounds.Width - 2 * GridCellPadding.Width, 
                NameHeight
            );
            var nameForamt = new StringFormat{ Alignment = StringAlignment.Center };
            e.Graphics.DrawString(
                item.Name, 
                Font, 
                SystemBrushes.ControlText, 
                new Rectangle(nameLocation, nameSize),
                nameForamt);
        }

        private void GridPanel_CellMouseLeave(object sender, GridPanel.CellChangeEventArgs e)
        {
            GridPanel.Invalidate(e.OldCell);
        }

        private void GridPanel_CellMouseEnter(object sender, GridPanel.CellChangeEventArgs e)
        {
            GridPanel.Invalidate(e.NewCell);
        }
        
        private void GridPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // find cell below mouse cursor
            var activeCell = GridPanel.ActiveCell;
            if (activeCell.IsValid)
            {
                if (!_selection.Contains(activeCell.Index))
                {
                    // invalidate old selection
                    foreach (var item in _selection)
                    {
                        GridPanel.Invalidate(item);
                    }

                    // make this the only item in selection
                    SelectionStart?.Invoke(sender, new SelectionEventArgs(_selection));
                    _selection.Clear();
                    _selection.Add(activeCell.Index);
                    SelectionChanged?.Invoke(sender, new SelectionEventArgs(_selection));
                }

                // begin the move operation on files in selection
                if ((e.Button & MouseButtons.Left) != 0)
                {
                MoveFilesInSelection();
            }
            }
            else
            {
                // start a new selection
                SelectionStart?.Invoke(sender, new SelectionEventArgs(_selection));
                _rangeSelection.Start(GridPanel.UnprojectLocation(e.Location));
            }
        }

        private void GridPanel_MouseUp(object sender, MouseEventArgs e)
        {
            _rangeSelection.End();

            // invalidate range selection
            GridPanel.Invalidate(GridPanel.ProjectBounds(_rangeSelection.Bounds));
            GridPanel.Update();
        }

        private void GridPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_rangeSelection.IsActive)
            {
                // invalidate old selection
                GridPanel.Invalidate(GridPanel.ProjectBounds(_rangeSelection.Bounds));

                // change selection
                _rangeSelection.MoveTo(GridPanel.UnprojectLocation(e.Location));
                SelectionChanged?.Invoke(sender, new SelectionEventArgs(GetRangeSelection()));

                // invalidate new selection
                GridPanel.Invalidate(GridPanel.ProjectBounds(_rangeSelection.Bounds));
                GridPanel.Update();
            }
        }
        
        private void ThumbnailGridControl_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseView?.Invoke(sender, e);
        }

        private void GridPanel_Paint(object sender, PaintEventArgs e)
        {
            if (_rangeSelection.IsActive)
            {
                var bounds = GridPanel.ProjectBounds(_rangeSelection.Bounds);
                e.Graphics.FillRectangle(_rangeSelectionFill, bounds.Shrink(1));
                e.Graphics.DrawRectangle(_rangeSelectionStroke, bounds.Shrink(1));
            }
        }
    }
}
