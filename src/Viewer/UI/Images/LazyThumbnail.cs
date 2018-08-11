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
using Viewer.Data;
using Viewer.Images;
using Viewer.Properties;

namespace Viewer.UI.Images
{
    public interface ILazyThumbnail : IDisposable
    {
        /// <summary>
        /// Returns currently loaded thumbnail or null if there is none.
        /// This will start loading a thumbnail if a better thumbnail is available.
        /// This method is non-blocking. 
        /// </summary>
        /// <param name="thumbnailAreaSize">Size of the area for this thumbnail.</param>
        Image GetCurrent(Size thumbnailAreaSize);

        /// <summary>
        /// Causes the class to regenerate the thumbnail next time its value is requested.
        /// This can be used for resizing, for example.
        /// </summary>
        void Invalidate();
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
    public class PhotoThumbnailFactory : ILazyThumbnailFactory
    {
        private readonly IThumbnailLoader _thumbnailLoader;

        [ImportingConstructor]
        public PhotoThumbnailFactory(IThumbnailLoader thumbnailLoader)
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

        public void Invalidate()
        {
        }
    }

    public class PhotoThumbnail : ILazyThumbnail
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IThumbnailLoader _thumbnailLoader;
        private readonly IEntity _entity;
        private Image _current = Default;
        private Task<Thumbnail> _loading = Task.FromResult(new Thumbnail(Default, Size.Empty));
        private Size _loadingThumbnailAreaSize;
        private bool _isInitialized = false;

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

            // start loading a new thumbnail if necessary
            if (!_isInitialized)
            {
                _loading = _thumbnailLoader.LoadEmbeddedThumbnailAsync(_entity, thumbnailAreaSize, _cancellationToken);
                _isInitialized = true;
            }

            // if the loading has finished, replace current thumbnail
            if (_loading.Status == TaskStatus.RanToCompletion)
            {
                if (_loading.Result.Picture != null && 
                    _loading.Result.Picture != _current)
                {
                    DisposeCurrent();
                    _current = _loading.Result.Picture;
                }

                if (!IsSufficient(_loading.Result.OriginalSize, _loadingThumbnailAreaSize))
                {
                    _loading = _thumbnailLoader.LoadNativeThumbnailAsync(_entity, thumbnailAreaSize, _cancellationToken);
                }
            }
            else
            {
                _thumbnailLoader.Prioritize(_entity.Path);
            }
            
            return _current;
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

        public void Invalidate()
        {
            var current = _current;
            _loading.ContinueWith(p =>
            {
                if (p.Result.Picture != Default &&
                    p.Result.Picture != current)
                {
                    p.Result.Picture?.Dispose();
                }
            });
            _isInitialized = false;
        }

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
                if (p.Result.Picture != Default)
                {
                    p.Result.Picture?.Dispose();
                }
            });
        }
    }
}
