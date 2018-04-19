using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
using Viewer.IO;
using Directory = MetadataExtractor.Directory;

namespace Viewer.Data.Storage
{
    /// <summary>
    /// Attribute storage which stores attributes directly in JPEG files.
    /// </summary>
    [Export(typeof(IAttributeStorage))]
    public class FileSystemAttributeStorage : IAttributeStorage
    {
        private readonly IFileSystem _fileSystem;
        private readonly IJpegSegmentReaderFactory _segmentReaderFactory;
        private readonly IJpegSegmentWriterFactory _segmentWriterFactory;
        private readonly IAttributeWriterFactory _attrWriterFactory;
        private readonly IEnumerable<IAttributeReaderFactory> _attrReaderFactories;

        [ImportingConstructor]
        public FileSystemAttributeStorage(
            IFileSystem fileSystem,
            IJpegSegmentReaderFactory segmentReaderFactory,
            IJpegSegmentWriterFactory segmentWriterFactory,
            IAttributeWriterFactory attrWriterFactory,
            [ImportMany] IAttributeReaderFactory[] attrReaderFactories)
        {
            _fileSystem = fileSystem;
            _segmentReaderFactory = segmentReaderFactory;
            _segmentWriterFactory = segmentWriterFactory;
            _attrReaderFactories = attrReaderFactories;
            _attrWriterFactory = attrWriterFactory;
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Load attributes from given file.
        /// Read algorithm:
        /// (1) read all JPEG segments to memory
        /// (2) for each attribute reader in _attrReaderFactories:
        /// (2.1) create the reader from read segments
        /// (2.2) read all attributes and add them to the attributes collection 
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <exception cref="T:Viewer.Data.Formats.InvalidDataFormatException">
        ///     Format of attributes in some segment is invalid.
        /// </exception>
        /// <returns>Collection of attributes read from the file</returns>
        public IEntity Load(string path)
        {
            // read all JPEG segments to memory
            IEntity attrs;
            FileInfo fileInfo;
            var segments = new List<JpegSegment>();
            using (var segmentReader = _segmentReaderFactory.CreateFromPath(path))
            {
                fileInfo = new FileInfo(path);
                attrs = new Entity(path, fileInfo.LastWriteTime, fileInfo.LastAccessTime);

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
                var attrReader = factory.CreateFromSegments(fileInfo, segments);
                for (;;)
                {
                    var attr = attrReader.Read();
                    if (attr == null)
                        break;
                    attrs = attrs.SetAttribute(attr);
                }
            }
            
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
                _fileSystem.ReplaceFile(tmpFileName, attrs.Path, null);
            }
        }

        public void Remove(string path)
        {
            _fileSystem.DeleteFile(path);
        }

        public void Move(string oldPath, string newPath)
        {
            _fileSystem.MoveFile(oldPath, newPath);
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
        
        private static bool IsAttributeSegment(JpegSegment segment)
        {
            const string header = AttributeReader.JpegSegmentHeader;
            return JpegSegmentUtils.MatchSegment(segment, JpegSegmentType.App1, header);
        }
    }
}
