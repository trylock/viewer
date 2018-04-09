using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats;

namespace Viewer.Data.Storage
{
    /// <summary>
    /// Attribute storage manages loading and storing attributes.
    /// </summary>
    /// <example>
    ///     This example loads attributes from file, changes them and stores them back to the file.
    ///     <code>
    ///         IAttributeStorage storage = ...;
    ///         var attrs = storage.Load("C:/path.jpeg");
    ///         attrs = attrs.SetAttribute(new IntAttribute("visited", 1));
    ///         storage.Store(attrs);
    ///     </code>
    /// </example>
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
        IEntity Load(string path);

        /// <summary>
        /// Store attributes to a path.
        /// </summary>
        /// <param name="attrs">Attributes to store in this file</param>
        void Store(IEntity attrs);

        /// <summary>
        /// Permanently remove entity at given path
        /// </summary>
        /// <param name="path">Path to an entity</param>
        void Remove(string path);

        /// <summary>
        /// Move an entity from <paramref name="oldPath"/> to <paramref name="newPath"/>.
        /// </summary>
        /// <param name="oldPath">Old path of an entity</param>
        /// <param name="newPath">New path to entity</param>
        void Move(string oldPath, string newPath);
    }
}
