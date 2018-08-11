﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security;
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

namespace Viewer.Data.Storage
{
    /// <inheritdoc />
    /// <summary>
    /// Attribute storage which stores attributes directly in JPEG files.
    /// </summary>
    [Export(typeof(FileSystemAttributeStorage))]
    public class FileSystemAttributeStorage : IAttributeStorage
    {
        private readonly IFileSystem _fileSystem;
        private readonly IJpegSegmentReaderFactory _segmentReaderFactory;
        private readonly IJpegSegmentWriterFactory _segmentWriterFactory;
        private readonly IAttributeWriterFactory _attrWriterFactory;
        private readonly IEnumerable<IAttributeReaderFactory> _attrReaderFactories;

        /// <summary>
        /// Create a file system attribute storage
        /// </summary>
        /// <param name="fileSystem">A service used to access file system.</param>
        /// <param name="segmentReaderFactory">Factory which creates <see cref="IJpegSegmentReader"/> to read JPEG segments from a file</param>
        /// <param name="segmentWriterFactory">Factory which creates <see cref="IJpegSegmentWriter"/> to write JPEG segments to a file</param>
        /// <param name="attrWriterFactory">Factory which creates <see cref="IAttributeWriter"/> to write attributes to the Attributes segment</param>
        /// <param name="attrReaderFactories">List of factories which create <see cref="IAttributeReader"/> to read attributes from various JPEG segments</param>
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
        /// <list type="number">
        ///     <item>
        ///         <description>read all JPEG segments to memory</description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///             for each attribute reader in _attrReaderFactories:
        ///             <list type="number">
        ///                 <item>
        ///                     <description>create the reader from read segments</description>
        ///                 </item>
        ///                 <item>
        ///                     <description>read all attributes and add them to the attributes collection </description>
        ///                 </item>
        ///             </list>
        ///         </description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <returns>Collection of attributes read from the file</returns>
        public IEntity Load(string path)
        {
            try
            {
                // read all JPEG segments to memory
                IEntity attrs;
                FileInfo fileInfo;
                var segments = new List<JpegSegment>();
                using (var segmentReader = _segmentReaderFactory.CreateFromPath(path))
                {
                    fileInfo = new FileInfo(path);
                    attrs = new FileEntity(path, fileInfo.LastWriteTime, fileInfo.LastAccessTime);

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
            catch (FileNotFoundException)
            {
                return null;
            }
        }
        
        public void Store(IEntity entity)
        {
            using (var segmentReader = _segmentReaderFactory.CreateFromPath(entity.Path))
            {
                string tmpFileName;
                using (var segmentWriter = _segmentWriterFactory.CreateFromPath(entity.Path, out tmpFileName))
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
                    var serialized = Serialize(entity);
                    var segments = JpegSegmentUtils.SplitSegmentData(serialized, JpegSegmentType.App1,
                        AttributeReader.JpegSegmentHeader);

                    // write attribute segments
                    foreach (var segment in segments)
                    {
                        segmentWriter.WriteSegment(segment);
                    }

                    // write image data
                    segmentWriter.Finish(segmentReader.BaseStream);
                }

                // replace the original file with the modified file
                _fileSystem.ReplaceFile(tmpFileName, entity.Path, null);
            }
        }
        
        public void Remove(IEntity entity)
        {
            if (entity is FileEntity)
            {
                _fileSystem.DeleteFile(entity.Path);
            }
            else if (entity is DirectoryEntity)
            {
                _fileSystem.DeleteDirectory(entity.Path, true);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(entity));
            }
        }

        public void Move(IEntity entity, string newPath)
        {
            if (entity is FileEntity)
            {
                _fileSystem.MoveFile(entity.Path, newPath);
            }
            else if (entity is DirectoryEntity)
            {
                _fileSystem.MoveDirectory(entity.Path, newPath);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(entity));
            }
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