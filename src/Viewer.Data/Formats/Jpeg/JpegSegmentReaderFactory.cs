using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;

namespace Viewer.Data.Formats.Jpeg
{
    public interface IJpegSegmentReaderFactory
    {
        /// <summary>
        /// Create JPEG segment reader from stream
        /// </summary>
        /// <param name="input">Stream with JPEG data</param>
        /// <returns>JPEG segment reader</returns>
        IJpegSegmentReader CreateFromStream(Stream input);
    }

    [Export(typeof(IJpegSegmentReaderFactory))]
    public class JpegSegmentReaderFactory : IJpegSegmentReaderFactory
    {
        public IJpegSegmentReader CreateFromStream(Stream input)
        {
            return new JpegSegmentReader(new BinaryReader(input));
        }
    }
}
