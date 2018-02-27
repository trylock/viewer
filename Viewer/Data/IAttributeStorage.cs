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
        /// <exception cref="InvalidDataFormatException">
        ///     Attribute format in the file is invalid
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     File was not found
        /// </exception>
        /// <returns>
        ///     Collection of attributes found in given file.
        ///     The collection will be empty if there are no attributes.
        /// </returns>
        AttributeCollection Load(string path);

        /// <summary>
        /// Store attributes to a path.
        /// This is a blocking operation.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <param name="attrs">Attributes to store in this file</param>
        /// <exception cref="FileNotFoundException">
        ///     File was not found
        /// </exception>
        void Store(string path, AttributeCollection attrs);
    }
}
