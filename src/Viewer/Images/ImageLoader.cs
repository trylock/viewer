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
        /// Decode encoded image data from <paramref name="encodedData"/>.
        /// </summary>
        /// <param name="entity">Image metadata</param>
        /// <param name="encodedData">Stream with encoded JPEG</param>
        /// <param name="areaSize"></param>
        /// <returns>Decoded image in BMP format</returns>
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
        byte[] Decode(IEntity entity, Stream encodedData, Size areaSize);

        /// <summary>
        /// Decode image of <paramref name="entity"/> from its file.
        /// </summary>
        /// <param name="entity">Image metadata</param>
        /// <returns>Image in BMP format</returns>
        /// <see cref="Decode(IEntity, Stream, Size)"/>
        byte[] Decode(IEntity entity);
    }
    
    [Export(typeof(IImageLoader))]
    public class ImageLoader : IImageLoader
    {
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

        public byte[] Decode(IEntity entity, Stream encodedData, Size areaSize)
        {
            var image = DecodeImage(encodedData, areaSize, GetOrientation(entity));
            return image;
        }

        public byte[] Decode(IEntity entity)
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

        private static byte[] DecodeImage(Stream input, Size areaSize, int orientation)
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

            return processedData.ToArray();
        }
    }
}
