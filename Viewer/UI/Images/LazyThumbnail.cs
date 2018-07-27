using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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

    public interface ILazyThumbnailFactory
    {
        /// <summary>
        /// Create lazy thumbnail for given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        ILazyThumbnail Create(IEntity entity);
    }

    [Export(typeof(ILazyThumbnailFactory))]
    public class PhotoThumbnailFactory : ILazyThumbnailFactory
    {
        private readonly IImageLoader _imageLoader;
        private readonly IThumbnailGenerator _thumbnailGenerator;

        [ImportingConstructor]
        public PhotoThumbnailFactory(IImageLoader loader, IThumbnailGenerator generator)
        {
            _imageLoader = loader;
            _thumbnailGenerator = generator;
        }

        public ILazyThumbnail Create(IEntity entity)
        {
            return new PhotoThumbnail(_imageLoader, _thumbnailGenerator, entity);
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
        private enum State
        {
            Default,
            Embedded,
            Native
        }

        private readonly IImageLoader _imageLoader;
        private readonly IThumbnailGenerator _thumbnailGenerator;
        private readonly IEntity _entity;
        private Image _current = Default;
        private Task<(Image Picture, bool IsSufficient)> _loading = Task.FromResult((Default, false));
        private Size _loadingThumbnailAreaSize;
        private State _state = State.Default;
        
        public static Image Default { get; } = Resources.DefaultThumbnail;

        public Image GetCurrent(Size thumbnailAreaSize)
        {
            // invalidate current thumbnail if its size is smaller
            if (_loadingThumbnailAreaSize.Width < thumbnailAreaSize.Width &&
                _loadingThumbnailAreaSize.Height < thumbnailAreaSize.Height)
            {
                if (_loading.Status != TaskStatus.RanToCompletion)
                {
                    DisposeLoading();
                }
                _state = State.Default;
            }

            _loadingThumbnailAreaSize = thumbnailAreaSize;

            // start loading a new thumbnail if necessary
            if (_state == State.Default)
            {
                _loading = LoadEmbededThumbnail(thumbnailAreaSize);
                _state = State.Embedded;
            }
            else if (_state == State.Embedded ||
                     _state == State.Native)
            {
                if (_loading.Status == TaskStatus.RanToCompletion)
                {
                    if (_loading.Result.Picture != null && 
                        _loading.Result.Picture != _current)
                    {
                        DisposeCurrent();
                        _current = _loading.Result.Picture;
                    }

                    if (!_loading.Result.IsSufficient)
                    {
                        _loading = LoadNativeThumbnail(thumbnailAreaSize);
                        _state = State.Native;
                    }
                }
            }
            
            return _current;
        }
        
        private async Task<(Image, bool)> LoadNativeThumbnail(Size thumbnailAreaSize)
        {
            using (var image = await _imageLoader.LoadImageAsync(_entity).ConfigureAwait(false))
            {
                var thumbnail = _thumbnailGenerator.GetThumbnail(image, thumbnailAreaSize);
                return (thumbnail, true);
            }
        }

        private async Task<(Image, bool)> LoadEmbededThumbnail(Size thumbnailAreaSize)
        {
            using (var image = await _imageLoader.LoadThumbnailAsync(_entity).ConfigureAwait(false))
            {
                var isSufficient = image != null && (
                                    image.Width >= thumbnailAreaSize.Width ||
                                    image.Height >= thumbnailAreaSize.Height
                                );
                var thumbnail = image == null ? null : _thumbnailGenerator.GetThumbnail(image, thumbnailAreaSize);
                return (thumbnail, isSufficient);
            }
        }

        public PhotoThumbnail(IImageLoader loader, IThumbnailGenerator thumbnailGenerator, IEntity entity)
        {
            _imageLoader = loader;
            _thumbnailGenerator = thumbnailGenerator;
            _entity = entity;
        }

        public void Invalidate()
        {
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
            _loading?.ContinueWith(p =>
            {
                if (p.Result.Picture != Default)
                {
                    p.Result.Picture?.Dispose();
                }
            });
        }
    }
}
