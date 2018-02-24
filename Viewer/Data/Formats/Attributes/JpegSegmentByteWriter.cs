using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Viewer.Data;

namespace Viewer.Data.Formats.Attributes
{
    /// <summary>
    /// This class writes data to JPEG segments.
    /// It writes given header to the start of every segment.
    /// If the maximum size of a segment is reached, a new segment is created.
    /// </summary>
    public sealed class JpegSegmentByteWriter : ByteWriter
    {
        private List<byte> _buffer = new List<byte>();

        private List<JpegSegment> _segments = new List<JpegSegment>();

        private readonly byte[] _header;

        private readonly int _maxSegmentSize;

        public JpegSegmentByteWriter(byte[] header, int maxSegmentSize = 0x10000)
        {
            _header = header;
            _maxSegmentSize = maxSegmentSize;
            _buffer.AddRange(_header);
        }

        public override void WriteByte(byte value)
        {
            _buffer.Add(value);
            if (_buffer.Count >= _maxSegmentSize)
            {
                FinishSegment();
            }
        }

        /// <summary>
        /// Get all JPEG segments 
        /// </summary>
        /// <returns>List of JPEG segments</returns>
        public List<JpegSegment> ToSegments()
        {
            // create segment from current buffer
            if (_buffer.Count > _header.Length)
            {
                FinishSegment();
            }

            // return all the segmnets
            var segments = _segments;
            _segments = null;
            return segments;
        }

        private void FinishSegment()
        {
            // add new segment
            var segment = new JpegSegment(JpegSegmentType.App1, _buffer.ToArray(), 0);
            _segments.Add(segment);

            // create a new buffer with just the header
            _buffer = new List<byte>();
            _buffer.AddRange(_header);
        }
    }
}
