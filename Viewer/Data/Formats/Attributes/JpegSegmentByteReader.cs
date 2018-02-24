using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;

namespace Viewer.Data.Formats.Attributes
{
    /// <summary>
    /// JPEG segment byte reader reads bytes from multiple JPEG segments.
    /// It skips the segment header at the start of every segment.
    /// It is basically an adapter for a byte reader for reading from multiple byte arrays.
    /// </summary>
    public sealed class JpegSegmentByteReader : ByteReader
    {
        private readonly IList<JpegSegment> _segments;
        private int _segmentIndex;
        private int _segmentOffset;
        private int _segmentHeaderLength;

        private JpegSegment CurrentSegment => _segments[_segmentIndex];
        
        public override long Position
        {
            get
            {
                if (_segmentIndex < _segments.Count)
                {
                    return CurrentSegment.Offset + _segmentOffset;
                }

                if (_segments.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return _segments[_segments.Count - 1].Offset + _segments[_segments.Count - 1].Bytes.Length;
                }

            }
        }
        
        public override bool IsEnd => _segmentIndex >= _segments.Count;

        public JpegSegmentByteReader(IList<JpegSegment> segments, int segmentHeaderLength)
        {
            _segments = segments;
            _segmentHeaderLength = segmentHeaderLength;
            _segmentOffset = _segmentHeaderLength;
        }

        /// <summary>
        /// Read next byte from current segment.
        /// It will skip the segment header.
        /// If the end of a segment is reached, it will move to the next segment.
        /// </summary>
        /// <exception cref="EndOfStreamException">
        ///     If we reached the end of the last segment
        /// </exception>
        /// <returns>Next byte in the segment</returns>
        public override byte ReadByte()
        {
            FixState();

            if (IsEnd)
            {
                throw new EndOfStreamException();
            }

            var value = CurrentSegment.Bytes[_segmentOffset++];
            FixState();
            return value;
        }

        public override void Dispose()
        {
        }

        private void FixState()
        {
            if (_segmentIndex >= _segments.Count)
            {
                return; // end state is a valid sate
            }

            if (_segmentOffset < _segments[_segmentIndex].Bytes.Length)
            {
                return; // we are in a segment
            }

            // move to the next segment and start reading after the segment header
            _segmentIndex++;
            _segmentOffset = _segmentHeaderLength;

            // check if the segment is valid
            if (_segmentIndex < _segments.Count &&
                _segmentOffset >= _segments[_segmentIndex].Bytes.Length)
            {
                // invalid segment: it has to contain header
                throw new InvalidDataFormatException(
                    Position,
                    "Invalid segment data. Segment does not contain header.");
            }
        }
    }
}
