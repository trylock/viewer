using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.IO;

namespace Viewer.Data
{
    public class FileSystemAttributeStorage : IAttributeStorage
    {
        public Task<AttributeCollection> Load(string path)
        {
            throw new NotImplementedException();
        }

        public Task Store(string path, AttributeCollection attrs)
        {
            throw new NotImplementedException();
        }
    }
}
