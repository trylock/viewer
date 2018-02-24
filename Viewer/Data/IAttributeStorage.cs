using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats;

namespace Viewer.Data
{
    /// <summary>
    /// Facade for the attribute storage system. 
    /// </summary>
    public interface IAttributeStorage
    {
        /// <summary>
        /// Load attributes from given path.
        /// This is a blocking operation.
        /// </summary>
        /// <param name="path">Path to a file which contains some attributes</param>
        /// <exception cref="FileNotFoundException">Given file was not found</exception>
        /// <exception cref="InvalidDataFormatException">Attribute format is invalid</exception>
        /// <returns>Collection of attributes found in given file</returns>
        AttributeCollection Load(string path);

        /// <summary>
        /// Store attributes to a path.
        /// This is a blocking operation.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <param name="attrs">Attributes to store in this file</param>
        /// <exception cref="FileNotFoundException">Given file was not found</exception>
        void Store(string path, AttributeCollection attrs);
    }
}
