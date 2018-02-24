using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Formats
{
    public interface IAttributeWriterFactory
    {
        /// <summary>
        /// Create a new attribute writer.
        /// </summary>
        /// <returns>New attribute writer</returns>
        IAttributeWriter Create();
    }
}
