using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
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
        /// <summary>
        /// Padding between thumbnail
        /// </summary>
        public Size ItemPadding { get; set; } = new Size(8, 8);
        
        private HashSet<int> _selectedItems = new HashSet<int>();
        private IReadOnlyList<ResultItem> _items;
        private Size _itemSize;

        #region Graphics settings

        private Brush _highlightBrush;
        private Pen _highlightBorderPen;
        private int _highlightBorderSize = 1;

        private Brush _selectionBrush;
        private Pen _selectionBorderPen;
        private int _selectionBorderSize = 1;

        #endregion

        private class RangeSelectionState
        {
            /// <summary>
            /// true iff the user is currenlty selecting items with mouse
            /// </summary>
            public bool IsActive = false;

            /// <summary>
            /// First point of the range selection
            /// </summary>
            public Point StartPoint;

            /// <summary>
            /// Get selection boundign box given the end point of the selection
            /// </summary>
            /// <param name="endPoint">End point of the selection</param>
            /// <returns>Bounding box</returns>
            public Rectangle GetBounds(Point endPoint)
            {
                var minX = Math.Min(StartPoint.X, endPoint.X);
                var maxX = Math.Max(StartPoint.X, endPoint.X);
                var minY = Math.Min(StartPoint.Y, endPoint.Y);
                var maxY = Math.Max(StartPoint.Y, endPoint.Y);
                return new Rectangle(minX, minY, maxX - minX, maxY - minY);
            }
        }

        private RangeSelectionState _selectionState = new RangeSelectionState();

        public ThumbnailGridControl()
        {
            InitializeComponent();

            _highlightBrush = new SolidBrush(Color.FromArgb(226, 241, 255));
            _highlightBorderPen = new Pen(Color.FromArgb(221, 232, 248), _highlightBorderSize);

            _selectionBrush = new SolidBrush(Color.FromArgb(221, 232, 248));
            _selectionBorderPen = new Pen(Color.FromArgb(210, 220, 236), _selectionBorderSize);

            GridPanel.CellRedraw += GridPanel_CellRedraw;
            GridPanel.CellMouseEnter += GridPanel_CellMouseEnter;
            GridPanel.CellMouseLeave += GridPanel_CellMouseLeave;
        }

        #region View interface 

        public event EventHandler SelectionChanged;
        public event EventHandler<KeyEventArgs> HandleShortcuts;

        public IReadOnlyList<ResultItem> Items
        {
            get => _items;
            set
            {
                // dispose old items
                if (_items != null)
                {
                    foreach (var item in _items)
                    {
                        item.Dispose();
                    }
                }

                // add new items
                _items = value;
                if (_items == null)
                {
                    GridPanel.Grid.CellCount = 0;
                    return;
                }
                GridPanel.Grid.CellCount = _items.Count;
            }
        }

        public Size ItemSize
        {
            get => _itemSize;
            set
            {
                _itemSize = value;
                GridPanel.Grid.MinCellWidth = _itemSize.Width + ItemPadding.Width * 2;
                GridPanel.Grid.CellHeight = _itemSize.Height + ItemPadding.Height * 2;
            }
        }
        
        public IEnumerable<int> SelectedItems => _selectedItems;

        public void ClearSelection()
        {
            // invalidate all cells in current selection
            foreach (var index in _selectedItems)
            {
                GridPanel.Invalidate(index);
            }

            // clear current selection
            _selectedItems.Clear();
        }

        public void AddToSelection(IEnumerable<int> items)
        {
            foreach (var item in items)
            {
                _selectedItems.Add(item);
                GridPanel.Invalidate(item);
            }
        }

        #endregion

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            Items = null; // dispose items
            }
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        /// <summary>
        /// Calculate the largest image size such that it fits in <paramref name="thumbnailAreaSize"/> and 
        /// preserves the aspect ratio of <paramref name="originalSize"/>
        /// </summary>
        /// <param name="originalSize">Actual size of the image</param>
        /// <param name="thumbnailAreaSize">Size of the area where the image will be drawn</param>
        /// <returns>
        ///     Size of the resized image s.t. it fits in <paramref name="thumbnailAreaSize"/> 
        ///     and preserves the aspect ratio of <paramref name="originalSize"/>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Arguments contain negative size or <paramref name="originalSize"/>.Height is 0
        /// </exception>
        private Size GetThumbnailSize(Size originalSize, Size thumbnailAreaSize)
        {
            if (originalSize.Width < 0 || originalSize.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(originalSize));
            if (thumbnailAreaSize.Width < 0 || thumbnailAreaSize.Height < 0)
                throw new ArgumentOutOfRangeException(nameof(thumbnailAreaSize));

            var aspectRatio = originalSize.Width / (double)originalSize.Height;
            if (aspectRatio > 1)
            {
                thumbnailAreaSize.Height = (int)(thumbnailAreaSize.Width / aspectRatio);
            }
            else
            {
                thumbnailAreaSize.Width = (int)(thumbnailAreaSize.Height * aspectRatio);
            }

            return thumbnailAreaSize;
        }

        private Point GetThumbnailLocation(Rectangle cellBounds)
        {
            return new Point(
                // align the thumbnail to the center horizontally
                cellBounds.Location.X + (cellBounds.Width - ItemSize.Width) / 2,
                // align the thumbnail to the bottom vertically
                cellBounds.Location.Y + (cellBounds.Height - ItemSize.Height - ItemPadding.Height)
            );
        }
        
        private void GridPanel_CellRedraw(object sender, GridPanel.CellRedrawEventArgs e)
        {
            // find thumbnail
            var item = Items[e.GridCell.Index];

            e.Graphics.FillRectangle(Brushes.White, e.Bounds);

            if (SelectedItems.Contains(e.GridCell.Index))
            {
                // draw selection 
                e.Graphics.FillRectangle(_selectionBrush, e.Bounds.Shrink(_selectionBorderSize));
                e.Graphics.DrawRectangle(_selectionBorderPen, e.Bounds.Shrink(_selectionBorderSize));
            }
            else if (e.GridCell.Index == GridPanel.ActiveCell.Index)
            {
                // draw highlight
                e.Graphics.FillRectangle(_highlightBrush, e.Bounds.Shrink(_highlightBorderSize));
                e.Graphics.DrawRectangle(_highlightBorderPen, e.Bounds.Shrink(_highlightBorderSize));
            }
            
            // draw the thumbnail
            var thumbnailSize = GetThumbnailSize(item.Thumbnail.Size, ItemSize);
            var thumbnailLocation = GetThumbnailLocation(e.Bounds);
            e.Graphics.DrawImage(item.Thumbnail, new Rectangle(thumbnailLocation, thumbnailSize));

            // draw name
            var nameLocation = new Point(
                thumbnailLocation.X, 
                thumbnailLocation.Y + thumbnailSize.Height + ItemPadding.Height);
            var nameSize = new Size(ItemSize.Width, ItemSize.Height - thumbnailSize.Height - ItemPadding.Height * 2);
            var nameForamt = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
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
            ClearSelection();
            _selectionState.IsActive = true;
            _selectionState.StartPoint = GridPanel.UnprojectLocation(e.Location);
        }

        private void GridPanel_MouseUp(object sender, MouseEventArgs e)
        {
            _selectionState.IsActive = false;
            if (SelectionChanged != null)
            {
                SelectionChanged(this, EventArgs.Empty);
            }
        }

        private void GridPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_selectionState.IsActive)
            {
                return;
            }
            var endPoint = GridPanel.UnprojectLocation(e.Location);

            // remove all items from selection and invalidate their location
            ClearSelection();

            // add new cells to the selection
            var bounds = _selectionState.GetBounds(endPoint);
            foreach (var cell in GridPanel.Grid.GetCellsInBounds(bounds))
            {
                _selectedItems.Add(cell.Index);
                GridPanel.Invalidate(cell);
            }
        }

        private void GridPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (HandleShortcuts != null)
            {
                HandleShortcuts(sender, e);
            }
        }
    }
}
