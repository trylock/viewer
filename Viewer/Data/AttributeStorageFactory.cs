using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Jpeg;

namespace Viewer.Data
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
                new AttributeReaderFactory(),
                new ExifAttributeReaderFactory()
            };
            var attrWriterFactory = new AttributeWriterFactory();
            var collectionFactory = new AttributeCollectionFactory();
            return new FileSystemAttributeStorage(
                segmentReaderFactory, 
                segmentWriterFactory, 
                collectionFactory, 
                attrWriterFactory, 
                attrReaderFactories);
        }
    }
}
