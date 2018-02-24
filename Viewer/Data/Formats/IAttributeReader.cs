using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Formats
{
    public interface IAttributeReader : IDisposable
    {
        /// <summary>
        /// Read next attribute in the input
        /// </summary>
        /// <exception cref="InvalidDataFormatException">
        ///     Data format is invalid (invalid type, unexpected end of input etc)
        /// </exception>
        /// <returns>Next attribute or null if there is none</returns>
        Attribute ReadNext();
    }
}
