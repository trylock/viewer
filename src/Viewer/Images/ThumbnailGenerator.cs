using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace Viewer.Images
{
    public interface IThumbnailGenerator
    {
        /// <summary>
        /// Generate thumbnail image for <paramref name="original"/>.
        /// The thumbnail is scaled down/up to fill in <paramref name="thumbnailArea"/> in one dimension.
        /// </summary>
        /// <param name="original">Original image</param>
        /// <param name="thumbnailArea">Size of an area for the thumbnail</param>
        /// <returns>Thumbnail image</returns>
        /// <exception cref="ArgumentNullException"><paramref name="original"/> is null</exception>
        SKBitmap GetThumbnail(SKBitmap original, Size thumbnailArea);
    }

    [Export(typeof(IThumbnailGenerator))]
    public class ThumbnailGenerator : IThumbnailGenerator
    {
        public SKBitmap GetThumbnail(SKBitmap originalImage, Size thumbnailArea)
        {
            if (originalImage == null)
                throw new ArgumentNullException(nameof(originalImage));

            var originalSize = new Size(originalImage.Width, originalImage.Height);
            var thumbnailSize = GetThumbnailSize(originalSize, thumbnailArea);
            SKBitmap thumbnail = null;
            try
            {
                thumbnail = originalImage.Resize(
                    new SKImageInfo(thumbnailSize.Width, thumbnailSize.Height), 
                    SKFilterQuality.Medium);
            }
            catch (Exception)
            {
                thumbnail?.Dispose();
                throw;
            }

            return thumbnail;
        }
        
        /// <summary>
        /// Calculate the largest image size such that it fits in
        /// <paramref name="thumbnailAreaSize"/> and preserves the aspect ratio of
        /// <paramref name="originalSize"/>.
        /// </summary>
        /// <param name="originalSize">Actual size of the image</param>
        /// <param name="thumbnailAreaSize">Size of the area where the image will be drawn</param>
        /// <returns>
        /// Size of the resized image s.t. it fits in <paramref name="thumbnailAreaSize"/> 
        /// and preserves the aspect ratio of <paramref name="originalSize"/>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Arguments contain negative size or <paramref name="originalSize"/>.Height is 0
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
                thumbnailAreaSize.Height = Math.Max(
                    (int)(thumbnailAreaSize.Width / originalAspectRatio), 1);
            }
            else
            {
                thumbnailAreaSize.Width = Math.Max(
                    (int)(thumbnailAreaSize.Height * originalAspectRatio), 1);
            }

            return thumbnailAreaSize;
        }
    }
}
