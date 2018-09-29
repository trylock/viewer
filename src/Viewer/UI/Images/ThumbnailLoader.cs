using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Viewer.Data;
using Viewer.Data.Formats.Exif;
using Viewer.Data.Storage;
using Viewer.Images;
using Viewer.IO;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Images
{
    public struct Thumbnail
    {
        /// <summary>
        /// Thumbnail picture
        /// </summary>
        public Image ThumbnailImage { get; }

        /// <summary>
        /// Size of the original image from wich <see cref="ThumbnailImage"/> is generated.
        /// </summary>
        public Size OriginalSize { get; }

        public Thumbnail(Image thumbnailImage, Size originalSize)
        {
            ThumbnailImage = thumbnailImage;
            OriginalSize = originalSize;
        }
    }

    /// <summary>
    /// Thumbnail loader loads thumbnails using <see cref="IImageLoader"/> and resizes them using
    /// <see cref="IThumbnailGenerator"/>.
    /// </summary>
    public interface IThumbnailLoader
    {
        /// <summary>
        /// Load embedded thumbnail of <paramref name="entity"/> and resize it to fit in
        /// <paramref name="thumbnailAreaSize"/> so that it presrves its aspect ratio. This does
        /// not trigger any I/O. It uses thumbnail loaded in <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">Entity for which you want to load an embedded thumbnail</param>
        /// <param name="thumbnailAreaSize">
        ///     Area for the thumbnail. Generated thumbanil will be scaled so that it fits in this area.
        /// </param>
        /// <param name="cancellationToken">Cancellation token of the load operation.</param>
        /// <returns>
        ///     <para>Task finished when the thumbnail is loaded.</para>
        ///     <para>
        ///         If <paramref name="entity"/> does not have an embedded thumbnail, this function
        ///         returns immediately with a completed task where <see cref="Thumbnail.ThumbnailImage"/>
        ///         is null and <see cref="Thumbnail.OriginalSize"/> is <see cref="Size.Empty"/>.
        ///     </para>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null</exception>
        /// <seealso cref="IImageLoader.LoadThumbnailAsync(IEntity, CancellationToken)">
        ///     For the list of possible exceptions returned by the task.
        /// </seealso>
        /// <seealso cref="IThumbnailGenerator.GetThumbnail">
        ///     For the list of possible exceptions returned by the task.
        /// </seealso>
        Task<Thumbnail> LoadEmbeddedThumbnailAsync(
            IEntity entity, 
            Size thumbnailAreaSize,
            CancellationToken cancellationToken);

        /// <summary>
        /// Load the original image of <paramref name="entity"/> and and resize it to fit in
        /// <paramref name="thumbnailAreaSize"/> so that it presrves its aspect ratio. This
        /// function reads the whole file of <paramref name="entity"/> on a background thread.
        /// I/O operations are synchronized and ordered (see <see cref="Prioritize"/>). Other
        /// operations can run in parallel.
        /// method.
        /// </summary>
        /// <remarks>
        /// Side effect of this method is that it encodes the generated thumbnail and replaces
        /// current embedded thumbnail of <paramref name="entity"/> with it.
        /// </remarks>
        /// <param name="entity">Entity whose file will be loaded.</param>
        /// <param name="thumbnailAreaSize">
        ///     Area for the thumbnail. Generated thumbanil will be scaled so that it fits in this area.
        /// </param>
        /// <param name="cancellationToken">Cancellation token of the load operation.</param>
        /// <returns>
        ///     Task finished when the thumbnail is loaded. See <see cref="IImageLoader.LoadImage(IEntity)"/>
        ///     for the list of possible exceptions returned by this task.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null</exception>
        /// <seealso cref="IFileSystem.ReadAllBytes"/>
        /// <seealso cref="IImageLoader.LoadImage(IEntity, Stream)" />
        /// <seealso cref="IThumbnailGenerator.GetThumbnail" />
        Task<Thumbnail> LoadNativeThumbnailAsync(
            IEntity entity, 
            Size thumbnailAreaSize, 
            CancellationToken cancellationToken);

        /// <summary>
        /// Increase priority of given entity so that it is loaded before any other entity
        /// waiting in the queue. This won't change any priorities if there is no entity with
        /// <paramref name="path"/> in the waiting queue. This only affects load operations
        /// started by <see cref="LoadNativeThumbnailAsync"/>.
        /// </summary>
        /// <param name="path">Path to an entity to prioritize</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        void Prioritize(string path);
    }

    [Export(typeof(IThumbnailLoader))]
    public class ThumbnailLoader : IThumbnailLoader
    {
        private readonly IImageLoader _imageLoader;
        private readonly IThumbnailGenerator _thumbnailGenerator;
        private readonly IAttributeStorage _storage;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Chained tasks which process I/O operations for <see cref="LoadRequest"/>s in the
        /// <see cref="_requests"/> list.
        /// </summary>
        private Task _loadQueue = Task.CompletedTask;

        /// <summary>
        /// Degree of parallelism for tasks which decode jpeg images and generate thumbnails from them.
        /// </summary>
        private readonly SemaphoreSlim _loaderCount = new SemaphoreSlim(Environment.ProcessorCount);

        /// <summary>
        /// Request to generate a thumbnail from the original image of <see cref="Entity"/>.
        /// Disk I/O is synchronized. Request won't compete for bandwidth.
        /// </summary>
        private class LoadRequest
        {
            public TaskCompletionSource<Thumbnail> Completion { get; } =
                new TaskCompletionSource<Thumbnail>();

            public CancellationToken Cancellation { get; }
            public IEntity Entity { get; }
            public Size ThumbnailAreaSize { get; }

            public LoadRequest(IEntity entity, Size thumbnailAreaSize, CancellationToken cancellationToken)
            {
                Entity = entity;
                ThumbnailAreaSize = thumbnailAreaSize;
                Cancellation = cancellationToken;
            }
        }

        /// <summary>
        /// Quality of thumbnails saved to the cache storage. This number has to be between 0 and 100.
        /// See <see cref="SKPixmap.Encode(SKWStream, SKBitmap, SKEncodedImageFormat, int)"/>
        /// </summary>
        public const int SavedThumbnailQuaity = 75;

        [ImportingConstructor]
        public ThumbnailLoader(
            IImageLoader imageLoader, 
            IThumbnailGenerator thumbnailGenerator, 
            IAttributeStorage storage,
            IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _imageLoader = imageLoader;
            _thumbnailGenerator = thumbnailGenerator;
            _storage = storage;
        }

        public Task<Thumbnail> LoadEmbeddedThumbnailAsync(
            IEntity entity, 
            Size thumbnailAreaSize,
            CancellationToken cancellationToken)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return LoadEmbeddedThumbnailAsyncImpl(entity, thumbnailAreaSize, cancellationToken);
        }

        private async Task<Thumbnail> LoadEmbeddedThumbnailAsyncImpl(
            IEntity entity, 
            Size thumbnailAreaSize, 
            CancellationToken cancellationToken)
        {
            using (var image = await _imageLoader.LoadThumbnailAsync(entity, cancellationToken)
                .ConfigureAwait(false))
            {
                // the entity does not have an embedded thumbnail
                if (image == null)
                {
                    return new Thumbnail(null, Size.Empty);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // the entity does have an embedded thumbnail
                using (var thumbnail = _thumbnailGenerator.GetThumbnail(image, thumbnailAreaSize))
                {
                    return new Thumbnail(thumbnail.ToBitmap(), new Size(image.Width, image.Height));
                }
            }
        }
        
        public Task<Thumbnail> LoadNativeThumbnailAsync(
            IEntity entity, 
            Size thumbnailAreaSize,
            CancellationToken cancellationToken)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var request = new LoadRequest(entity, thumbnailAreaSize, cancellationToken);
            var task = AddLoadRequest(request);

            // pick next load request once the previous request has finished
            // note: we intentionally don't wait for the task returned by ProcessNextRequestAsync()
            _loadQueue = _loadQueue.ContinueWith(_ => ProcessNextRequestAsync(), TaskContinuationOptions.None);
            
            return task;
        }

        /// <summary>
        /// Load content of an image file synchronously and then decode it asynchronously.
        /// This method blocks until _loaderCount is available.
        /// </summary>
        /// <returns>Task finished when a request is fully processed</returns>
        private async Task ProcessNextRequestAsync()
        {
            _loaderCount.Wait();
            
            LoadRequest req = null;
            try
            {
                req = ConsumeLoadRequest();
                Trace.Assert(req != null);
                if (req.Cancellation.IsCancellationRequested)
                {
                    req.Completion.SetCanceled();
                    return;
                }

                // this memory is going to end up on LOH
                var buffer = LoadFile(req.Entity.Path);

                // decode JPEG image, generate a thumbnail from it and save it back as entity
                // thumbnail in parallel
                await Task.Run(() =>
                {
                    var result = Generate(Decode(buffer, req), req);
                    req.Completion.SetResult(result);
                });
            }
            catch (OperationCanceledException)
            {
                req?.Completion.SetCanceled();
            }
            catch (Exception e)
            {
                req?.Completion.SetException(e);
            }
            finally
            {
                _loaderCount.Release();
            }
        }

        private byte[] LoadFile(string path)
        {
            return _fileSystem.ReadAllBytes(path);
        }

        private SKBitmap Decode(byte[] buffer, LoadRequest req)
        {
            req.Cancellation.ThrowIfCancellationRequested();

            using (var input = new MemoryStream(buffer))
            {
                return _imageLoader.LoadImage(req.Entity, input);
            }
        }

        private Thumbnail Generate(SKBitmap original, LoadRequest req)
        {
            using (original)
            {
                req.Cancellation.ThrowIfCancellationRequested();

                using (var thumbnail = _thumbnailGenerator.GetThumbnail(original, req.ThumbnailAreaSize))
                {
                    req.Cancellation.ThrowIfCancellationRequested();
                    SaveThumbnail(req.Entity, thumbnail);
                    var result = new Thumbnail(thumbnail.ToBitmap(), new Size(original.Width, original.Height));
                    return result;
                }
            }
        }
        
        private void SaveThumbnail(IEntity entity, SKBitmap thumbnail)
        {
            using (var dataStrem = new MemoryStream())
            using (var outputStream = new SKManagedWStream(dataStrem))
            {
                var isEncoded = SKPixmap.Encode(
                    outputStream,
                    thumbnail,
                    SKEncodedImageFormat.Jpeg,
                    SavedThumbnailQuaity);
                if (!isEncoded)
                {
                    return;
                }

                var value = new ImageValue(dataStrem.ToArray());
                var newEntity = entity.SetAttribute(new Attribute(
                    ExifAttributeReaderFactory.Thumbnail,
                    value, AttributeSource.Metadata));
                _storage.StoreThumbnail(newEntity);
            }
        }

        #region Request collection
        
        private readonly List<LoadRequest> _requests = new List<LoadRequest>();
        
        public void Prioritize(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            
            lock (_requests) 
            {
                // find the request
                var index = _requests.Count - 1;
                for (; index >= 0; --index)
                {
                    if (_requests[index].Entity.Path == path)
                    {
                        break;
                    }
                }
                if (index < 0)
                {
                    return;
                }

                // move it to the start of the collection
                var request = _requests[index];
                _requests.RemoveAt(index);
                _requests.Add(request);
            }
        }

        private LoadRequest ConsumeLoadRequest()
        {
            lock (_requests)
            {
                var req = _requests[_requests.Count - 1];
                _requests.RemoveAt(_requests.Count - 1);
                return req;
            }
        }

        private Task<Thumbnail> AddLoadRequest(LoadRequest req)
        {
            lock (_requests)
            {
                _requests.Add(req);
            }
            return req.Completion.Task;
        }

        #endregion
    }
}
