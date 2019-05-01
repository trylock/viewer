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
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PhotoSauce.MagicScaler;
using SkiaSharp;
using Viewer.Data;
using Viewer.Data.Formats.Exif;
using Viewer.IO;

namespace Viewer.Images
{
    /// <summary>
    /// Purpose of this class is to read an image of an entity and decode it correctly. This class
    /// takes into account file metadata such as the Exif Orientation tag used to rotate and flip
    /// images without altering image data. 
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
        ///     If <paramref name="entity"/> does not contain metadata about its image dimensions,
        ///     <c>new Size(1, 1)</c> will be returned.
        /// </returns>
        Size GetImageSize(IEntity entity);

        /// <summary>
        /// Decode encoded image data.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="encodedData"></param>
        /// <param name="areaSize"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        ///     File <see cref="IEntity.Path"/> does not contain a valid image or
        ///     <see cref="IEntity.Path"/> is an invalid path to a file.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     File <see cref="IEntity.Path"/> was not found.
        /// </exception>
        /// <exception cref="IOException">
        ///     File <see cref="IEntity.Path"/> is used by another process.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <see cref="IEntity.Path"/> refers to a non-file device, such as "con:", "com1:",
        ///     "lpt1:", etc. in a non-NTFS environment.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     The caller does not have the required permission.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     Unauthorized access to the file <see cref="IEntity.Path"/>.
        /// </exception>
        Image Decode(IEntity entity, Stream encodedData, Size areaSize);

        Image GetImage(IEntity entity);
    }
    
    [Export(typeof(IImageLoader))]
    public class ImageLoader : IImageLoader
    {
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
            var orientationAttr = entity.GetValue<IntValue>(ExifAttributeReaderFactory.Orientation);
            return orientationAttr?.Value ?? 1;
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
            var widthAttr = entity.GetValue<IntValue>(ExifAttributeReaderFactory.Width);
            var heightAttr = entity.GetValue<IntValue>(ExifAttributeReaderFactory.Height);
            
            var width = widthAttr?.Value ?? 1;
            var height = heightAttr?.Value ?? 1;
            var orientation = GetOrientation(entity);
            return orientation < 5 ? 
                new Size(width, height) : 
                new Size(height, width);
        }

        public Image Decode(IEntity entity, Stream encodedData, Size areaSize)
        {
            var image = DecodeImage(encodedData, areaSize, GetOrientation(entity));
            return image;
        }

        public Image GetImage(IEntity entity)
        {
            using (var input = new MemoryStream(_fileSystem.ReadAllBytes(entity.Path)))
            {
                var info = new ImageFileInfo(input);
                Size original = Size.Empty;
                if (info.Frames.Length > 0)
                {
                    original.Width = info.Frames[0].Width;
                    original.Height = info.Frames[0].Height;
                }

                input.Position = 0;

                return Decode(
                    entity, input,
                    new Size(original.Width, original.Height));
            }
        }

        private static Image DecodeImage(Stream input, Size areaSize, int orientation)
        {
            // decode the image
            var processedData = new MemoryStream();
            using (var pipeline = MagicImageProcessor.BuildPipeline(input, new ProcessImageSettings
            {
                Width = areaSize.Width,
                Height = areaSize.Height,
                ResizeMode = CropScaleMode.Max,
                Interpolation = InterpolationSettings.Lanczos,
                SaveFormat = FileFormat.Bmp,
                ColorProfileMode = ColorProfileMode.Normalize,
                OrientationMode = OrientationMode.Ignore // we will use custom transform
            }))
            {
                pipeline.AddTransform(new FormatConversionTransform(PixelFormats.Bgra32bpp));
                pipeline.AddTransform(new OrientationTransform((Orientation) orientation));
                pipeline.ExecutePipeline(processedData);
            }

            // convert the processed image to GDI
            var image = Image.FromStream(new MemoryStream(processedData.ToArray()), false);
            return image;
        }
    }
}
