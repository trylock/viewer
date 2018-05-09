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
        private class WriterVisitor : IValueVisitor, IDisposable
        {
            private BinaryWriter Writer { get; }

            public WriterVisitor(BinaryWriter writer)
            {
                Writer = writer ?? throw new ArgumentNullException(nameof(writer));
            }

            public void Write(Attribute attr)
            {
                Writer.Write((short)attr.Value.Type);
                WriteString(attr.Name);
                attr.Value.Accept(this);
            }

            void IValueVisitor.Visit(IntValue attr)
            {
                Writer.Write((int) attr.Value);
            }

            void IValueVisitor.Visit(RealValue attr)
            {
                Writer.Write((double) attr.Value);
            }

            void IValueVisitor.Visit(StringValue attr)
            {
                WriteString(attr.Value);
            }

            void IValueVisitor.Visit(DateTimeValue attr)
            {
                WriteString(attr.Value?.ToString(DateTimeValue.Format));
            }

            void IValueVisitor.Visit(ImageValue attr)
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
            
            _writer.Write(attr);
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
