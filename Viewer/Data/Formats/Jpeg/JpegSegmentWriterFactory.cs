using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Formats.Jpeg
{
    public interface IJpegSegmentWriterFactory
    {
        /// <summary>
        /// Create JPEG segment writer from path to a file.
        /// </summary>
        /// <param name="path">Path to a file from which we read.</param>
        /// <param name="tmpFileName">Path to a file to which we write.</param>
        /// <returns>Writer</returns>
        IJpegSegmentWriter CreateFromPath(string path, out string tmpFileName);
    }
    
    [Export(typeof(IJpegSegmentWriterFactory))]
    public class JpegSegmentWriterFactory : IJpegSegmentWriterFactory
    {
        private Random _random = new Random();

        /// <summary>
        /// Create a temporary file for given file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tmpFileName">Path to a file to which we write.</param>
        /// <returns></returns>
        public IJpegSegmentWriter CreateFromPath(string filePath, out string tmpFileName)
        {
            FileStream input = null;
            for (;;)
            {
                try
                {
                    tmpFileName = filePath + ".tmp." + _random.Next();
                    input = new FileStream(tmpFileName, FileMode.CreateNew, FileAccess.Write);
                    break;
                }
                catch (IOException e) when (e.GetType() == typeof(IOException))
                {
                    // generate a new name
                }
            }
            return new JpegSegmentWriter(new BinaryWriter(input));
        }
    }
}
