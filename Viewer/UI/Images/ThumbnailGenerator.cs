using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Images
{
    [Export(typeof(IThumbnailGenerator))]
    public class ThumbnailGenerator : IThumbnailGenerator
    {
        public Image GetThumbnail(Image originalImage, Size thumbnailArea)
        {
            var thumbnailSize = GetThumbnailSize(originalImage.Size, thumbnailArea);
            var thumbnail = new Bitmap(thumbnailSize.Width, thumbnailSize.Height);
            using (var graphics = Graphics.FromImage(thumbnail))
            {
                thumbnail.SetResolution(
                    originalImage.HorizontalResolution, 
                    originalImage.VerticalResolution);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(originalImage,
                    new Rectangle(0, 0, thumbnailSize.Width, thumbnailSize.Height),
                    new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                    GraphicsUnit.Pixel);
            }

            return thumbnail;
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
        public static Size GetThumbnailSize(Size originalSize, Size thumbnailAreaSize)
        {
            if (originalSize.Width < 0 || originalSize.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(originalSize));
            if (thumbnailAreaSize.Width < 0 || thumbnailAreaSize.Height < 0)
                throw new ArgumentOutOfRangeException(nameof(thumbnailAreaSize));

            var originalAspectRatio = originalSize.Width / (double)originalSize.Height;
            var thumbnailAspectRatio = thumbnailAreaSize.Width / (double) thumbnailAreaSize.Height;
            if (originalAspectRatio >= thumbnailAspectRatio)
            {
                thumbnailAreaSize.Height = (int)(thumbnailAreaSize.Width / originalAspectRatio);
            }
            else
            {
                thumbnailAreaSize.Width = (int)(thumbnailAreaSize.Height * originalAspectRatio);
            }

            return thumbnailAreaSize;
        }
    }
}
