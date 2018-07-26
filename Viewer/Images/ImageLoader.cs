using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.IO;

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
        /// Load thumbnail asynchronously.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="entity">Entity to load</param>
        /// <param name="thumbnailAreaSize">Area for the thumbnail</param>
        /// <returns>Task which loads the thumbnail</returns>
        Task<Image> LoadThumbnailAsync(IEntity entity, Size thumbnailAreaSize);
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

        private readonly IThumbnailGenerator _thumbnailGenerator;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public ImageLoader(IThumbnailGenerator generator, IFileSystem fileSystem)
        {
            _thumbnailGenerator = generator;
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
            var orientationAttr = entity.GetAttribute(OrientationAttrName)?.Value as IntValue;
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

        /// <summary>
        /// Fix image orientation based on the orientation attribute (loaded from the exif orientation tag)
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="image"></param>
        private void FixImageOrientation(IEntity entity, Image image)
        {
            var fix = GetTransformation(entity);
            if (fix != RotateFlipType.RotateNoneFlipNone)
            {
                image.RotateFlip(fix);
            }
        }

        public Size GetImageSize(IEntity entity)
        {
            var widthAttr = entity.GetAttribute(WidthAttrName);
            var heightAttr = entity.GetAttribute(HeightAttrName);
            if (widthAttr == null || heightAttr == null)
            {
                return new Size(1, 1);
            }

            var width = (widthAttr.Value as IntValue)?.Value ?? 0;
            var height = (heightAttr.Value as IntValue)?.Value ?? 0;
            var orientation = GetOrientation(entity);
            return orientation < 5 ? 
                new Size(width, height) : 
                new Size(height, width);
        }

        public Image LoadImage(IEntity entity)
        {
            var image = Image.FromStream(new MemoryStream(_fileSystem.ReadAllBytes(entity.Path)));
            FixImageOrientation(entity, image);
            return image;
        }

        public async Task<Image> LoadThumbnailAsync(IEntity entity, Size thumbnailAreaSize)
        {
            var thumbnail = await LoadEmbeddedThumbnail(entity, thumbnailAreaSize);
            if (thumbnail == null)
            {
                thumbnail = await LoadNativeThumbnail(entity, thumbnailAreaSize);
            }

            return thumbnail;
        }

        private Image LoadThumbnail(Stream input, RotateFlipType orientation, Size thumbnailAreaSize)
        {
            using (var image = Image.FromStream(input))
            {
                if (orientation != RotateFlipType.RotateNoneFlipNone)
                {
                    image.RotateFlip(orientation);
                }

                return _thumbnailGenerator.GetThumbnail(image, thumbnailAreaSize);
            }
        }

        private Task<Image> LoadEmbeddedThumbnail(IEntity entity, Size thumbnailAreaSize)
        {
            var orientation = GetTransformation(entity);
            var thumbnail = entity.GetAttribute(ThumbnailAttrName).Value as ImageValue;
            if (thumbnail == null)
            {
                return Task.FromResult<Image>(null);
            }
            return Task.Run(() => LoadThumbnail(new MemoryStream(thumbnail.Value), orientation, thumbnailAreaSize));
        }

        private Task<Image> LoadNativeThumbnail(IEntity entity, Size thumbnailAreaSize)
        {
            var request = new ThumbnailRequest(entity, thumbnailAreaSize, GetTransformation(entity));
            _requests.Push(request);
            _requestCount.Release();
            return request.TaskCompletion.Task;
        }

        private class ThumbnailRequest
        {
            public TaskCompletionSource<Image> TaskCompletion { get; } = new TaskCompletionSource<Image>();
            public IEntity Entity { get; }
            public Size ThumbnailAreaSize { get; }
            public RotateFlipType Orientation { get; }

            public ThumbnailRequest(IEntity entity, Size thumbnailAreaSize, RotateFlipType orientation)
            {
                Entity = entity;
                ThumbnailAreaSize = thumbnailAreaSize;
                Orientation = orientation;
            }
        }

        private readonly ConcurrentStack<ThumbnailRequest> _requests = new ConcurrentStack<ThumbnailRequest>();
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
                var thumbnail = LoadThumbnail(
                    new FileStream(request.Entity.Path, FileMode.Open, FileAccess.Read),
                    request.Orientation,
                    request.ThumbnailAreaSize);
                request.TaskCompletion.SetResult(thumbnail);
            }
        }
    }
}
