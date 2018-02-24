using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;

namespace Viewer.Data.Formats
{
    public interface IAttributeReaderFactory
    {
        /// <summary>
        /// Create attribute reader from JPEG segments loaded in memory
        /// </summary>
        /// <param name="segments">JPEG segments</param>
        /// <returns>Attribute reader of given segments</returns>
        IAttributeReader CreateFromSegments(IList<JpegSegment> segments);
    }
}
