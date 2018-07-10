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
        Image GetCurrent();

        /// <summary>
        /// Change thumbnail area size.
        /// This won't start loading the thumbnail. New thumbnail will start
        /// loading on the next call to the Current getter.
        /// </summary>
        /// <param name="newThumbnailAreaSize">New size of an area for the thumbnail</param>
        void Resize(Size newThumbnailAreaSize);
    }

    public class PhotoThumbnail : ILazyThumbnail
    {
        private readonly IImageLoader _imageLoader;
        private readonly IEntity _entity;
        private Size _thumbnailAreaSize;
        private Image _current = Default;
        private Task<Image> _loading;
        private bool _isInvalid = true;
        
        public static Image Default { get; } = Resources.DefaultThumbnail;

        public Image GetCurrent()
    { 
            if (_isInvalid)
            {
                DisposeLoading();
                _isInvalid = false;
                _loading = _imageLoader.LoadThumbnailAsync(_entity, _thumbnailAreaSize);

                // return current thumbnail if the loading does not complete immediately
                if (!_loading.IsCompleted)
                {
                    return _current;
                }
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

        public PhotoThumbnail(IImageLoader loader, IEntity entity, Size thumbnailAreaSize)
        {
            _imageLoader = loader;
            _entity = entity;
            _thumbnailAreaSize = thumbnailAreaSize;
        }

        public void Resize(Size newThumbnailAreaSize)
        {
            _thumbnailAreaSize = newThumbnailAreaSize;
            _isInvalid = true; // invalidate current thumbnail
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
