using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;

namespace Viewer.Data.Formats.Jpeg
{
    public static class JpegSegmentUtils
    {
        /// <summary>
        /// Maximal number of bytes in a JPEG segment 
        /// (not including the 2 size bytes and header)
        /// </summary>
        public const long MaxSegmentSize = 0x10000 - 2;

        /// <summary>
        /// Copy segment data from multiple segments without header to a single array.
        /// </summary>
        /// <param name="segments">All segments</param>
        /// <param name="type">Type of segments to copy</param>
        /// <param name="header">Header of the segments to copy (first bytes, ASCII characters only)</param>
        /// <returns></returns>
        public static byte[] JoinSegmentData(
            IEnumerable<JpegSegment> segments,
            JpegSegmentType type,
            string header)
        {
            // compute the exact size
            long size = 0;
            foreach (var segment in segments)
            {
                if (MatchSegment(segment, type, header))
                {
                    size += segment.Bytes.Length - header.Length;
                }
            }

            // copy segments
            var buffer = new byte[size];
            long bufferOffset = 0;
            foreach (var segment in segments)
            {
                if (MatchSegment(segment, type, header))
                {
                    var bytesToCopyCount = segment.Bytes.Length - header.Length;
                    Array.Copy(
                        segment.Bytes,
                        header.Length,
                        buffer,
                        bufferOffset,
                        bytesToCopyCount);
                    bufferOffset += bytesToCopyCount;
                }
            }

            return buffer;
        }
        
        /// <summary>
        /// Split data array to segments of length at most <paramref name="maxSegmentSize"/> bytes
        /// </summary>
        /// <param name="data">Data to split</param>
        /// <param name="type">Type of each JPEG segment</param>
        /// <param name="header">
        /// Header of each JPEG segment. All characters have to be ASCII.
        /// </param>
        /// <param name="maxSegmentSize">
        /// Maximal size of the JPEG segment (excluding the 2 size bytes)
        /// </param>
        /// <returns>JPEG segments enumerator</returns>
        public static IEnumerable<JpegSegment> SplitSegmentData(
            byte[] data,
            JpegSegmentType type,
            string header,
            long maxSegmentSize = MaxSegmentSize)
        {
            if (header.Length >= maxSegmentSize)
            {
                throw new ArgumentException(
                    "Header must fit in the JPEG segment with some data.");
            }

            long dataOffset = 0;
            while (dataOffset < data.Length)
            {
                long bufferSize = Math.Min(
                    data.Length - dataOffset,
                    maxSegmentSize - header.Length);
                var buffer = new byte[bufferSize + header.Length];

                // copy header 
                for (int i = 0; i < header.Length; ++i)
                {
                    buffer[i] = (byte) header[i];
                }

                // copy part of the data
                Array.Copy(data, dataOffset, buffer, header.Length, bufferSize);
                dataOffset += bufferSize;
                yield return new JpegSegment(type, buffer, 0);
            }
        }

        /// <summary>
        /// Check segment type and header.
        /// </summary>
        /// <param name="segment">JPEG segment</param>
        /// <param name="type">Expected type</param>
        /// <param name="header">Expected header</param>
        /// <returns>true iff the segment has given type and header</returns>
        public static bool MatchSegment(JpegSegment segment, JpegSegmentType type, string header)
        {
            if (segment.Type != type || segment.Bytes.Length < header.Length)
            {
                return false;
            }

            for (int i = 0; i < header.Length; ++i)
            {
                if ((char) segment.Bytes[i] != header[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
