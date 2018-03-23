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

namespace Viewer.UI
{
    public partial class ThumbnailGridControl : UserControl
    {
        private QueryResultController _controller = new QueryResultController();
        
        /// <summary>
        /// Minimal width of a cell
        /// </summary>
        public int MinCellWidth { get; set; }

        /// <summary>
        /// Height of every cell
        /// </summary>
        public int CellHeight { get; set; }

        /// <summary>
        /// Number of cells in the grid
        /// </summary>
        public int CellsCount => _controller.Result.Count;

        /// <summary>
        /// Number of columns in the grid
        /// </summary>
        public int ColumnsCount => Math.Max(ClientSize.Width / MinCellWidth, 1);

        /// <summary>
        /// Number of rows in the grid
        /// </summary>
        public int RowsCount => Math.Max(MathUtils.RoundUpDiv(CellsCount, ColumnsCount), 1);

        /// <summary>
        /// Actual size of each cell in the grid
        /// </summary>
        public Size CellSize => new Size(
            ClientSize.Width / ColumnsCount,
            CellHeight
        );

        /// <summary>
        /// Current scroll position
        /// </summary>
        public Point ScrollPosition => new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y);

        public ThumbnailGridControl()
        {
            InitializeComponent();
            
            MinCellWidth = _controller.ThumbnailSize.Width + 8;
            CellHeight = _controller.ThumbnailSize.Height + 25;
        }

        private Point GetThumbnailLocation(int row, int column)
        {
            return new Point(
                // align the thumbnail to the center horizontally
                column * CellSize.Width + (CellSize.Width - _controller.ThumbnailSize.Width) / 2,
                // align the thumbnail to the bottom vertically 
                row * CellSize.Height + (CellSize.Height - _controller.ThumbnailSize.Height)
            );
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

        /// <summary>
        /// Project UI location to location in visible area
        /// </summary>
        /// <param name="p">UI location</param>
        /// <returns>
        ///     Location in visible area:
        ///     [0, 0] is the top left corner of the visible area,
        ///     [ClientSize.Width, ClientSize.Height] is the bottom right corner of the visible area
        /// </returns>
        private Point ProjectLocation(Point p)
        {
            return new Point(p.X - ScrollPosition.X, p.Y - ScrollPosition.Y);
        }

        private void ThumbnailGridControl_Resize(object sender, EventArgs e)
        {
            // resize the scrollable area
            AutoScrollMinSize = new Size(
                0, // we don't want to have horizontal scroll bar
                RowsCount * CellSize.Height
            );
        }

        private void ThumbnailGridControl_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            
            var row = ScrollPosition.Y / CellSize.Height;
            var lastRow = MathUtils.RoundUpDiv(ScrollPosition.Y + ClientSize.Height, CellSize.Height);
            for (; row < lastRow; ++row)
            {
                for (var column = 0; column < ColumnsCount; ++column)
                {
                    // find result item
                    var itemIndex = row * ColumnsCount + column;
                    if (itemIndex >= CellsCount)
                    {
                        break;
                    }
                    var item = _controller.Result[itemIndex];

                    // find item's thumbnail
                    if (!item.TryGetValue("thumbnail", out var thumbnailAttr) ||
                        thumbnailAttr.GetType() != typeof(ImageAttribute))
                    {
                        // TODO: load the "missing thumbnail image" instead
                        continue;
                    }
                    var image = ((ImageAttribute)thumbnailAttr).Value;

                    // draw the thumbnail
                    var thumbnailLocation = GetThumbnailLocation(row, column);
                    var size = GetThumbnailSize(image.Size, _controller.ThumbnailSize);
                    e.Graphics.DrawImage(image, new Rectangle(ProjectLocation(thumbnailLocation), size));

                    // draw the item name
                    var labelLocation = new Point(thumbnailLocation.X, thumbnailLocation.Y + size.Height + 2);
                    var labelSize = new Size(size.Width, 25);
                    var format = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString(
                        _controller.GetName(item), 
                        Font, 
                        SystemBrushes.ControlText, 
                        new Rectangle(ProjectLocation(labelLocation), labelSize),
                        format);
                }
            }
        }
    }
}
