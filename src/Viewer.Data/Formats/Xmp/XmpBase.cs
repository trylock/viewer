using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Viewer.Data.Formats.Jpeg;
using XmpCore;
using XmpCore.Impl;
using XmpCore.Options;
using XmpUtils = XmpCore.XmpUtils;

namespace Viewer.Data.Formats.Xmp
{
    public abstract class XmpBase
    {
        /// <summary>
        /// XMP data
        /// </summary>
        protected IXmpMeta Data;

        /// <summary>
        /// User attribute prefix
        /// </summary>
        protected string Prefix;

        /// <summary>
        /// XML namespace used to identify user attributes
        /// </summary>
        public const string Namespace = "https://trylock.github.io/viewer/attributes/";

        /// <summary>
        /// Header of the standard segment
        /// </summary>
        public const string StandardSegmentHeader = "http://ns.adobe.com/xap/1.0/\0";

        /// <summary>
        /// Header of the extended JPEG segment
        /// </summary>
        public const string ExtendedSegmentHeader = "http://ns.adobe.com/xmp/extension/";

        protected XmpBase(IXmpMeta data)
        {
            Data = data;
            Prefix = XmpMetaFactory.SchemaRegistry.RegisterNamespace(Namespace, "viewer");
        }

        protected string GetAttributePropertyPath(int attributeIndex, string propertyName)
        {
            return $"{Prefix}attributes[{attributeIndex}]/{Prefix}{propertyName}";
        }

        /// <summary>
        /// Build attribute property path
        /// </summary>
        /// <param name="attributeIndex">
        /// Index of an attribute (starting at 1 according to XMP spec.)
        /// </param>
        /// <param name="propertyName">Name of the property (name, type or value)</param>
        /// <returns>Path to attribute property</returns>
        protected IXmpProperty GetAttributeProperty(int attributeIndex, string propertyName)
        {
            var path = GetAttributePropertyPath(attributeIndex, propertyName);
            return Data.GetProperty(Namespace, path);
        }

        private struct ExtendedSegmentData
        {
            /// <summary>
            /// GUID of all extended XMP segments
            /// </summary>
            public string Guid { get; set; }

            /// <summary>
            /// Length of the whole extended XMP segment
            /// </summary>
            public int Length { get; set; }

            /// <summary>
            /// Offset of this portion of the extended XMP data
            /// </summary>
            public int Offset { get; set; }
        }

        /// <summary>
        /// Parse extended XMP segment from JPEG segment
        /// </summary>
        /// <param name="segment"></param>
        /// <returns>
        /// Metainformation about the extended XMP segment portion in <paramref name="segment"/>
        /// or null if there is not an extended XMP data in <paramref name="segment"/>
        /// </returns>
        private static ExtendedSegmentData? ParseExtendedSegment(JpegSegment segment)
        {
            if (!segment.MatchSegment(JpegSegmentType.App1, ExtendedSegmentHeader))
            {
                return null; // only parse APP1 segments with extended XMP data
            }

            const int guidPosition = 35;
            const int lengthPosition = guidPosition + 32;
            const int offsetPosition = lengthPosition + 4;
            const int dataPosition = offsetPosition + 4;

            if (segment.Bytes.Length < dataPosition)
            {
                return null; // invalid XMP segment
            }

            var data = new ExtendedSegmentData
            {
                Guid = Encoding.ASCII.GetString(segment.Bytes, guidPosition, 32),
                Length = BitConverter.ToInt32(segment.Bytes, lengthPosition),
                Offset = BitConverter.ToInt32(segment.Bytes, offsetPosition),
            };
            
            return data;
        }

        /// <summary>
        /// Join all extended XMP segments to a single buffer.
        /// </summary>
        /// <param name="segments">All JPEG segments</param>
        /// <param name="guid">GUID of the extended segments which should be joined</param>
        /// <returns>
        /// Valid XMP data which contain all portions of extended XMP segments or null if there
        /// is no extended segment.
        /// </returns>
        private static byte[] JoinExtendedSegments(IEnumerable<JpegSegment> segments, string guid)
        {
            byte[] buffer = null;

            // position of the segment data in an extended XMP segment
            const int extendedSegmentDataOffset = 35 + 32 + 4 + 4;

            // copy all portions of extended XMP data to a single buffer
            foreach (var segment in segments)
            {
                var portion = ParseExtendedSegment(segment);
                if (portion == null)
                {
                    continue;
                }

                // only parse extended segments which belong to the standard segment
                if (portion.Value.Guid != guid)
                {
                    continue;
                }

                if (buffer == null)
                {
                    buffer = new byte[portion.Value.Length];
                }

                // the buffer should always be large enough according to specification
                // this just makes the implementation more robust
                var dataLength = segment.Bytes.Length - extendedSegmentDataOffset;
                if (portion.Value.Offset + dataLength > buffer.Length)
                {
                    Array.Resize(ref buffer, portion.Value.Offset + dataLength);
                }

                // copy this portion of the extended XMP data to the buffer
                Array.Copy(
                    segment.Bytes, extendedSegmentDataOffset,
                    buffer, portion.Value.Offset, dataLength);
            }

            return buffer;
        }

        /// <summary>
        /// Join all XMP segments in <paramref name="segments"/> and parse the XMP tree.
        /// </summary>
        /// <param name="segments">JPEG segments in a file</param>
        /// <returns>Parsed XMP metadata or null</returns>
        public static IXmpMeta ParseSegments(IEnumerable<JpegSegment> segments)
        {
            if (segments == null)
                throw new ArgumentNullException(nameof(segments));

            XmpMetaFactory.SchemaRegistry.RegisterNamespace(Namespace, "viewer");

            // find standard XMP segment
            var standardXmpSegment = segments
                .FirstOrDefault(segment => 
                    segment.MatchSegment(JpegSegmentType.App1, StandardSegmentHeader));

            if (standardXmpSegment == null)
            {
                return null; // there is no XMP metadata
            }

            // parse standard XMP segment
            var standardXmp = XmpMetaFactory.ParseFromBuffer(
                standardXmpSegment.Bytes, 
                StandardSegmentHeader.Length, 
                standardXmpSegment.Bytes.Length - StandardSegmentHeader.Length);

            // parse extended XMP segments
            var extendedXmpGuid = standardXmp.GetProperty(
                "http://ns.adobe.com/xmp/note/", 
                "HasExtendedXMP")?.Value;
            var extendedXmpData = JoinExtendedSegments(segments, extendedXmpGuid);
            if (extendedXmpData != null)
            {
                var extendedXmp = XmpMetaFactory.ParseFromBuffer(extendedXmpData);
                XmpUtils.MergeFromJPEG(standardXmp, extendedXmp);
            }

            return standardXmp;
        }
        
        /// <summary>
        /// Serialize <paramref name="data"/> to JPEG segments
        /// </summary>
        /// <param name="data">Data to serialize</param>
        /// <returns>Serialized data</returns>
        public static IEnumerable<JpegSegment> SerializeSegments(IXmpMeta data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var standardXmp = new StringBuilder();
            var extendedXmp = new StringBuilder();
            var digest = new StringBuilder();
            XmpUtils.PackageForJPEG(data, standardXmp, extendedXmp, digest);
            
            // return standard XMP segment
            var standardData = Encoding.UTF8.GetBytes(
                StandardSegmentHeader + standardXmp.ToString());
            yield return new JpegSegment(JpegSegmentType.App1, standardData, 0);

            // split extended data to segments
            const int maxDataSize = 65400;
            int offset = 0;

            var extendedData = Encoding.UTF8.GetBytes(extendedXmp.ToString());
            while (offset < extendedXmp.Length)
            {
                int length = Math.Min(maxDataSize, extendedXmp.Length - offset);
                
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    writer.Write(ExtendedSegmentHeader);
                    writer.Write(digest.ToString());
                    writer.Write((uint) extendedData.Length);
                    writer.Write((uint) offset);
                    
                    yield return new JpegSegment(JpegSegmentType.App1, stream.GetBuffer(), 0);
                }
                
                offset += length;
            }
        }
    }
}
