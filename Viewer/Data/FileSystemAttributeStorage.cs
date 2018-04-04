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
        public IEntity Load(string path)
        {
            // read all JPEG segments to memory
            Entity attrs;
            var segments = new List<JpegSegment>();
            using (var segmentReader = _segmentReaderFactory.CreateFromPath(path))
            {
                attrs = Entity.CreateFromFile(path);

                for (;;)
                {
                    // read segment
                    var segment = segmentReader.ReadSegment();
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
                    var attr = attrReader.Read();
                    if (attr == null)
                        break;
                    attrs.SetAttribute(attr);
                }
            }
            
            attrs.ResetDirty();
            return attrs;
        }
        
        public void Store(IEntity attrs)
        {
            string tmpFileName;

            using (var segmentReader = _segmentReaderFactory.CreateFromPath(attrs.Path))
            {
                using (var segmentWriter = _segmentWriterFactory.CreateFromPath(attrs.Path, out tmpFileName))
                {
                    // copy all but attribute segments
                    for (;;)
                    {
                        var segment = segmentReader.ReadSegment();
                        if (segment == null)
                            break;

                        if (IsAttributeSegment(segment) ||
                            segment.Type == JpegSegmentType.Sos)
                        {
                            continue;
                        }

                        segmentWriter.WriteSegment(segment);
                    }

                    // serialize attributes to JpegSegments
                    var serialized = Serialize(attrs);
                    var segments = JpegSegmentUtils.SplitSegmentData(serialized, JpegSegmentType.App1, AttributeReader.JpegSegmentHeader);

                    // write attribute segments
                    foreach (var segment in segments)
                    {
                        segmentWriter.WriteSegment(segment);
                    }

                    // write image data
                    segmentWriter.Finish(segmentReader.BaseStream);
                }

                // replace the original file with the modified file
                File.Replace(tmpFileName, attrs.Path, null);
            }
        }

        public void Remove(string path)
        {
            File.Delete(path);
        }

        public void Move(string oldPath, string newPath)
        {
            File.Move(oldPath, newPath);
        }

        private byte[] Serialize(IEnumerable<Attribute> attrs)
        {
            using (var serialized = new MemoryStream())
            {
                var writer = _attrWriterFactory.Create(serialized);
                foreach (var attr in attrs)
                {
                    writer.Write(attr);
                }

                return serialized.ToArray();
            }
        }

        public void Flush()
        {
        }

        private bool IsAttributeSegment(JpegSegment segment)
        {
            const string header = AttributeReader.JpegSegmentHeader;
            return JpegSegmentUtils.MatchSegment(segment, JpegSegmentType.App1, header);
        }
    }
}
