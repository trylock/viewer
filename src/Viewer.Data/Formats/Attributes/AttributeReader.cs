using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Xmp;
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
    public class AttributeReader : IDisposable, IEnumerable<Attribute>
    {
        /// <summary>
        /// Name of a JPEG segment with attribute data.
        /// It will be at the start of every attribute segment as an ASCII string.
        /// </summary>
        public const string JpegSegmentHeader = "Attr\0";
       
        private readonly BinaryReader _reader;

        public AttributeReader(BinaryReader reader)
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
            if (_reader.BaseStream.Position >= _reader.BaseStream.Length)
            {
                return null;
            }

            try
            {
                // read type and name
                var typeOffset = _reader.BaseStream.Position;
                var type = _reader.ReadInt16();
                var name = ReadStringUTF8();

                // read value
                switch ((AttributeType) type)
                {
                    case AttributeType.Int:
                        var valueInt = _reader.ReadInt32();
                        return new Attribute(name, new IntValue(valueInt), AttributeSource.Custom);
                    case AttributeType.Double:
                        var valueDouble = _reader.ReadDouble();
                        return new Attribute(name, new RealValue(valueDouble), AttributeSource.Custom);
                    case AttributeType.String:
                        var valueString = ReadStringUTF8();
                        return new Attribute(name, new StringValue(valueString), AttributeSource.Custom);
                    case AttributeType.DateTime:
                        var valueRaw = ReadStringUTF8();
                        var valueDate = DateTime.ParseExact(valueRaw, DateTimeValue.Format, CultureInfo.InvariantCulture);
                        return new Attribute(name, new DateTimeValue(valueDate), AttributeSource.Custom);
                    default:
                        throw new InvalidDataFormatException(typeOffset, $"Invalid type: 0x{type:X}");
                }
            }
            catch (EndOfStreamException e)
            {
                throw new InvalidDataFormatException(
                    _reader.BaseStream.Position, 
                    "Unexpected end of input", 
                    e);
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

        public IEnumerator<Attribute> GetEnumerator()
        {
            for (;;)
            {
                var attribute = Read();
                if (attribute == null)
                {
                    break;
                }

                yield return attribute;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
