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
        private Attribute[] _attributes;
        private int _index;

        public FileAttributeReader(FileInfo fileInfo)
        {
            _attributes = new Attribute[]
            {
                new StringAttribute("FileName", fileInfo.Name, AttributeFlags.ReadOnly),
                new IntAttribute("FileSize", (int)fileInfo.Length, AttributeFlags.ReadOnly),
                new StringAttribute("Directory", fileInfo.Directory.Name, AttributeFlags.ReadOnly),
                new DateTimeAttribute("LastAccessTime", fileInfo.LastAccessTime, AttributeFlags.ReadOnly),
                new DateTimeAttribute("LastWriteTime", fileInfo.LastWriteTime, AttributeFlags.ReadOnly),
                new DateTimeAttribute("CreationTime", fileInfo.CreationTime, AttributeFlags.ReadOnly),
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
