using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;

namespace Viewer.Data.Formats.Jpeg
{
    /// <summary>
    /// This class serializes attributes from JPEG files. It reads JPEG segments and uses
    /// all available <see cref="IJpegSerializer"/>s to read/write attributes.
    /// </summary>
    [Export(typeof(IAttributeSerializer))]
    public class JpegAttributeSerializer : IAttributeSerializer
    {
        private readonly IJpegSerializer[] _serializers;
        private readonly IJpegSegmentReaderFactory _segmentReaderFactory;
        private readonly IJpegSegmentWriterFactory _segmentWriterFactory;

        public IEnumerable<string> MetadataAttributes => 
            _serializers.SelectMany(item => item.MetadataAttributes);

        [ImportingConstructor]
        public JpegAttributeSerializer(
            IJpegSegmentReaderFactory segmentReaderFactory,
            IJpegSegmentWriterFactory segmentWriterFactory,
            [ImportMany] IJpegSerializer[] serializers)
        {
            _segmentReaderFactory = segmentReaderFactory;
            _segmentWriterFactory = segmentWriterFactory;
            _serializers = serializers;
        }
        
        public bool CanRead(FileInfo file)
        {
            return string.Equals(file.Extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(file.Extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(file.Extension, ".jfif", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(file.Extension, ".jif", StringComparison.OrdinalIgnoreCase);
        }

        public bool CanWrite(FileInfo file)
        {
            return CanRead(file);
        }

        public IEnumerable<Attribute> Deserialize(FileInfo file, Stream input)
        {
            // read all JPEG segments to memory
            var segments = new List<JpegSegment>();
            using (var reader = _segmentReaderFactory.CreateFromStream(input))
            {
                for (;;)
                {
                    var segment = reader.ReadSegment();
                    if (segment == null)
                    {
                        break;
                    }
                    segments.Add(segment);
                }
            }

            // parse JPEG segments
            var result = new Dictionary<string, Attribute>();
            foreach (var serializer in _serializers)
            {
                var attributes = serializer.Deserialize(segments);
                foreach (var attribute in attributes)
                {
                    result[attribute.Name] = attribute;
                }
            }

            return result.Values;
        }

        public void Serialize(FileInfo file, Stream input, Stream output, IEnumerable<Attribute> attributes)
        {
            using (var reader = _segmentReaderFactory.CreateFromStream(input))
            using (var writer = _segmentWriterFactory.CreateFromStream(input, output))
            {
                // read all metadata segments to memory
                var segments = new List<JpegSegment>();
                for (; ; )
                {
                    var segment = reader.ReadSegment();
                    if (segment == null || segment.Type == JpegSegmentType.Sos)
                    {
                        break;
                    }
                    segments.Add(segment);
                }

                // modify segments as necessary
                foreach (var serializer in _serializers)
                {
                    segments = serializer.Serialize(segments, attributes);
                }

                // write modified segments
                Trace.Assert(segments.Count > 0);
                Trace.Assert(segments[0].Type == JpegSegmentType.Soi);

                foreach (var segment in segments)
                {
                    writer.WriteSegment(segment);
                }
                
                // copy the rest of the file
                writer.Finish(input);
            }
        }
    }
}
