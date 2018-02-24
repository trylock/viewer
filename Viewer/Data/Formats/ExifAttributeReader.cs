using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using Viewer.Data.Formats.Attributes;

namespace Viewer.Data.Formats
{
    public class ExifAttributeReader : IAttributeReader
    {
        private struct ExifTag
        {
            public string Name;
            public int Tag;
            public AttributeType Type;

            public ExifTag(string name, int tag, AttributeType type)
            {
                Name = name;
                Tag = tag;
                Type = type;
            }
        }

        private IReadOnlyList<Directory> _exif;

        private bool _thumbnailRead = false;

        private List<ExifTag> _tags = new List<ExifTag>
        {
            new ExifTag("ImageWidth", ExifDirectoryBase.TagImageWidth, AttributeType.Int),
            new ExifTag("ImageHeight", ExifDirectoryBase.TagImageHeight, AttributeType.Int),
            new ExifTag("Make", ExifDirectoryBase.TagMake, AttributeType.String),
            new ExifTag("Model", ExifDirectoryBase.TagModel, AttributeType.String),
        };

        private int _index;

        public ExifAttributeReader(IReadOnlyList<Directory> exif)
        {
            _exif = exif;
        }

        public Attribute ReadNext()
        {
            var directory = _exif.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (directory == null)
            {
                return null; // no exif metadata
            }

            for (;;)
            {
                if (_index >= _tags.Count)
                {
                    return null;
                }

                var tag = _tags[_index++];
                if (!directory.ContainsTag(tag.Tag))
                {
                    continue;
                }

                switch (tag.Type)
                {
                    case AttributeType.Int:
                        return new IntAttribute(tag.Name, AttributeSource.Exif, directory.GetInt32(tag.Tag));
                    case AttributeType.Double:
                        return new DoubleAttribute(tag.Name, AttributeSource.Exif, directory.GetDouble(tag.Tag));
                    case AttributeType.String:
                        return new StringAttribute(tag.Name, AttributeSource.Exif, directory.GetString(tag.Tag));
                    case AttributeType.DateTime:
                        return new DateTimeAttribute(tag.Name, AttributeSource.Exif, directory.GetDateTime(tag.Tag));
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void Dispose()
        {
        }
    }

    public class ExifAttributeReaderFactory : IAttributeReaderFactory
    {
        public IAttributeReader CreateFromSegments(IList<JpegSegment> segments)
        {
            var exifReader = new ExifReader();
            var dirs = exifReader.ReadJpegSegments(segments);
            return new ExifAttributeReader(dirs);
        }
    }
}
