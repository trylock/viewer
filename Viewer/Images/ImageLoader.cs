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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.IO;
using Image = System.Drawing.Image;

namespace Viewer.Images
{
    public interface IImageLoader
    {
        /// <summary>
        /// Get size of the image of entity without reading the whole image
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Size GetImageSize(IEntity entity);

        /// <summary>
        /// Load image of an entity entirely to main memory.
        /// Underlying file will be closed after this method finishes.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Full image of the entity</returns>
        Image LoadImage(IEntity entity);

        /// <summary>
        /// Load embeded thumbnail asynchronously.
        /// The thumbnail is loaded in its original size.
        /// </summary>
        /// <param name="entity">Entity to load</param>
        /// <returns>Task which loads the thumbnail. The task will return null if there is no thumbnail.</returns>
        Task<Image> LoadThumbnailAsync(IEntity entity);

        /// <summary>
        /// Load the whole image asynchronously.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Task which loads the image</returns>
        Task<Image> LoadImageAsync(IEntity entity);
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

            var loaderThread = new Thread(ThumbnailLoaderThread)
            {
                IsBackground = true
            };
            loaderThread.Start();
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
            if (widthAttr == null || heightAttr == null)
            {
                return new Size(1, 1);
            }

            var width = widthAttr.Value ?? 0;
            var height = heightAttr.Value ?? 0;
            var orientation = GetOrientation(entity);
            return orientation < 5 ? 
                new Size(width, height) : 
                new Size(height, width);
        }

        public Image LoadImage(IEntity entity)
        {
            var orientation = GetTransformation(entity);
            var image = DecodeImage(new MemoryStream(_fileSystem.ReadAllBytes(entity.Path)), orientation);
            return image;
        }

        public Task<Image> LoadThumbnailAsync(IEntity entity)
        {
            var orientation = GetTransformation(entity);
            var thumbnail = entity.GetValue<ImageValue>(ThumbnailAttrName);
            if (thumbnail == null)
            {
                return Task.FromResult<Image>(null);
            }
            return Task.Run(() => DecodeImage(new MemoryStream(thumbnail.Value), orientation));
        }

        public Task<Image> LoadImageAsync(IEntity entity)
        {
            var request = new LoadRequest(entity, GetTransformation(entity));
            _requests.Push(request);
            _requestCount.Release();
            return request.TaskCompletion.Task;
        }

        private Image DecodeImage(Stream input, RotateFlipType orientation)
        {
            var image = Image.FromStream(input);
            if (orientation != RotateFlipType.RotateNoneFlipNone)
            {
                image.RotateFlip(orientation);
            }

            return image;
        }

        private class LoadRequest
        {
            public TaskCompletionSource<Image> TaskCompletion { get; } = new TaskCompletionSource<Image>();
            public IEntity Entity { get; }
            public RotateFlipType Orientation { get; }

            public LoadRequest(IEntity entity, RotateFlipType orientation)
            {
                Entity = entity;
                Orientation = orientation;
            }
        }

        private readonly ConcurrentStack<LoadRequest> _requests = new ConcurrentStack<LoadRequest>();
        private readonly SemaphoreSlim _requestCount = new SemaphoreSlim(0);

        private void ThumbnailLoaderThread()
        {
            for (;;)
            {
                _requestCount.Wait();

                if (!_requests.TryPop(out var request))
                {
                    continue; // this should never happen
                }
                
                // load the original image
                var input = new MemoryStream(_fileSystem.ReadAllBytes(request.Entity.Path));
                var thumbnail = DecodeImage(input, request.Orientation);
                request.TaskCompletion.SetResult(thumbnail);
            }
        }
    }
}
