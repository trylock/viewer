using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Images;

namespace Viewer.UI.Presentation
{
    public class ImageWindow : IDisposable
    {
        private readonly IImageLoader _imageLoader;
        private readonly IReadOnlyList<IEntity> _entities;
        private readonly Task<Image>[] _buffer;
        private int _position;

        /// <summary>
        /// Current center of the window
        /// </summary>
        public Task<Image> Current => _buffer[_buffer.Length / 2];

        /// <summary>
        /// Index of the center of the buffer in the entities array
        /// </summary>
        public int Index => BufferIndexToEntityIndex(_buffer.Length / 2);

        public ImageWindow(IImageLoader imageLoader, IReadOnlyList<IEntity> entities, int size)
        {
            _imageLoader = imageLoader;
            _entities = entities;
            _buffer = new Task<Image>[size];
            for (var i = 0; i < _buffer.Length; ++i)
            {
                _buffer[i] = Task.FromResult<Image>(null);
            }
        }

        /// <summary>
        /// Load window at given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Task<Image> LoadPositionAsync(int position)
        {
            if (position < 0 || position >= _entities.Count)
                throw new ArgumentOutOfRangeException(nameof(position));
            
            Dispose();

            // set current window position
            _position = position - _buffer.Length / 2;
            if (_position < 0)
            {
                _position = _entities.Count + _position;
            }

            // load images from the center of the window
            for (var i = 0; i < _buffer.Length; ++i)
            {
                var bufferIndex = _buffer.Length / 2 + (i % 2 == 0 ? -1 : 1) * MathUtils.RoundUpDiv(i, 2);
                var entityIndex = BufferIndexToEntityIndex(bufferIndex);
                _buffer[bufferIndex] = LoadImageAsync(_entities[entityIndex]);
            }

            return _buffer[_buffer.Length / 2];
        }

        public bool TryToMoveForward()
        {
            return TryShift(1);
        }

        public bool TryToMoveBackward()
        {
            return TryShift(-1);
        }

        private bool TryShift(int shift)
        {
            var current = _buffer[_buffer.Length / 2];
            if (current.Status != TaskStatus.RanToCompletion)
            {
                return false;
            }
            ShiftAsync(shift);
            return true;
        }

        private Task<Image> ShiftAsync(int shift)
        {
            if (shift != 1 && shift != -1)
                throw new ArgumentOutOfRangeException(nameof(shift));

            var firstIndex = shift > 0 ? 0 : _buffer.Length - 1;
            var lastIndex = _buffer.Length - 1 - firstIndex;

            // dispose the first image
            _buffer[firstIndex].ContinueWith(parent => parent.Result?.Dispose());

            // shift the window
            if (shift > 0)
            {
                for (var i = 0; i < _buffer.Length - 1; ++i)
                {
                    _buffer[i] = _buffer[i + 1];
                }
            }
            else
            {
                for (var i = _buffer.Length - 1; i >= 1; --i)
                {
                    _buffer[i] = _buffer[i - 1];
                }
            }

            _position += shift;
            if (_position < 0)
            {
                _position = _entities.Count + _position;
            }
            else if (_position >= _entities.Count)
            {
                _position %= _entities.Count;
            }

            // start loading the last image
            var entityIndex = BufferIndexToEntityIndex(lastIndex);
            _buffer[lastIndex] = LoadImageAsync(_entities[entityIndex]);

            return _buffer[_buffer.Length / 2];
        }

        private int BufferIndexToEntityIndex(int bufferIndex)
        {
            return (_position + bufferIndex) % _entities.Count;
        }

        private Task<Image> LoadImageAsync(IEntity entity)
        {
            return _imageLoader.LoadImageAsync(entity);
        }

        public void Dispose()
        {
            foreach (var item in _buffer)
            {
                item.ContinueWith(parent => parent.Result?.Dispose());
            }
        }
    }
}
