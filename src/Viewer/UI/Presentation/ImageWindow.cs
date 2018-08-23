using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Viewer.Core;
using Viewer.Data;
using Viewer.Images;
using Viewer.IO;

namespace Viewer.UI.Presentation
{
    /// <inheritdoc />
    /// <summary>
    /// Image window preloads encoded image data to an internal buffer.
    /// </summary>
    public class ImageWindow : IDisposable
    {
        private class ImageProxy : IDisposable
        {
            private readonly Task<SKBitmap> _loadTask;
            private readonly object _lock = new object();
            private bool _isDisposed = false;

            public ImageProxy(Task<SKBitmap> loadTask)
            {
                _loadTask = loadTask ?? throw new ArgumentNullException(nameof(loadTask));
            }

            public async Task<SKBitmap> LoadAsync()
            {
                var bitmap = await _loadTask.ConfigureAwait(false);
                lock (_lock)
                {
                    return _isDisposed ? null : bitmap.Copy();
                }
            }

            public void Dispose()
            {
                _loadTask?.ContinueWith(parent =>
                {
                    lock (_lock)
                    {
                        parent.Result?.Dispose();
                        _isDisposed = true;
                    }
                }, TaskScheduler.Default);
            }
        }
        
        private readonly ImageProxy[] _buffer;
        private readonly IReadOnlyList<IEntity> _entities;
        private readonly IImageLoader _imageLoader;
        private int _position;

        /// <summary>
        /// Index of the current image in presentation.
        /// </summary>
        public int CurrnetIndex => (_position + _buffer.Length / 2) % _entities.Count;

        /// <summary>
        /// Create a new image window.
        /// </summary>
        /// <param name="imageLoader">Service used to decode images</param>
        /// <param name="entities">Entities in the presentation</param>
        /// <param name="windowSize">
        ///     Size of the image window. This has to be an odd number. <c>windowSize / 2</c> images
        ///     will be preloaded before and after currently loaded image.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="windowSize"/> is negative or an even number
        /// </exception>
        public ImageWindow(IImageLoader imageLoader, IReadOnlyList<IEntity> entities, int windowSize)
        {
            if (windowSize < 0 || windowSize % 2 == 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize));

            _entities = entities;
            _imageLoader = imageLoader;
            _buffer = new ImageProxy[windowSize];
            _position = 0;
        }

        /// <summary>
        /// Get current image. This function transfers ownership of returned image to the caller
        /// (i.e., the caller has to dispose the image)
        /// </summary>
        /// <returns>
        ///     <para>
        ///     Task which decodes current image (at the time of this call). The caller has to
        ///     dispose the returned image.
        ///     </para>
        ///
        ///     <para>
        ///     The task can return null if the image has been disposed. This can happen if you call
        ///     <see cref="SetPosition"/> or <see cref="Next"/> and <see cref="Previous"/> after
        ///     this too many times so that the current image will be disposed before it can load.
        ///     </para>
        /// </returns>
        public Task<SKBitmap> GetCurrentAsync()
        {
            var imageProxy = _buffer[_buffer.Length / 2];
            return imageProxy.LoadAsync();
        }

        /// <summary>
        /// Initialize window at <paramref name="position"/>. This will start loading files from the
        /// center of the window so that current image and neighboring images are loaded first.
        /// </summary>
        /// <param name="position">New center of the window</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="position"/> is out of range.
        /// </exception>
        public void SetPosition(int position)
        {
            if (position < 0 || position >= _entities.Count)
                throw new ArgumentOutOfRangeException(nameof(position));

            // move the buffer position
            _position = position - _buffer.Length / 2;
            while (_position < 0) // the buffer can be larger than _entities
            {
                _position = _position + _entities.Count;
            }

            DisposeItemsInBuffer();

            // preload images from the center of the window
            for (var i = 0; i < _buffer.Length; ++i)
            {
                var bufferIndex = _buffer.Length / 2 + (i % 2 == 0 ? -1 : 1) * MathUtils.RoundUpDiv(i, 2);
                Preload(bufferIndex);
            }
        }

        /// <summary>
        /// Move to the next image in presentation. 
        /// </summary>
        public void Next()
        {
            if (_buffer[0] == null)
                throw new InvalidOperationException(
                    "Window is not initialized. Call SetPosition to initialize it.");

            // dispose the first item
            _buffer[0].Dispose();

            // shift the buffer
            for (var i = 0; i < _buffer.Length - 1; ++i)
            {
                _buffer[i] = _buffer[i + 1];
            }

            ++_position;
            if (_position == _entities.Count)
            {
                _position = 0;
            }
            
            Preload(_buffer.Length - 1);
        }

        /// <summary>
        /// Move to the previous image in presentation.
        /// </summary>
        public void Previous()
        {
            if (_buffer[0] == null)
                throw new InvalidOperationException(
                    "Window is not initialized. Call SetPosition to initialize it.");

            // dispose the last item
            _buffer[_buffer.Length - 1].Dispose();

            // shift the buffer
            for (var i = _buffer.Length - 1; i >= 1; --i)
            {
                _buffer[i] = _buffer[i - 1];
            }

            --_position;
            if (_position < 0)
            {
                _position = _entities.Count - 1;
            }
            
            Preload(0);
        }

        private void Preload(int bufferIndex)
        {
            if (bufferIndex < 0 || bufferIndex >= _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(bufferIndex));

            var entityIndex = (_position + bufferIndex) % _entities.Count;
            var entity = _entities[entityIndex];
            var loadTask = LoadAsync(entity);
            _buffer[bufferIndex] = new ImageProxy(loadTask);
        }

        private readonly object _lock = new object();

        private Task<SKBitmap> LoadAsync(IEntity entity)
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    return _imageLoader.LoadImage(entity);
                }
            });
        }

        private void DisposeItemsInBuffer()
        {
            foreach (var proxy in _buffer)
            {
                proxy?.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeItemsInBuffer();
        }
    }
}
