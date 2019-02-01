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
    public interface IJpegSegmentWriterFactory
    {
        /// <summary>
        /// Create JPEG segment writer from path to a file.
        /// </summary>
        /// <param name="input">Stream with original data</param>
        /// <param name="output">Stream where the new data will be written to</param>
        /// <returns>Writer</returns>
        IJpegSegmentWriter CreateFromStream(Stream input, Stream output);
    }
    
    [Export(typeof(IJpegSegmentWriterFactory))]
    public class JpegSegmentWriterFactory : IJpegSegmentWriterFactory
    {
        public IJpegSegmentWriter CreateFromStream(Stream input, Stream output)
        {
            long expectedLength = input.Length;
            output.SetLength(expectedLength);
            return new JpegSegmentWriter(new BinaryWriter(output));
        }
    }
}
