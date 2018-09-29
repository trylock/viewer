using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;

namespace Viewer.Data.Formats
{
    public interface IAttributeReaderFactory
    {
        /// <summary>
        /// List of names of metadata attributes a reader returned by this factory can return. All
        /// other attribute names must be user attributes.
        /// </summary>
        IEnumerable<string> MetadataAttributeNames { get; }

        /// <summary>
        /// Create attribute reader from JPEG segments loaded in memory
        /// </summary>
        /// <param name="file">File metadata</param>
        /// <param name="segments">JPEG segments</param>
        /// <returns>Attribute reader of given segments</returns>
        IAttributeReader CreateFromSegments(FileInfo file, IEnumerable<JpegSegment> segments);
    }
}
