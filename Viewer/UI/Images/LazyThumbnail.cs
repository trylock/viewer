using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
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
        /// </summary>
        /// <param name="thumbnailAreaSize">Size of the area for this thumbnail.</param>
        Image GetCurrent(Size thumbnailAreaSize);

        /// <summary>
        /// Causes the class to regenerate the thumbnail next time its value is requested.
        /// This can be used for resizing, for example.
        /// </summary>
        void Invalidate();
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
        private readonly IImageLoader _imageLoader;
        private readonly IEntity _entity;
        private Image _current = Default;
        private Task<Image> _loading;
        private bool _isInvalid = true;
        private Size _thumbnailAreaSize;
        
        public static Image Default { get; } = Resources.DefaultThumbnail;

        public Image GetCurrent(Size thumbnailAreaSize)
        {
            // invalidate the thumbnail if the thumbnail size has changed
            if (_thumbnailAreaSize != thumbnailAreaSize)
            {
                Invalidate();
            }

            _thumbnailAreaSize = thumbnailAreaSize;

            // start loading a new thumbnail
            if (_isInvalid)
            {
                DisposeLoading();
                _isInvalid = false;
                _loading = _imageLoader.LoadThumbnailAsync(_entity, thumbnailAreaSize);
            }

            // if the thumbnail loading has finished, replace the loaded thumbnail with the current thumbnail
            if (_loading?.Status == TaskStatus.RanToCompletion)
            {
                DisposeCurrent();
                _current = _loading.Result;
                _loading = null;
            }

            return _current;
        }

        public PhotoThumbnail(IImageLoader loader, IEntity entity)
        {
            _imageLoader = loader;
            _entity = entity;
        }

        public void Invalidate()
        {
            _isInvalid = true; 
        }

        public void Dispose()
        {
            DisposeCurrent();
            DisposeLoading();
        }

        private void DisposeCurrent()
        {
            if (_current != Default)
            {
                _current?.Dispose();
            }
        }

        private void DisposeLoading()
        {
            _loading?.ContinueWith(p => p.Result.Dispose());
        }
    }
}
