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
        /// Copy segment data from multiple segments without header to a single array.
        /// </summary>
        /// <param name="segments">All segments</param>
        /// <param name="type">Type of segments to copy</param>
        /// <param name="header">Header of the segments to copy (first bytes, ASCII characters only)</param>
        /// <returns></returns>
        public static byte[] CopySegmentData(
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
