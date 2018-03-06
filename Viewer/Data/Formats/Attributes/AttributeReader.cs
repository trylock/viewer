using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Jpeg;

namespace Viewer.Data.Formats.Attributes
{
    /// <summary>
    /// Type numbers as in the binary format of attributes
    /// </summary>
    public enum AttributeType
    {
        Int = 1,
        Double = 2,
        String = 3,
        DateTime = 4,
        Image = 5
    }

    /// <summary>
    /// Read attributes in a binary format. 
    /// </summary>
    /// 
    /// <remarks>
    /// Types in the format:
    /// - uint16: unsigned 2 byte integer, little endian
    /// - int32: signed 4 byte integer, two's complement, little endian
    /// - Double: 8 bytes, IEEE 745, (binary64)
    /// - String: UTF8 string with 0 byte at the end
    /// - DateTime: String in the W3C DTF format: "YYYY-MM-DDThh:mm:ss.sTZD"
    /// 
    /// Attribute format:
    /// - type (uint16) <see cref="AttributeType"/>
    /// - name (String)
    /// - Value (int32, String, Double or DateTime - depends on the type value)
    /// </remarks>
    public class AttributeReader : IAttributeReader
    {
        /// <summary>
        /// Name of a JPEG segment with attribute data.
        /// It will be at the start of every attribute segment as an ASCII string.
        /// </summary>
        public const string JpegSegmentHeader = "Attr\0";
       
        private readonly IByteReader _reader;

        public AttributeReader(IByteReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <summary>
        /// Read next attribute from the input stream.
        /// </summary>
        /// <exception cref="InvalidDataFormatException">
        ///     Data format is invalid (invalid type, unexpected end of input)
        /// </exception>
        /// <returns>
        ///     Next attriubte from the input or null if there is none
        /// </returns>
        public Attribute Read()
        {
            if (_reader.IsEnd)
            {
                return null;
            }

            try
            {
                // read type and name
                var typeOffset = _reader.Position;
                var type = _reader.ReadInt16();
                var name = ReadStringUTF8();

                // read value
                switch ((AttributeType) type)
                {
                    case AttributeType.Int:
                        var valueInt = _reader.ReadInt32();
                        return new IntAttribute(name, AttributeSource.Custom, valueInt);
                    case AttributeType.Double:
                        var valueDouble = _reader.ReadDouble();
                        return new DoubleAttribute(name, AttributeSource.Custom, valueDouble);
                    case AttributeType.String:
                        var valueString = ReadStringUTF8();
                        return new StringAttribute(name, AttributeSource.Custom, valueString);
                    case AttributeType.DateTime:
                        var valueRaw = ReadStringUTF8();
                        var valueDate = DateTime.ParseExact(valueRaw, DateTimeAttribute.Format, CultureInfo.InvariantCulture);
                        return new DateTimeAttribute(name, AttributeSource.Custom, valueDate);
                    default:
                        throw new InvalidDataFormatException(typeOffset, $"Invalid type: 0x{type:X}");
                }
            }
            catch (EndOfStreamException e)
            {
                throw new InvalidDataFormatException(_reader.Position, "Unexpected end of input", e);
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
        
        private string ReadStringUTF8()
        {
            var buffer = new List<byte>();
            for (;;)
            {
                var value = _reader.ReadByte();
                if (value == 0)
                    break;
                buffer.Add(value);
            }

            return Encoding.UTF8.GetString(buffer.ToArray());
        }
    }

    /// <summary>
    /// Create attribute reader of the custom attribute segments.
    /// </summary>
    public class AttributeReaderFactory : IAttributeReaderFactory
    {
        /// <summary>
        /// Extract data from attributes segment and copy them to 1 continuous block of memory.
        /// </summary>
        /// <param name="segments">JPEG segments</param>
        /// <returns>Serialized attributes data in one array</returns>
        public static byte[] ExtractAttributeData(IEnumerable<JpegSegment> segments)
        {
            // compute the exact size
            long size = 0;
            foreach (var segment in segments)
            {
                if (IsAttributeSegment(segment))
                {
                    size += segment.Bytes.Length - AttributeReader.JpegSegmentHeader.Length;
                }
            }

            // copy segments
            var buffer = new byte[size];
            long bufferOffset = 0;
            foreach (var segment in segments)
            {
                if (IsAttributeSegment(segment))
                {
                    var bytesToCopyCount = segment.Bytes.Length - AttributeReader.JpegSegmentHeader.Length;
                    Array.Copy(
                        segment.Bytes, 
                        AttributeReader.JpegSegmentHeader.Length, 
                        buffer, 
                        bufferOffset,
                        bytesToCopyCount);
                }
            }

            return buffer;
        }

        public static bool IsAttributeSegment(JpegSegment segment)
        {
            if (segment.Type != JpegSegmentType.App1)
            {
                return false;
            }

            var segmentHeader = Encoding.UTF8.GetString(segment.Bytes, 0, AttributeReader.JpegSegmentHeader.Length);
            return segmentHeader == AttributeReader.JpegSegmentHeader;
        }

        /// <summary>
        /// Create attribute reader from list of 
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        public IAttributeReader CreateFromSegments(IEnumerable<JpegSegment> segments)
        {
            var data = ExtractAttributeData(segments);
            return new AttributeReader(new MemoryByteReader(data));
        }
    }
}
