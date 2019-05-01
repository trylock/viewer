﻿using System;
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
        /// <param name="path">Path to a file from which we read.</param>
        /// <param name="tmpFileName">Path to a file to which we write.</param>
        /// <returns>Writer</returns>
        IJpegSegmentWriter CreateFromPath(string path, out string tmpFileName);
    }
    
    [Export(typeof(IJpegSegmentWriterFactory))]
    public class JpegSegmentWriterFactory : IJpegSegmentWriterFactory
    {
        private readonly Random _random = new Random();

        /// <summary>
        /// Create a temporary file for given file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tmpFileName">Path to a file to which we'll write.</param>
        /// <returns>
        /// JPEG segment writer which will write to <paramref name="tmpFileName"/>
        /// </returns>
        public IJpegSegmentWriter CreateFromPath(string filePath, out string tmpFileName)
        {
            FileStream input = null;
            for (;;)
            {
                try
                {
                    var number = 0;
                    lock (_random)
                    {
                        number = _random.Next();
                    }
                    
                    tmpFileName = filePath + ".tmp." + number;
                    input = new FileStream(tmpFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1 << 16);
                    break;
                }
                catch (IOException)
                {
                    // generate a new name
                }
            }
            
            long expectedLength = new FileInfo(filePath).Length;
            input.SetLength(expectedLength);
            return new JpegSegmentWriter(new BinaryWriter(input));
        }
    }
}
