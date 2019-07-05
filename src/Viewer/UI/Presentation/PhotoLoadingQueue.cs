using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Viewer.Data;
using Viewer.Images;

namespace Viewer.UI.Presentation
{
    /// <summary>
    /// Photo loading queue loads photos in given order. Queued load requests can be cancelled 
    /// </summary>
    internal class PhotoLoadingQueue
    {
        private readonly IImageLoader _imageLoader;

        /// <summary>
        /// Queue of image loading tasks
        /// </summary>
        private Task _queue = Task.CompletedTask;

        /// <summary>
        /// Current version of <see cref="_queue"/>. Requests remember queue version at the point
        /// when they were queued. If this version is not equal to current value, the load request
        /// will be skipped.
        /// </summary>
        private int _queueVersion;

        public PhotoLoadingQueue(IImageLoader imageLoader)
        {
            _imageLoader = imageLoader;
        }
        
        /// <summary>
        /// Cancel all requests in the queue which have not been started yet.
        /// </summary>
        public void Cancel()
        {
            ++_queueVersion;
        }

        /// <summary>
        /// Add a new load request to the queue
        /// </summary>
        /// <param name="entity">Entity to load</param>
        /// <returns>
        /// Task finished when <paramref name="entity"/> is loaded or the loading failed.
        /// </returns>
        public Task<SKBitmap> EnqueueAsync(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var version = _queueVersion;
            var promise = new TaskCompletionSource<SKBitmap>();

            _queue = _queue.ContinueWith(_ =>
            {
                // check if this request has been cancelled
                if (version != _queueVersion)
                {
                    promise.SetResult(null);
                    return;
                }

                try
                {
                    var image = SKBitmap.Decode(_imageLoader.Decode(entity));

                    // if the request has been canceled, dispose the image 
                    if (version != _queueVersion)
                    {
                        image?.Dispose();
                        image = null;
                    }

                    promise.SetResult(image);
                }
                catch (OperationCanceledException)
                {
                    promise.SetCanceled();
                }
                catch (Exception e)
                {
                    promise.SetException(e);
                }
            });

            return promise.Task;
        }
    }
}
