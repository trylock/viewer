using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public interface IAttributeStorage
    {
        /// <summary>
        /// Load attributes from given path
        /// </summary>
        /// <param name="path">Path to a file which contains some attributes</param>
        /// <exception cref="FileNotFoundException">Given file was not found</exception>
        /// <returns>Task whose result is a collection of attributes form given file</returns>
        Task<AttributeCollection> Load(string path);

        /// <summary>
        /// Store attributes to a path
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <param name="attrs">Attributes to store in this file</param>
        /// <exception cref="FileNotFoundException">Given file was not found</exception>
        /// <returns>Task completed when the attributes are successfully written</returns>
        Task Store(string path, AttributeCollection attrs);
    }
}
