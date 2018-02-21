using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Formats
{
    /// <summary>
    /// Write attributes in a binary format.
    /// For more info on the format, see the <see cref="AttributeReader"/> class.
    /// </summary>
    public class AttributeWriter : IDisposable
    {
        private class WriterVisitor : IAttributeVisitor, IDisposable
        {
            private readonly BinaryWriter _writer;

            public WriterVisitor(BinaryWriter writer)
            {
                _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            }

            public void Visit(IntAttribute attr)
            {
                _writer.Write((ushort)AttributeType.Int);
                WriteString(attr.Name);
                _writer.Write(attr.Value);
            }

            public void Visit(DoubleAttribute attr)
            {
                _writer.Write((ushort)AttributeType.Double);
                WriteString(attr.Name);
                _writer.Write(attr.Value);
            }

            public void Visit(StringAttribute attr)
            {
                _writer.Write((ushort)AttributeType.String);
                WriteString(attr.Name);
                WriteString(attr.Value);
            }

            public void Visit(DateTimeAttribute attr)
            {
                _writer.Write((ushort)AttributeType.String);
                WriteString(attr.Name);
                WriteString(attr.Value.ToString(AttributeReader.DateTimeFormat));
            }

            public void Visit(ImageAttribute attr)
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
                _writer.Dispose();
            }

            private void WriteString(string value)
            {
                _writer.Write(Encoding.UTF8.GetBytes(value));
                _writer.Write((byte)0);
            }
        }

        private readonly WriterVisitor _writer;
        
        public AttributeWriter(BinaryWriter writer)
        {
            _writer = new WriterVisitor(writer);
        }

        public void Write(Attribute attr)
        {
            attr.Accept(_writer);
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }
}
