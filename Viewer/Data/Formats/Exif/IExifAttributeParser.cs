using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;

namespace Viewer.Data.Formats.Exif
{
    /// <summary>
    /// Create attribute from parsed exif data
    /// </summary>
    public interface IExifAttributeParser
    {
        /// <summary>
        /// Parse attribute given Exif directories
        /// </summary>
        /// <param name="exif">Parsed exif metadata</param>
        /// <returns>Parsed attribute</returns>
        Attribute Parse(IExifMetadata exif);
    }
}
