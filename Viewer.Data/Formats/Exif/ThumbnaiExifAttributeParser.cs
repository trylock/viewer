using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace Viewer.Data.Formats.Exif
{
    public class ThumbnaiExifAttributeParser<T> : IExifAttributeParser where T : ExifDirectoryBase
    {
        private readonly string _attributeName;

        /// <summary>
        /// Create thumbnail parser 
        /// </summary>
        /// <param name="attributeName">Thumbnail attribute name</param>
        public ThumbnaiExifAttributeParser(string attributeName)
        {
            _attributeName = attributeName;
        }

        /// <summary>
        /// Try parsing thumbnail from exif
        /// </summary>
        /// <param name="exif">Exif metadata</param>
        /// <returns>Thumbnail attribute</returns>
        public Attribute Parse(IExifMetadata exif)
        {
            var dir = exif.GetDirectoryOfType<T>();
            if (dir == null)
            {
                return null;
            }

            if (!dir.ContainsTag(ExifThumbnailDirectory.TagThumbnailOffset) ||
                !dir.ContainsTag(ExifThumbnailDirectory.TagThumbnailLength))
            {
                return null;
            }
            
            var offset = dir.GetInt32(ExifThumbnailDirectory.TagThumbnailOffset);
            var length = dir.GetInt32(ExifThumbnailDirectory.TagThumbnailLength);
            if (length <= 0)
            {
                return null;
            }

            var buffer = new byte[length];
            // the offset is from the start of the exif data, not the start of the segment data
            // we have to add 6 for the segment header "Exif\0\0"
            Array.Copy(exif.Segment.Bytes, offset + 6, buffer, 0, length); 
            return new Attribute(_attributeName, new ImageValue(buffer), AttributeFlags.ReadOnly);
        }
    }
}
