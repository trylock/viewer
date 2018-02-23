using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;

namespace Viewer.Data.Formats
{
    public interface IJpegSegmentReader : IDisposable
    {
        /// <summary>
        /// Read next JPEG segment in an input stream
        /// </summary>
        /// <exception cref="InvalidDataFormatException">
        ///     JPEG format of given data is invalid.
        /// </exception>
        /// <returns>Next JPEG segment or null if there is none</returns>
        JpegSegment ReadNext();
    }

    public class JpegSegmentReader : IJpegSegmentReader, IEnumerable<JpegSegment>
    {
        private readonly BinaryReader _reader;
        private bool _isEnd = false;
        private bool _isStart = true;
        private readonly long _offset;

        /// <summary>
        /// Number of bytes from the start of the image.
        /// Note: this is not necessarily the same as position in stream as 
        ///       the image could be stored anywhere in the stream. 
        /// </summary>
        private long PositionInImage => _reader.BaseStream.Position - _offset;

        public JpegSegmentReader(BinaryReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _isEnd = _reader.BaseStream.Position >= _reader.BaseStream.Length;
            _offset = _reader.BaseStream.Position;
        }

        /// <summary>
        /// Read next JPEG segment in an input stream.
        /// It won't read data past the Start of Scan segment header.
        /// The Start of Scan segment won't have any data.
        /// </summary>
        /// <returns>Next JPEG segment or null if there is none</returns>
        public JpegSegment ReadNext()
        {
            if (_isEnd)
            {
                return null;
            }

            try
            {
                // read segment header
                var header = _reader.ReadByte();
                if (header != 0xFF)
                {
                    throw new InvalidDataFormatException(
                        PositionInImage - 1,
                        $"Invalid JPEG segment header. Expecting 0xFF, got 0x{header:X}");
                }

                var type = (JpegSegmentType) _reader.ReadByte();
                if (_isStart)
                {
                    if (type != JpegSegmentType.Soi)
                    {
                        throw new InvalidDataFormatException(
                            PositionInImage - 1,
                            "Invalid JPEG file. Expecting 0xFFD8 (Start of Image) header" +
                            $", got 0xFF{(byte) type:X}");
                    }

                    _isStart = false;
                }

                if (type == JpegSegmentType.Sos || type == JpegSegmentType.Eoi)
                {
                    Debug.Assert(type != JpegSegmentType.Eoi);
                    _isEnd = true;
                    return new JpegSegment(type, new byte[0], _reader.BaseStream.Position);
                }

                // some segments don't have data
                if ((int) type >= 0xD0 && (int) type <= 0xDA)
                {
                    return new JpegSegment(type, new byte[0], _reader.BaseStream.Position);
                }

                // read segment size 
                int size = _reader.ReadByte();
                size <<= 8;
                size |= _reader.ReadByte();
                if (size < 2)
                {
                    throw new InvalidDataFormatException(
                        PositionInImage - 2, // position before we read the size
                        $"Invalid size. It has to be at least 2 bytes, actual value: {size}");
                }

                size -= 2; // the size includes the 2 size bytes

                // read segment data
                var dataOffset = _reader.BaseStream.Position;
                var data = _reader.ReadBytes(size);
                if (data.Length != size)
                {
                    throw new InvalidDataFormatException(
                        PositionInImage - data.Length,
                        $"Invalid segment data size. Expected {size}, got {data.Length}.");
                }

                return new JpegSegment(type, data, dataOffset);
            }
            catch (EndOfStreamException e)
            {
                throw new InvalidDataFormatException(
                    PositionInImage, 
                    "Unexpected end of input", e);
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public IEnumerator<JpegSegment> GetEnumerator()
        {
            for (;;)
            {
                var segment = ReadNext();
                if (segment == null)
                    break;
                yield return segment;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
