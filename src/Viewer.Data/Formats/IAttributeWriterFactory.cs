using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Formats
{
    public interface IAttributeWriterFactory
    {
        /// <summary>
        /// Create attribute writer
        /// </summary>
        /// <param name="output">
        ///     Output stream where the writer will store serialized attribute data
        /// </param>
        /// <returns>New attribute writer</returns>
        IAttributeWriter Create(Stream output);
    }
}
