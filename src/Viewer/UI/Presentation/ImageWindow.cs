using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private class ImageProxy
        {
            /// <summary>
            /// Entity of this image.
            /// </summary>
            public IEntity Entity { get; }

            /// <summary>
            /// Raw image data loaded from a file. The task can potentially throw many exceptions.
            /// See <see cref="IFileSystem.ReadAllBytesAsync"/>
            /// </summary>
            public Task<byte[]> Data { get; }

            public ImageProxy(IEntity entity, Task<byte[]> data)
            {
                Entity = entity;
                Data = data;
            }
        }
        
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1);
        private readonly ImageProxy[] _buffer;
        private readonly IReadOnlyList<IEntity> _entities;
        private readonly IImageLoader _imageLoader;
        private readonly IFileSystem _fileSystem;
        private int _position;

        /// <summary>
        /// Index of the current image in presentation.
        /// </summary>
        public int CurrnetIndex => (_position + _buffer.Length / 2) % _entities.Count;

        /// <summary>
        /// Create a new image window.
        /// </summary>
        /// <param name="imageLoader">Service used to decode images</param>
        /// <param name="fileSystem">Service used to load image content</param>
        /// <param name="entities">Entities in the presentation</param>
        /// <param name="windowSize">
        ///     Size of the image window. This has to be an odd number. <c>windowSize / 2</c> images
        ///     will be preloaded before and after currently loaded image.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowSize"/> is negative or an even number</exception>
        public ImageWindow(IImageLoader imageLoader, IFileSystem fileSystem, IReadOnlyList<IEntity> entities, int windowSize)
        {
            if (windowSize < 0 || windowSize % 2 == 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize));

            _entities = entities;
            _imageLoader = imageLoader;
            _fileSystem = fileSystem;
            _buffer = new ImageProxy[windowSize];
            _position = 0;
        }

        /// <summary>
        /// Get current image. This function transfers ownership of returned image to the caller
        /// (i.e., the caller has to dispose the image)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Task which decodes current image. The caller has to dispose the returned image.</returns>
        public async Task<Bitmap> GetCurrentAsync(CancellationToken cancellationToken)
        {
            // load file content
            var imageProxy = _buffer[_buffer.Length / 2];
            var data = await imageProxy.Data.ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // decode the image
            using (var stream = new MemoryStream(data))
            using (var image = _imageLoader.DecodeImage(imageProxy.Entity, stream))
            {
                return image.ToBitmap();
            }
        }

        /// <summary>
        /// Initialize window at <paramref name="position"/>. This will start loading files from the
        /// center of the window so that current image and neighboring images are loaded first.
        /// </summary>
        /// <param name="position">New center of the window</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is out of range.</exception>
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
            var loadTask = ReadAllBytesAsync(entity.Path);
            _buffer[bufferIndex] = new ImageProxy(entity, loadTask);
        }

        private async Task<byte[]> ReadAllBytesAsync(string path)
        {
            await _sync.WaitAsync().ConfigureAwait(false);
            try
            {
                return await _fileSystem.ReadAllBytesAsync(path);
            }
            finally
            {
                _sync.Release();
            }
        }

        public void Dispose()
        {
            _sync.Dispose();
        }
    }
}
