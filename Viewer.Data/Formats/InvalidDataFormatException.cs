using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Formats
{
    /// <summary>
    /// Exception thrown when the input data format is invalid.
    /// </summary>
    public class InvalidDataFormatException : Exception
    {
        public long Offset { get; }

        public InvalidDataFormatException(long offset, string message) 
            : base(message)
        {
            Offset = offset;
        }

        public InvalidDataFormatException(long offset, string message, Exception parent) 
            : base(message, parent)
        {
            Offset = offset;
        }
    }
}
