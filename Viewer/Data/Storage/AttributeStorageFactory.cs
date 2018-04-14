using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Exif;
using Viewer.Data.Formats.Jpeg;

namespace Viewer.Data.Storage
{
    public class AttributeStorageFactory
    {
        /// <summary>
        /// Create attribute storage 
        /// </summary>
        /// <returns></returns>
        public IAttributeStorage Create()
        {
            var segmentReaderFactory = new JpegSegmentReaderFactory();
            var segmentWriterFactory = new JpegSegmentWriterFactory();
            var attrReaderFactories = new List<IAttributeReaderFactory>
            {
                new FileAttributeReaderFactory(),
                new AttributeReaderFactory(),
                new ExifAttributeReaderFactory(Settings.Instance.ExifTags)
            };
            var attrWriterFactory = new AttributeWriterFactory();

            var fileSystemStorage = new FileSystemAttributeStorage(
                segmentReaderFactory,
                segmentWriterFactory,
                attrWriterFactory,
                attrReaderFactories);

            return fileSystemStorage;
        }
    }
}
