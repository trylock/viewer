using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Core.Collections
{
    /// <summary>
    /// Bitmap of arbitrary size.
    /// </summary>
    public class Bitmap : IReadOnlyList<bool>
    {
        private readonly List<ulong> _data;
        private const int BlockSize = 64;

        /// <summary>
        /// Number of bits in the bitmap
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Create a new reseted (each bit is 0) bitmap with <paramref name="size"/> bits.
        /// </summary>
        /// <param name="size">Number of bits in the bitmap</param>
        public Bitmap(int size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            Count = size;
            _data = new List<ulong>();
            var blockCount = Count.RoundUpDiv(BlockSize);
            for (var i = 0; i < blockCount; ++i)
            {
                _data.Add(0);
            }
        }

        public Bitmap(int size, bool state)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            var value = state ? ulong.MaxValue : 0;
            Count = size;
            _data = new List<ulong>();
            var blockCount = Count.RoundUpDiv(BlockSize);
            for (var i = 0; i < blockCount; ++i)
            {
                _data.Add(value);
            }
        }

        /// <summary>
        /// Copy <paramref name="other"/> bitmap.
        /// </summary>
        /// <param name="other">Bitmap to copy</param>
        public Bitmap(Bitmap other)
        {
            Count = other.Count;
            _data = new List<ulong>(other._data);
        }

        /// <summary>
        /// Set <paramref name="index"/>th bit in the bitmap
        /// </summary>
        /// <param name="index">Index of a bit to set (to 1)</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="index"/> is negative or greater than or equal to <see cref="Count"/>
        /// </exception>
        public void Set(int index)
        {
            if (index < 0 || index >= Count) 
                throw new ArgumentOutOfRangeException(nameof(index));

            SetUnchecked(index);
        }
        
        private void SetUnchecked(int index)
        {
            var blockIndex = index / BlockSize;
            var blockOffset = index % BlockSize;
            var mask = 1UL << blockOffset;
            _data[blockIndex] |= mask;
        }

        /// <summary>
        /// Compute the bitwise and operation on 2 bitmaps. This operation will modify this bitmap.
        /// <paramref name="bitmap"/> will remain unmodified.
        /// </summary>
        /// <param name="bitmap">Other bitmap</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <see cref="Count"/> are <paramref name="bitmap"/>.Count are different.
        /// </exception>
        public void And(Bitmap bitmap)
        {
            if (Count != bitmap.Count)
                throw new ArgumentOutOfRangeException(nameof(bitmap));

            for (var i = 0; i < _data.Count; ++i)
            {
                _data[i] &= bitmap._data[i];
            }
        }

        /// <summary>
        /// Compute the bitwise or operation on 2 bitmaps. This operation will modify this bitmap.
        /// <paramref name="bitmap"/> will remain unmodified.
        /// </summary>
        /// <param name="bitmap">Other bitmap</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <see cref="Count"/> are <paramref name="bitmap"/>.Count are different.
        /// </exception>
        public void Or(Bitmap bitmap)
        {
            if (Count != bitmap.Count)
                throw new ArgumentOutOfRangeException(nameof(bitmap));

            for (var i = 0; i < _data.Count; ++i)
            {
                _data[i] |= bitmap._data[i];
            }
        }

        /// <summary>
        /// Compute the bitwise not operation. The operation will modify this bitmap.
        /// </summary>
        public void Not()
        {
            for (var i = 0; i < _data.Count; ++i)
            {
                _data[i] = ~_data[i];
            }
        }

        /// <summary>
        /// Get all bits in the bitmap
        /// </summary>
        /// <returns></returns>
        public IEnumerator<bool> GetEnumerator()
        {
            for (var i = 0; i < _data.Count; ++i)
            {
                var block = _data[i];
                var blockSize = BlockSize;
                if (i + 1 >= _data.Count)
                {
                    blockSize = Count % BlockSize;
                }

                for (var j = 0; j < blockSize; ++j)
                {
                    var mask = 1ul << j;
                    yield return (block & mask) != 0;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get a single bit from the bitmap
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool this[int index]
        {
            get
            {
                var blockIndex = index / BlockSize;
                var blockOffset = index % BlockSize;
                var blockMask = 1ul << blockOffset;
                return (_data[blockIndex] & blockMask) != 0;
            }
        }
    }
}
