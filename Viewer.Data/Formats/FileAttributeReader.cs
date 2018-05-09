using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;

namespace Viewer.Data.Formats
{
    public class FileAttributeReader : IAttributeReader
    {
        private readonly Attribute[] _attributes;
        private int _index;

        public FileAttributeReader(FileInfo fileInfo)
        {
            _attributes = new Attribute[]
            {
                new Attribute("FileName", new StringValue(fileInfo.Name), AttributeFlags.ReadOnly),
                new Attribute("FileSize", new IntValue((int)fileInfo.Length), AttributeFlags.ReadOnly),
                new Attribute("Directory", new StringValue(fileInfo.Directory.Name), AttributeFlags.ReadOnly),
                new Attribute("LastAccessTime", new DateTimeValue(fileInfo.LastAccessTime), AttributeFlags.ReadOnly),
                new Attribute("LastWriteTime", new DateTimeValue(fileInfo.LastWriteTime), AttributeFlags.ReadOnly), 
                new Attribute("CreationTime", new DateTimeValue(fileInfo.CreationTime), AttributeFlags.ReadOnly), 
            };
        }

        public void Dispose()
        {
        }

        public Attribute Read()
        {
            return _index >= _attributes.Length ? null : _attributes[_index++];
        }
    }

    [Export(typeof(IAttributeReaderFactory))]
    public class FileAttributeReaderFactory : IAttributeReaderFactory
    {
        public IAttributeReader CreateFromSegments(FileInfo file, IEnumerable<JpegSegment> segments)
        {
            return new FileAttributeReader(file);
        }
    }
}
