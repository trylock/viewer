using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Viewer.IO;

namespace Viewer.Data.Formats
{
    [Export]
    [Export(typeof(IAttributeSerializer))]
    public class FileMetadataAttributeSerializer : IAttributeSerializer
    {
        // Names of exported attributes
        public const string FileName = "FileName";
        public const string FileSize = "FileSize";
        public const string Directory = "Directory";
        public const string LastAccessTime = "LastAccessTime";
        public const string LastWriteTime = "LastWriteTime";
        public const string CreationTime = "CreationTime";

        public IEnumerable<string> MetadataAttributes
        {
            get
            {
                yield return FileName;
                yield return FileSize;
                yield return Directory;
                yield return LastAccessTime;
                yield return LastWriteTime;
                yield return CreationTime;
            }
        }
        
        public bool CanRead(FileInfo file)
        {
            return true;
        }

        public bool CanWrite(FileInfo file)
        {
            return false;
        }

        public IEnumerable<Attribute> Deserialize(FileInfo file, Stream input)
        {
            var directoryName = file.Directory?.Name ?? "";
            directoryName = directoryName.Trim(PathUtils.PathSeparators);
            var attributes = new[]
            {
                new Attribute(FileName, new StringValue(file.Name), AttributeSource.Metadata),
                new Attribute(FileSize, new IntValue((int)file.Length), AttributeSource.Metadata),
                new Attribute(Directory, new StringValue(directoryName), AttributeSource.Metadata),
                new Attribute(LastAccessTime, new DateTimeValue(file.LastAccessTime), AttributeSource.Metadata),
                new Attribute(LastWriteTime, new DateTimeValue(file.LastWriteTime), AttributeSource.Metadata),
                new Attribute(CreationTime, new DateTimeValue(file.CreationTime), AttributeSource.Metadata),
            };
            return attributes;
        }

        public void Serialize(FileInfo file, Stream input, Stream output, IEnumerable<Attribute> attributes)
        {
            // filesystem metadata attributes are read-only
        }
    }
}
