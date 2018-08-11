using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats;

namespace Viewer.Data.Storage
{
    /// <summary>
    /// Attribute storage manages loading and storing attributes. Implementation of this interface has to be thread safe.
    /// </summary>
    /// <example>
    ///     This example loads all attributes from the file <c>C:/photo.jpeg</c>, changes/adds one attribute and stores them back to the file.
    ///     <code>
    ///         IAttributeStorage storage = ...;
    ///         var entity = storage.Load("C:/photo.jpeg");
    ///         entity.SetAttribute(new Attribute("place", new StringValue("Edinburgh")));
    ///         storage.Store(entity);
    ///     </code>
    /// </example>
    public interface IAttributeStorage
    {
        /// <summary>
        /// Load attributes from given path.
        /// </summary>
        /// <param name="path">Path to a file which contains some attributes</param>
        /// <returns>
        ///     Collection of attributes found in given file.
        ///     The collection will be empty if there are no attributes.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an invalid file path.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="InvalidDataFormatException">File is in an invalid format.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is opened by another application.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in a non-NTFS environment.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> is too long.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller is not authorized to access <paramref name="path"/>.</exception>
        IEntity Load(string path);

        /// <summary>
        /// Store attributes to a path.
        /// </summary>
        /// <param name="entity">Attributes to store in this file</param>
        void Store(IEntity entity);

        /// <summary>
        /// Permanently remove entity at given path
        /// </summary>
        /// <param name="entity">Entity to remove</param>
        void Remove(IEntity entity);

        /// <summary>
        /// Move an <paramref name="entity"/> to <paramref name="newPath"/>.
        /// </summary>
        /// <param name="entity">Entity to move</param>
        /// <param name="newPath">New path to entity</param>
        void Move(IEntity entity, string newPath);
    }

    public interface ICacheAttributeStorage : IAttributeStorage
    {
        /// <summary>
        /// Update last access time of an entity to the current time.
        /// Noop if there is no entity at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Path to an entity.</param>
        void Touch(string path);

        /// <summary>
        /// Remove all entities with access time older than (NOW - <paramref name="threshold"/>)
        /// </summary>
        /// <param name="threshold"></param>
        void Clean(TimeSpan threshold);
    }
}
