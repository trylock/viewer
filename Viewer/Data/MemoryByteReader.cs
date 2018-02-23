using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public sealed class MemoryByteReader : ByteReader
    {
        private byte[] _data;
        private int _index;
        private int _last;

        public override long Position => _index;
        public override bool IsEnd => _index >= _last;

        /// <summary>
        /// Create byte reader that reads bytes from memory (byte array)
        /// </summary>
        /// <param name="buffer">Byte array from which we read</param>
        /// <param name="offset">Offset in the buffer where we should start reading</param>
        /// <param name="length">Number of bytes to read from buffer (starting at offset)</param>
        public MemoryByteReader(byte[] buffer, int offset, int length)
        {
            _data = buffer;
            _index = offset;
            _last = offset + length;
        }

        public MemoryByteReader(byte[] buffer) : this(buffer, 0, buffer.Length)
        {
        }

        public override byte ReadByte()
        {
            if (_index >= _last)
            {
                throw new EndOfStreamException();
            }

            return _data[_index++];
        }

        public override void Dispose()
        {
        }
    }
}
