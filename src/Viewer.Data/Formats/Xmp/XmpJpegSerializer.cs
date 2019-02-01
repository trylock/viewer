using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Jpeg;
using XmpCore;

namespace Viewer.Data.Formats.Xmp
{
    [Export(typeof(IJpegSerializer))]
    public class XmpJpegSerializer : IJpegSerializer
    {
        public IEnumerable<string> MetadataAttributes => Enumerable.Empty<string>();

        public IEnumerable<Attribute> Deserialize(IReadOnlyList<JpegSegment> segments)
        {
            var data = XmpBase.ParseSegments(segments);
            return new XmpUserAttributeReader(data);
        }

        public List<JpegSegment> Serialize(IReadOnlyList<JpegSegment> segments, IEnumerable<Attribute> attributes)
        {
            var data = XmpBase.ParseSegments(segments) ?? XmpMetaFactory.Create();
            var writer = new XmpUserAttributeWriter(data);
            writer.Write(attributes);

            var xmpSegments = XmpBase.SerializeSegments(data);
            var xmpSegmentsAdded = false;
            var result = new List<JpegSegment>();
            var index = 0;
            foreach (var segment in segments)
            {
                // insert the new XMP data after SOI, Exif/JFIF segments
                if (index == 2)
                {
                    // TODO: what about JFXX segment?
                    result.AddRange(xmpSegments);
                    xmpSegmentsAdded = true;
                }

                if (segment.MatchSegment(JpegSegmentType.App1, XmpBase.StandardSegmentHeader))
                {
                    // skip XMP segments 
                    continue;
                }
                else if (segment.MatchSegment(JpegSegmentType.App1, 
                    AttributeReader.JpegSegmentHeader)) 
                {
                    // skip segments with legacy attribute format
                    continue;
                }
                else
                {
                    result.Add(segment);
                }

                ++index;
            }

            if (!xmpSegmentsAdded)
            {
                result.AddRange(xmpSegments);
            }

            return result;
        }
    }
}
