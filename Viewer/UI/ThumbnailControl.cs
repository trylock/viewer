using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI
{
    public partial class ThumbnailControl : UserControl
    {
        public ThumbnailControl()
        {
            InitializeComponent();
        }
        
        public void SetModel(AttributeCollection attrs)
        {
            FileNameLabel.Text = Path.GetFileNameWithoutExtension(attrs.Path);

            if (attrs.TryGetValue("thumbnail", out Attribute attr))
            {
                if (attr is ImageAttribute imageAttr)
                {
                    ThumbnailPictureBox.Image = imageAttr.Value;
                }
            }
        }

        private void ThumbnailPictureBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);

            var image = ThumbnailPictureBox.Image;
            if (image == null)
            {
                return;
            }

            // compute thumbnail dimensions
            var size = GetThumbnailSize(image.Size, ThumbnailPictureBox.Size);

            // draw the thumbnail
            g.InterpolationMode = InterpolationMode.Bicubic;
            g.DrawImage(image, 
                // align the image vertically to the center
                (ThumbnailPictureBox.Width - size.Width) / 2,
                // align the image horizontaly to the bottom
                (ThumbnailPictureBox.Height - size.Height),
                size.Width,
                size.Height);
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
                thumbnailAreaSize.Width = (int)(thumbnailAreaSize.Height  * aspectRatio);
            }

            return thumbnailAreaSize;
        }
    }
}
