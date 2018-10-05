using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Viewer.Core;

namespace Viewer.Data.Formats.Jpeg
{
    public interface IJpegSegmentWriter : IDisposable
    {
        /// <summary>
        /// Write single JPEG segment at the current position
        /// </summary>
        /// <param name="segment">Segment to write</param>
        /// <exception cref="ArgumentNullException"><paramref name="segment"/> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="segment"/> data array has an invalid size
        /// </exception>
        void WriteSegment(JpegSegment segment);

        /// <summary>
        /// Finish writing JPEG segment.
        /// Copy unchanged image data starting at current position in dataStream 
        /// including the End of Image segment.
        /// segment after them.
        /// <param name="input">Stream with image data</param>
        /// </summary>
        void Finish(Stream input);
    }

    public class JpegSegmentWriter : IJpegSegmentWriter
    {
        private readonly BinaryWriter _writer;
        private readonly int _blockSize;
        
        public JpegSegmentWriter(BinaryWriter writer, int blockSize = 4096)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _blockSize = blockSize;
        }

        public void WriteSegment(JpegSegment segment)
        {
            if (segment == null)
                throw new ArgumentNullException(nameof(segment));
            if (segment.Bytes.Length + 2 > 0xFFFF)
                throw new ArgumentOutOfRangeException(nameof(segment));

            _writer.Write((byte)0xFF); // segment start
            _writer.Write((byte)segment.Type);

            if (HasData(segment.Type))
            {
                // write size as big endian ushort
                var size = segment.Bytes.Length + 2; // + 2 for the size bytes
                _writer.Write((byte)(size >> 8));
                _writer.Write((byte)(size & 0xFF));

                // write data
                _writer.Write(segment.Bytes, 0, segment.Bytes.Length);
            }
        }

        public void Finish(Stream input)
        {
            // write Start of Scan segment
            _writer.Write((byte)0xFF);
            _writer.Write((byte)JpegSegmentType.Sos);

            // write image data 
            // this will also copy the End of Image segment (assuming correct file format)
            var buffer = new byte[_blockSize];
            for (;;)
            {
                var length = input.Read(buffer, 0, buffer.Length);
                if (length <= 0)
                    break;
                
                _writer.Write(buffer, 0, length);
            }
            
            // truncate the temporary file to the image size if necessary
            if (_writer.BaseStream.Position < _writer.BaseStream.Length)
            {
                _writer.BaseStream.SetLength(_writer.BaseStream.Position);
            }
            _writer.Flush();
        }
        
        public void Dispose()
        {
            _writer?.Dispose();
        }

        private bool HasData(JpegSegmentType type)
        {
            return (byte) type < 0xD0 || (byte) type > 0xDA;
        }
    }
}
