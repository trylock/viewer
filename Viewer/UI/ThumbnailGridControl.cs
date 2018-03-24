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
        /// Padding between thumbnail
        /// </summary>
        public Size ThumbnailPadding { get; set; } = new Size(8, 8);

        private GridCell _activeCell;

        public ThumbnailGridControl()
        {
            InitializeComponent();

            _activeCell = GridPanel.InvalidCell;

            GridPanel.Grid.MinCellWidth = _controller.ThumbnailSize.Width + ThumbnailPadding.Width;
            GridPanel.Grid.CellHeight = _controller.ThumbnailSize.Height + ThumbnailPadding.Height;
            GridPanel.Grid.CellsCount = _controller.Result.Count;
            GridPanel.CellRedraw += GridPanel_CellRedraw;
            GridPanel.CellMouseEnter += GridPanel_CellMouseEnter;
        }
        
        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
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
                cellBounds.Location.X + (cellBounds.Width - _controller.ThumbnailSize.Width) / 2,
                // align the thumbnail to the bottom vertically
                cellBounds.Location.Y + (cellBounds.Height - _controller.ThumbnailSize.Height - ThumbnailPadding.Height)
            );
        }
        
        private void GridPanel_CellRedraw(object sender, GridPanel.CellRedrawEventArgs e)
        {
            // find thumbnail
            var item = _controller.Result[e.GridCell.Index];
            if (!item.TryGetValue("thumbnail", out var thumbnailAttr) ||
                thumbnailAttr.GetType() != typeof(ImageAttribute))
            {
                return;
            }

            var thumbnail = ((ImageAttribute) thumbnailAttr).Value;

            // draw highlight
            if (e.GridCell.Index == _activeCell.Index)
            {
                e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
            }

            // draw the thumbnail
            var thumbnailSize = GetThumbnailSize(thumbnail.Size, _controller.ThumbnailSize);
            var thumbnailLocation = GetThumbnailLocation(e.Bounds);
            e.Graphics.DrawImage(thumbnail, new Rectangle(thumbnailLocation, thumbnailSize));
        }

        private void GridPanel_CellMouseEnter(object sender, GridPanel.CellEventArgs e)
        {
            GridPanel.Invalidate(_activeCell);
            _activeCell = e.GridCell;
            GridPanel.Invalidate(e.GridCell);
        }
    }
}
