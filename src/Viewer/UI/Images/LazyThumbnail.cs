using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Data;
using Viewer.Images;
using Viewer.Properties;

namespace Viewer.UI.Images
{
    /// <inheritdoc />
    /// <summary>
    /// Lazily load entity thumbnail by calling <see cref="GetCurrent"/>.
    /// </summary>
    public interface ILazyThumbnail : IDisposable
    {
        /// <summary>
        /// Returns currently loaded thumbnail or null if there is none. This will start loading
        /// a new thumbnail if a better thumbnail is available. This method is non-blocking. This
        /// method returns currently loaded image Even if a loading operation is in progress.
        /// </summary>
        /// <param name="thumbnailAreaSize">Size of the area for this thumbnail.</param>
        /// <returns>Currently loaded thumbnail or null if no thumbnail is currently loaded.</returns>
        Image GetCurrent(Size thumbnailAreaSize);
    }

    public interface ILazyThumbnailFactory
    {
        /// <summary>
        /// Create lazy thumbnail for given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken">Cancellation token used to cancel thumbnail loading tasks.</param>
        /// <returns></returns>
        ILazyThumbnail Create(IEntity entity, CancellationToken cancellationToken);
    }

    [Export(typeof(ILazyThumbnailFactory))]
    public class ThumbnailFactory : ILazyThumbnailFactory
    {
        private readonly IThumbnailLoader _thumbnailLoader;

        [ImportingConstructor]
        public ThumbnailFactory(IThumbnailLoader thumbnailLoader)
        {
            _thumbnailLoader = thumbnailLoader;
        }

        public ILazyThumbnail Create(IEntity entity, CancellationToken cancellationToken)
        {
            if (entity is DirectoryEntity)
            {
                return new DirectoryThumbnail(entity.Path);
            }
            return new PhotoThumbnail(_thumbnailLoader, entity, cancellationToken);
        }
    }

    public class DirectoryThumbnail : ILazyThumbnail
    {
        private readonly string _path;

        public static Image Default { get; } = Resources.DirectoryThumbnail;

        public DirectoryThumbnail(string path)
        {
            _path = path;
        }

        public void Dispose()
        {
        }

        public Image GetCurrent(Size thumbnailAreaSize)
        {
            return Default;
        }
    }
    
    /// <inheritdoc />
    /// <summary>
    /// Decode thumbnail from entity data. If the thumbnail is not large enough or it does not
    /// have an embedded thumbnail, the thumbnail will be loaded from file.
    /// </summary>
    public class PhotoThumbnail : ILazyThumbnail
    {
        private enum LoadingType
        {
            /// <summary>
            /// _loading is the default completed task
            /// </summary>
            None,

            /// <summary>
            /// _loading is a task which loads an embedded thumbnail
            /// </summary>
            EmbeddedThumbnail,

            /// <summary>
            /// _loading is a task which loads a native thumbnail
            /// </summary>
            NativeThumbnail
        }

        private readonly CancellationToken _cancellationToken;
        private readonly IThumbnailLoader _thumbnailLoader;
        private readonly IEntity _entity;
        private Image _current = Default;
        private Task<Thumbnail> _loading = Task.FromResult(new Thumbnail(Default, Size.Empty));
        private LoadingType _loadingType = LoadingType.None;
        private Size _loadingThumbnailAreaSize;

        /// <summary>
        /// If a loading task fails due its file being busy (opened by another process), we want to
        /// retry the load operation. This is the delay between the failed load and the next retry
        /// operation.
        /// </summary>
        public static TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Default thumbnail image. <see cref="GetCurrent"/> will return this value if no other
        /// image is currently available (e.g., on the first call to the <see cref="GetCurrent"/>)
        /// </summary>
        public static Image Default { get; } = Resources.DefaultThumbnail;

        public Image GetCurrent(Size thumbnailAreaSize)
        {
            // invalidate current thumbnail if its size is smaller
            if (_loadingThumbnailAreaSize.Width < thumbnailAreaSize.Width &&
                _loadingThumbnailAreaSize.Height < thumbnailAreaSize.Height)
            {
                Invalidate();
            }

            _loadingThumbnailAreaSize = thumbnailAreaSize;
            
            // start loading an embedded thumbnail
            if (_loadingType == LoadingType.None)
            {
                _loading = LoadEmbeddedThumbnailAsync(thumbnailAreaSize);
                _loadingType = LoadingType.EmbeddedThumbnail;
            }

            // if the loading has finished, replace current thumbnail
            if (_loading.Status == TaskStatus.RanToCompletion)
            {
                if (_loading.Result.ThumbnailImage != null &&
                    _loading.Result.ThumbnailImage != _current)
                {
                    DisposeCurrent();
                    _current = _loading.Result.ThumbnailImage;
                }

                if (!IsSufficient(_loading.Result.OriginalSize, _loadingThumbnailAreaSize))
                {
                    _loading = LoadNativeThumbnailAsync(thumbnailAreaSize);
                    _loadingType = LoadingType.NativeThumbnail;
                }
            }
            else if (_loading.Status == TaskStatus.Faulted) // the loading has failed unexpectedly
            {
                // if we have failed to load an embedded thumbnail, try loading a native thumbnail
                if (_loadingType == LoadingType.EmbeddedThumbnail)
                {
                    _loading = LoadNativeThumbnailAsync(thumbnailAreaSize);
                    _loadingType = LoadingType.NativeThumbnail;
                }
            }
            else if (_loadingType == LoadingType.NativeThumbnail &&
                     _loading.Status != TaskStatus.Canceled) // the loading is in process
            {
                _thumbnailLoader.Prioritize(_entity.Path);
            }

            return _current;
        }

        private Task<Thumbnail> LoadEmbeddedThumbnailAsync(Size thumbnailAreaSize)
        {
            return _thumbnailLoader.LoadEmbeddedThumbnailAsync(_entity, thumbnailAreaSize, _cancellationToken);
        }

        private Task<Thumbnail> LoadNativeThumbnailAsync(Size thumbnailAreaSize)
        {
            return Retry
                .Async(() => _thumbnailLoader.LoadNativeThumbnailAsync(
                    _entity,
                    thumbnailAreaSize,
                    _cancellationToken))
                .WithAttempts(5)
                .WithDelay(RetryDelay)
                .WithCancellationToken(_cancellationToken)
                .WhenExactly<IOException>()
                .Task;
        }
        
        private static bool IsSufficient(Size originalImageSize, Size thumbnailAreaSize)
        {
            return originalImageSize.Width >= thumbnailAreaSize.Width ||
                   originalImageSize.Height >= thumbnailAreaSize.Height;
        }
        
        public PhotoThumbnail(IThumbnailLoader thumbnailLoader, IEntity entity, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _thumbnailLoader = thumbnailLoader;
            _entity = entity;
        }
        
        private void Invalidate()
        {
            var current = _current;
            _loading.ContinueWith(p =>
            {
                if (p.Result.ThumbnailImage != Default &&
                    p.Result.ThumbnailImage != current)
                {
                    p.Result.ThumbnailImage?.Dispose();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
            _loadingType = LoadingType.None;
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispose current and loading thumbnail images.
        /// This does **not** cancel any pending loading operation. Use <see cref="CancellationTokenSource"/> to cancel loading operations.
        /// </summary>
        public void Dispose()
        {
            DisposeCurrent();
            DisposeLoading();
        }

        private void DisposeCurrent()
        {
            var current = _current;
            if (current != Default)
            {
                current?.Dispose();
            }
        }

        private void DisposeLoading()
        {
            _loading.ContinueWith(p =>
            {
                if (p.Result.ThumbnailImage != Default)
                {
                    p.Result.ThumbnailImage?.Dispose();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
