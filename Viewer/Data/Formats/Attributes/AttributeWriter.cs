using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
    public class AttributeWriter : IAttributeWriter
    {
        private class WriterVisitor : IAttributeVisitor, IDisposable
        {
            private BinaryWriter Writer { get; }

            public WriterVisitor(BinaryWriter writer)
            {
                Writer = writer ?? throw new ArgumentNullException(nameof(writer));
            }

            public void Visit(IntAttribute attr)
            {
                Writer.Write((short)AttributeType.Int);
                WriteString(attr.Name);
                Writer.Write((int)attr.Value);
            }

            public void Visit(DoubleAttribute attr)
            {
                Writer.Write((short)AttributeType.Double);
                WriteString(attr.Name);
                Writer.Write(attr.Value);
            }

            public void Visit(StringAttribute attr)
            {
                Writer.Write((short)AttributeType.String);
                WriteString(attr.Name);
                WriteString(attr.Value);
            }

            public void Visit(DateTimeAttribute attr)
            {
                Writer.Write((short)AttributeType.DateTime);
                WriteString(attr.Name);
                WriteString(attr.Value.ToString(DateTimeAttribute.Format));
            }

            public void Visit(ImageAttribute attr)
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
                Writer.Dispose();
            }

            private void WriteString(string value)
            {
                Writer.Write(Encoding.UTF8.GetBytes(value));
                Writer.Write((byte)0x00);
            }
        }

        private readonly WriterVisitor _writer;
        
        public AttributeWriter(BinaryWriter writer)
        {
            _writer = new WriterVisitor(writer);
        }

        public void Write(Attribute attr)
        {
            if (attr.Flags.HasFlag(AttributeFlags.ReadOnly))
            {
                return;
            }

            attr.Accept(_writer);
        }
        
        public void Dispose()
        {
            _writer.Dispose();
        }
    }

    [Export(typeof(IAttributeWriterFactory))]
    public class AttributeWriterFactory : IAttributeWriterFactory
    {
        public IAttributeWriter Create(Stream output)
        {
            return new AttributeWriter(new BinaryWriter(output));
        }
    }
}
