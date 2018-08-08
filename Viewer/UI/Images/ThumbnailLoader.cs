using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Images;
using Viewer.IO;

namespace Viewer.UI.Images
{
    public struct Thumbnail
    {
        /// <summary>
        /// Thumbnail picture
        /// </summary>
        public Image Picture { get; }

        /// <summary>
        /// Size of the original image from wich Picture is generated.
        /// </summary>
        public Size OriginalSize { get; }

        public Thumbnail(Image picture, Size originalSize)
        {
            Picture = picture;
            OriginalSize = originalSize;
        }
    }

    public interface IThumbnailLoader
    {
        /// <summary>
        /// Load embedded thumbnail of <paramref name="entity"/>.
        /// This does not trigger any IO. If <paramref name="entity"/> does not have an embedded thumbnail,
        /// this function returns null immediately.
        /// </summary>
        /// <param name="entity">Entity for which you want to load an embedded thumbnail</param>
        /// <param name="thumbnailAreaSize">Area for the thumbnail. Generated thumbanil will be scaled so that it fits in this area.</param>
        /// <returns>Task finished when the thumbnail is decoded</returns>
        Task<Thumbnail> LoadEmbeddedThumbnailAsync(IEntity entity, Size thumbnailAreaSize);

        /// <summary>
        /// Load the original image of <paramref name="entity"/> and scale it down to <paramref name="thumbnailAreaSize"/>.
        /// This function reads the whole file of <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="thumbnailAreaSize"></param>
        /// <returns></returns>
        Task<Thumbnail> LoadNativeThumbnailAsync(IEntity entity, Size thumbnailAreaSize);
    }

    [Export(typeof(IThumbnailLoader))]
    public class ThumbnailLoader : IThumbnailLoader
    {
        private readonly IImageLoader _imageLoader;
        private readonly IThumbnailGenerator _thumbnailGenerator;

        [ImportingConstructor]
        public ThumbnailLoader(IImageLoader imageLoader, IThumbnailGenerator thumbnailGenerator)
        {
            _imageLoader = imageLoader;
            _thumbnailGenerator = thumbnailGenerator;

            var thread = new Thread(Loader)
            {
                IsBackground = true
            };
            thread.Start();
        }

        public async Task<Thumbnail> LoadEmbeddedThumbnailAsync(IEntity entity, Size thumbnailAreaSize)
        {
            using (var image = await _imageLoader.LoadThumbnailAsync(entity).ConfigureAwait(false))
            {
                var thumbnail = image != null ? 
                    _thumbnailGenerator.GetThumbnail(image, thumbnailAreaSize) : 
                    null;
                return new Thumbnail(thumbnail, image?.Size ?? Size.Empty);
            }
        }

        private class LoadRequest
        {
            public TaskCompletionSource<Thumbnail> Completion { get; } = new TaskCompletionSource<Thumbnail>();

            public IEntity Entity { get; }
            public Size ThumbnailAreaSize { get; }

            public LoadRequest(IEntity entity, Size thumbnailAreaSize)
            {
                Entity = entity;
                ThumbnailAreaSize = thumbnailAreaSize;
            }
        }

        private readonly ConcurrentQueue<LoadRequest> _requests = new ConcurrentQueue<LoadRequest>();
        private readonly SemaphoreSlim _requestCount = new SemaphoreSlim(0);

        private void Loader()
        {
            for (;;)
            {
                _requestCount.Wait();

                if (!_requests.TryDequeue(out var req))
                {
                    continue;
                }

                try
                {
                    using (var image = _imageLoader.LoadImage(req.Entity))
                    {
                        var thumbnail = _thumbnailGenerator.GetThumbnail(image, req.ThumbnailAreaSize);
                        req.Completion.SetResult(new Thumbnail(thumbnail, image.Size));
                    }
                }
                catch (Exception e)
                {
                    req.Completion.SetException(e);
                }
            }
        }

        private Task<Thumbnail> AddLoadRequest(LoadRequest req)
        {
            _requests.Enqueue(req);
            _requestCount.Release();
            return req.Completion.Task;
        }
        
        public Task<Thumbnail> LoadNativeThumbnailAsync(IEntity entity, Size thumbnailAreaSize)
        {
            var req = new LoadRequest(entity, thumbnailAreaSize);
            return AddLoadRequest(req);
        }
    }
}
