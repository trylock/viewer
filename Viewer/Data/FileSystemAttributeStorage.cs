using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.IO;
using Viewer.Data.Formats;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Jpeg;
using Directory = MetadataExtractor.Directory;

namespace Viewer.Data
{
    /// <summary>
    /// Attribute storage which stores attributes directly in JPEG files.
    /// </summary>
    public class FileSystemAttributeStorage : IAttributeStorage
    {
        private readonly IJpegSegmentReaderFactory _segmentReaderFactory;
        private readonly IJpegSegmentWriterFactory _segmentWriterFactory;
        private readonly IAttributeWriterFactory _attrWriterFactory;
        private readonly IList<IAttributeReaderFactory> _attrReaderFactories;

        public FileSystemAttributeStorage(
            IJpegSegmentReaderFactory segmentReaderFactory, 
            IJpegSegmentWriterFactory segmentWriterFactory,
            IAttributeWriterFactory attrWriterFactory,
            IList<IAttributeReaderFactory> attrReaderFactories)
        {
            _segmentReaderFactory = segmentReaderFactory;
            _segmentWriterFactory = segmentWriterFactory;
            _attrWriterFactory = attrWriterFactory;
            _attrReaderFactories = attrReaderFactories;
        }

        /// <summary>
        /// Load attributes from given file.
        /// Read algorithm:
        /// (1) read all JPEG segments to memory
        /// (2) for each attribute reader in _attrReaderFactories:
        /// (2.1) create the reader from read segments
        /// (2.2) read all attributes and add them to the attributes collection 
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <exception cref="InvalidDataFormatException">
        ///     Format of attributes in some segment is invalid.
        /// </exception>
        /// <returns>Collection of attributes read from the file</returns>
        public AttributeCollection Load(string path)
        {
            // read all JPEG segments to memory
            AttributeCollection attrs;
            var segments = new List<JpegSegment>();
            using (var segmentReader = _segmentReaderFactory.CreateFromPath(path))
            {
                attrs = new AttributeCollection(path);

                for (;;)
                {
                    // read segment
                    var segment = segmentReader.ReadNext();
                    if (segment == null)
                        break;

                    // we only parse data in APP1 segments
                    if (segment.Type != JpegSegmentType.App1)
                        continue;

                    segments.Add(segment);
                }
            }

            // read attributes from all sources and add them to the collection
            foreach (var factory in _attrReaderFactories)
            {
                var attrReader = factory.CreateFromSegments(segments);
                for (;;)
                {
                    var attr = attrReader.ReadNext();
                    if (attr == null)
                        break;
                    attrs.SetAttribute(attr);
                }
            }

            return attrs;
        }
        
        public void Store(string path, AttributeCollection attrs)
        {
            string tmpFileName;

            using (var segmentReader = _segmentReaderFactory.CreateFromPath(path))
            {
                using (var segmentWriter = _segmentWriterFactory.CreateFromPath(path, out tmpFileName))
                {
                    // copy all but attribute segments
                    for (;;)
                    {
                        var segment = segmentReader.ReadNext();
                        if (segment == null)
                            break;

                        if (IsAttributeSegment(segment) ||
                            segment.Type == JpegSegmentType.Sos)
                        {
                            continue;
                        }

                        segmentWriter.WriteSegment(segment);
                    }

                    // write attributes
                    var attrWriter = _attrWriterFactory.Create();
                    foreach (var attr in attrs)
                    {
                        attrWriter.Write(attr);
                    }

                    // write attribute segments
                    var segments = attrWriter.Finish();
                    foreach (var segment in segments)
                    {
                        segmentWriter.WriteSegment(segment);
                    }

                    // write image data
                    segmentWriter.Finish(segmentReader.BaseStream);
                }

                // replace the original file with the modified file
                File.Replace(tmpFileName, path, null);
            }
        }

        private bool IsAttributeSegment(JpegSegment segment)
        {
            const string header = AttributeReader.JpegSegmentHeader;
            return segment.Type == JpegSegmentType.App1 &&
                   Encoding.UTF8.GetString(segment.Bytes, 0, header.Length) == header;
        }
    }
}
