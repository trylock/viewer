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
    /// Image window preloads images within a constant distance from the current image. This
    /// reduces preceivable delay when user goes to the next or the previous image in presentation.
    /// </summary>
    /// <example>
    /// The window is created with a fixed sized buffer. This buffer has to be a positive even
    /// number.
    /// <code>
    /// var window = new ImageWindow(imageLoader, 3);
    /// </code>
    /// The window is initialized using the <see cref="Initialize"/> method. This method preloads
    /// images within a constant distance from given position. The image in the center is loaded
    /// first.
    /// <code>
    /// window.Initialize(entities, 22);
    /// </code>
    /// You can navigate throught the presentation using the <see cref="Previous"/> and
    /// <see cref="Next"/> methods. These methods dispose old images and add new photos to the
    /// loading queue. They do **not** wait for the image to load.
    /// <code>
    /// window.Previous();
    /// window.Next();
    /// window.Previous();
    /// window.Next();
    /// window.Next();
    /// window.Next();
    /// </code>
    /// You can wait for current image to load using the <see cref="GetCurrentAsync"/> method.
    /// <code>
    /// using (var image = await window.GetCurrentAsync())
    /// {
    ///     // do things with the image and dispose it
    /// }
    /// </code>
    /// </example>
    internal class ImageWindow : IDisposable
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

                // the window is allowed to dispose this image whenever (usually when it gets
                // outside of the window buffer). Therefore we have to check if it has been
                // disposed in a way which is thread safe.
                lock (_lock)
                {
                    return _isDisposed ? null : bitmap?.Copy();
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
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }
        
        private readonly ImageProxy[] _buffer;
        private int _position;

        /// <summary>
        /// All entities in the presentation
        /// </summary>
        public IReadOnlyList<IEntity> Entities { get; private set; }

        /// <summary>
        /// Index of the current image in presentation.
        /// </summary>
        public int CurrnetIndex => (_position + _buffer.Length / 2) % Entities.Count;

        /// <summary>
        /// Queue of loading images
        /// </summary>
        public PhotoLoadingQueue Queue { get; }

        /// <summary>
        /// true iff the window has been initialized. If it is false, you are only allowed to call
        /// the <see cref="Initialize"/> method.
        /// </summary>
        public bool IsInitialized => Entities != null;

        /// <summary>
        /// Create a new image window.
        /// </summary>
        /// <param name="imageLoader">Service used to decode images</param>
        /// <param name="windowSize">
        ///     Size of the image window. This has to be an odd number. <c>windowSize / 2</c> images
        ///     will be preloaded before and after currently loaded image.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="windowSize"/> is negative or an even number
        /// </exception>
        public ImageWindow(IImageLoader imageLoader, int windowSize)
        {
            if (windowSize < 0 || windowSize % 2 == 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize));

            Queue = new PhotoLoadingQueue(imageLoader);
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
        ///     <see cref="Initialize"/> or <see cref="Next"/> and <see cref="Previous"/> after
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
        /// <remarks>
        /// All pending load requests will be cancelled. All images in window buffer will be
        /// disposed.
        /// </remarks>
        /// <param name="entities">List of entities in presentation</param>
        /// <param name="position">
        /// New center of the window (index of an entity from <paramref name="entities"/>)
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="position"/> is out of range.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="entities"/> is null</exception>
        public void Initialize(IReadOnlyList<IEntity> entities, int position)
        {
            // cancel pending load requests and dispose all images which have been loaded already
            Queue.Cancel();
            DisposeItemsInBuffer();

            Entities = entities ?? throw new ArgumentNullException(nameof(entities));
            if (position < 0 || position >= Entities.Count)
                throw new ArgumentOutOfRangeException(nameof(position));

            // move the buffer position
            _position = position - _buffer.Length / 2;
            if (_position < 0)
            {
                _position = Entities.Count + (_position % Entities.Count);
            }

            // preload images from the center of the window
            for (var i = 0; i < _buffer.Length; ++i)
            {
                var bufferIndex = _buffer.Length / 2 + (i % 2 == 0 ? -1 : 1) * i.RoundUpDiv(2);
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
                    $"Window is not initialized. Call {nameof(Initialize)} to initialize it.");

            // dispose the first item
            _buffer[0].Dispose();

            // shift the buffer
            for (var i = 0; i < _buffer.Length - 1; ++i)
            {
                _buffer[i] = _buffer[i + 1];
            }

            ++_position;
            if (_position == Entities.Count)
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
                    $"Window is not initialized. Call {nameof(Initialize)} to initialize it.");

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
                _position = Entities.Count - 1;
            }
            
            Preload(0);
        }

        private void Preload(int bufferIndex)
        {
            if (bufferIndex < 0 || bufferIndex >= _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(bufferIndex));

            var entityIndex = (_position + bufferIndex) % Entities.Count;
            var entity = Entities[entityIndex];
            var loadTask = Queue.EnqueueAsync(entity);
            _buffer[bufferIndex] = new ImageProxy(loadTask);
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
            Entities = null;
            _position = 0;
            DisposeItemsInBuffer();
        }
    }
}
