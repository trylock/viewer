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
    public class ThumbnaiExifAttributeParser : IExifAttributeParser
    {
        /// <summary>
        /// Try parsing thumbnail from exif
        /// </summary>
        /// <param name="exif">Exif metadata</param>
        /// <returns>Thumbnail attribute</returns>
        public Attribute Parse(ExifMetadata exif)
        {
            var dir = exif.GetDirectoryOfType<ExifThumbnailDirectory>();
            if (dir == null)
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
            return new ImageAttribute("thumbnail", buffer, AttributeFlags.ReadOnly);
        }
    }
}
