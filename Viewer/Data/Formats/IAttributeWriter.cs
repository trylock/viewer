using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;

namespace Viewer.Data.Formats
{
    public interface IAttributeWriter
    {
        /// <summary>
        /// Write attribute
        /// </summary>
        /// <param name="attr">Attribute to write</param>
        void Write(Attribute attr);
    }
}
