using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Viewer.Data;
using Viewer.IO;

namespace Viewer.Images
{
    /// <summary>
    /// Purpose of this class is to read an image of an entity and decode it correctly. This class takes
    /// into account file metadata such as the Exif Orientation tag used to rotate and flip images without
    /// altering image data. 
    /// </summary>
    public interface IImageLoader
    {
        /// <summary>
        /// Get image dimensions of <paramref name="entity"/> without any additional I/O.
        /// The image size is read from image metadata loaded in <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>
        ///     Dimensions of the original image of entity (i.e., image at <see cref="IEntity.Path"/>).
        ///     If <paramref name="entity"/> does not contain metadata about its image dimensions, <c>new Size(1, 1)</c> will be returned.
        /// </returns>
        Size GetImageSize(IEntity entity);

        /// <summary>
        /// Load image of an entity entirely to main memory.
        /// </summary>
        /// <param name="entity">Entity for which you want to load the image</param>
        /// <returns>Decoded image of the entity</returns>
        /// <exception cref="ArgumentException">File <see cref="IEntity.Path"/> does not contain a valid image or <see cref="IEntity.Path"/> is an invalid path to a file.</exception>
        /// <exception cref="FileNotFoundException">File <see cref="IEntity.Path"/> was not found.</exception>
        /// <exception cref="IOException">File <see cref="IEntity.Path"/> is used by another process.</exception>
        /// <exception cref="NotSupportedException"><see cref="IEntity.Path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in a non-NTFS environment.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">Unauthorized access to the file <see cref="IEntity.Path"/>.</exception>
        SKBitmap LoadImage(IEntity entity);

        /// <summary>
        /// Load embeded thumbnail asynchronously.
        /// The thumbnail is loaded in its original size.
        /// </summary>
        /// <param name="entity">Entity to load</param>
        /// <param name="cancellationToken">Cancellation token of the load operation</param>
        /// <returns>Task which loads the thumbnail. The task will return null if there is no thumbnail.</returns>
        Task<SKBitmap> LoadThumbnailAsync(IEntity entity, CancellationToken cancellationToken);
    }
    
    [Export(typeof(IImageLoader))]
    public class ImageLoader : IImageLoader
    {
        private const string OrientationAttrName = "orientation";
        private const string WidthAttrName = "ImageWidth";
        private const string HeightAttrName = "ImageHeight";
        private const string ThumbnailAttrName = "thumbnail";

        /// <summary>
        /// Rotate/flip transformation which fixes image orientation for each possible orientation value.
        /// Index in this array is a value of the orientation tag as defined in Exif 2.2
        /// </summary>
        private readonly RotateFlipType[] _orientationFixTransform =
        {
            RotateFlipType.RotateNoneFlipNone,  // invalid orientation value

            RotateFlipType.RotateNoneFlipNone,  // top left
            RotateFlipType.RotateNoneFlipX,     // top right
            RotateFlipType.Rotate180FlipNone,   // bottom right
            RotateFlipType.Rotate180FlipX,      // bottom left
            RotateFlipType.Rotate90FlipX,       // left top
            RotateFlipType.Rotate90FlipNone,    // right top
            RotateFlipType.Rotate270FlipX,      // right bottom
            RotateFlipType.Rotate270FlipNone,   // left bottom
        };
        
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public ImageLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Get orientation value from entity 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Orientation of the entity or 0</returns>
        private static int GetOrientation(IEntity entity)
        {
            var orientationAttr = entity.GetValue<IntValue>(OrientationAttrName);
            return orientationAttr?.Value ?? 0;
        }

        /// <summary>
        /// Get image transformation which will fix the image orientation
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Transformation which fixes the image orientation</returns>
        private RotateFlipType GetTransformation(IEntity entity)
        {
            var orientation = GetOrientation(entity);
            if (orientation < 0 || orientation >= _orientationFixTransform.Length)
            {
                return RotateFlipType.RotateNoneFlipNone;
            }

            return _orientationFixTransform[orientation];
        }

        public Size GetImageSize(IEntity entity)
        {
            var widthAttr = entity.GetValue<IntValue>(WidthAttrName);
            var heightAttr = entity.GetValue<IntValue>(HeightAttrName);
            
            var width = widthAttr?.Value ?? 1;
            var height = heightAttr?.Value ?? 1;
            var orientation = GetOrientation(entity);
            return orientation < 5 ? 
                new Size(width, height) : 
                new Size(height, width);
        }

        public SKBitmap LoadImage(IEntity entity)
        {
            var orientation = GetTransformation(entity);
            using (var file = new FileStream(entity.Path, FileMode.Open, FileAccess.Read))
            {
                var image = DecodeImage(file, orientation);
                return image;
            }
        }

        public Task<SKBitmap> LoadThumbnailAsync(IEntity entity, CancellationToken cancellationToken)
        {
            // isolate these values for the thread which will generate the thumbnail
            var orientation = GetTransformation(entity);
            var thumbnail = entity.GetValue<ImageValue>(ThumbnailAttrName);
            if (thumbnail == null)
            {
                return Task.FromResult<SKBitmap>(null);
            }

            return Task.Run(() =>
            {
                using (new MemoryStream(thumbnail.Value))
                {
                    return DecodeImage(new MemoryStream(thumbnail.Value), orientation);
                }
            }, cancellationToken);
        }

        private static SKBitmap DecodeImage(Stream input, RotateFlipType orientation)
        {
            var bitmap = SKBitmap.Decode(input);
            try
            {
                return RotateFlip(bitmap, orientation);
            }
            catch (Exception)
            {
                bitmap.Dispose();
                throw;
            }
        }

        private static SKBitmap RotateFlip(SKBitmap bitmap, RotateFlipType rotateFlip)
        {
            var rotationAngle = 0;
            var flipX = 1;
            switch (rotateFlip)
            {
                case RotateFlipType.RotateNoneFlipNone:
                    break;
                case RotateFlipType.Rotate90FlipNone:
                    rotationAngle = 90;
                    break;
                case RotateFlipType.Rotate180FlipNone:
                    rotationAngle = 180;
                    break;
                case RotateFlipType.Rotate270FlipNone:
                    rotationAngle = 270;
                    break;
                case RotateFlipType.RotateNoneFlipX:
                    flipX = -1;
                    break;
                case RotateFlipType.Rotate90FlipX:
                    rotationAngle = 90;
                    flipX = -1;
                    break;
                case RotateFlipType.Rotate180FlipX:
                    rotationAngle = 180;
                    flipX = -1;
                    break;
                case RotateFlipType.Rotate270FlipX:
                    rotationAngle = 270;
                    flipX = -1;
                    break;
            }

            // don't transform the bitmap if it's not necessary
            if (flipX == 1 && rotationAngle == 0)
            {
                return bitmap;
            }

            // transform the bitmap according to the rotateFlip value
            using (bitmap)
            {
                var flipDimensions = rotationAngle == 180 || rotationAngle == 0;
                var transformed = new SKBitmap(
                    flipDimensions ? bitmap.Width : bitmap.Height,
                    flipDimensions ? bitmap.Height : bitmap.Width);
                using (var surface = new SKCanvas(transformed))
                {
                    surface.Translate(transformed.Width / 2.0f, transformed.Height / 2.0f);
                    surface.Scale(flipX, 1);
                    surface.RotateDegrees(rotationAngle);
                    surface.Translate(-bitmap.Width / 2.0f, -bitmap.Height / 2.0f);
                    surface.DrawBitmap(bitmap, 0, 0);
                }
                return transformed;
            }
        }
    }
}
