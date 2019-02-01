using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Viewer.Data.Formats.Jpeg;

namespace Viewer.Data.Formats.Attributes
{
    /// <summary>
    /// This serializer can read attributes in the legacy format. It is provided for
    /// backwards compatibility.
    /// </summary>
    [Export(typeof(IJpegSerializer))]
    public class AttributeJpegSerializer : IJpegSerializer
    {
        public IEnumerable<string> MetadataAttributes => Enumerable.Empty<string>();

        public IEnumerable<Attribute> Deserialize(IReadOnlyList<JpegSegment> segments)
        {
            var data = JpegSegmentUtils.JoinSegmentData(segments, JpegSegmentType.App1, AttributeReader.JpegSegmentHeader);
            return new AttributeReader(new BinaryReader(new MemoryStream(data)));
        }

        public List<JpegSegment> Serialize(IReadOnlyList<JpegSegment> segments, IEnumerable<Attribute> attributes)
        {
            return new List<JpegSegment>(segments);
        }
    }
}
