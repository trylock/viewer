using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Viewer.Data.Formats.Attributes;
using Viewer.Data;

namespace Viewer.Data.Formats.Attributes
{
    /// <summary>
    /// Write attributes to a binary format.
    /// For more info on the format, see the <see cref="AttributeReader"/> class.
    /// </summary>
    public class AttributeWriter : IAttributeWriter, IDisposable
    {
        private class WriterVisitor : IAttributeVisitor, IDisposable
        {
            public JpegSegmentByteWriter Writer { get; }

            public WriterVisitor(JpegSegmentByteWriter writer)
            {
                Writer = writer ?? throw new ArgumentNullException(nameof(writer));
            }

            public void Visit(IntAttribute attr)
            {
                Writer.WriteInt16((short)AttributeType.Int);
                WriteString(attr.Name);
                Writer.WriteInt32(attr.Value);
            }

            public void Visit(DoubleAttribute attr)
            {
                Writer.WriteInt16((short)AttributeType.Double);
                WriteString(attr.Name);
                Writer.WriteDouble(attr.Value);
            }

            public void Visit(StringAttribute attr)
            {
                Writer.WriteInt16((short)AttributeType.String);
                WriteString(attr.Name);
                WriteString(attr.Value);
            }

            public void Visit(DateTimeAttribute attr)
            {
                Writer.WriteInt16((short)AttributeType.DateTime);
                WriteString(attr.Name);
                WriteString(attr.Value.ToString(DateTimeAttribute.Format));
            }

            public void Visit(ImageAttribute attr)
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }

            private void WriteString(string value)
            {
                Writer.WriteBytes(Encoding.UTF8.GetBytes(value));
                Writer.WriteByte(0);
            }
        }

        private readonly WriterVisitor _writer;
        
        public AttributeWriter(JpegSegmentByteWriter writer)
        {
            _writer = new WriterVisitor(writer);
        }

        public void Write(Attribute attr)
        {
            if (attr.Source != AttributeSource.Custom)
            {
                return;
            }

            attr.Accept(_writer);
        }

        public List<JpegSegment> Finish()
        {
            return _writer.Writer.ToSegments();
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }

    public class AttributeWriterFactory : IAttributeWriterFactory
    {
        public IAttributeWriter Create()
        {
            var header = Encoding.UTF8.GetBytes(AttributeReader.JpegSegmentHeader);
            return new AttributeWriter(new JpegSegmentByteWriter(header));
        }
    }
}
