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
    /// <inheritdoc />
    /// <summary>
    /// Attribute storage manages loading and storing attributes. Implementation of this interface
    /// has to be thread safe.
    /// </summary>
    /// <example>
    ///     This example loads all attributes from the file <c>C:/photo.jpeg</c>, changes/adds one
    ///     attribute and stores them back to the file.
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
        ///     If there are no attributes, the collection will be empty.
        ///     It will be null if the file <paramref name="path"/> does not exist.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path"/> is an invalid file path.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="InvalidDataFormatException">File is in an invalid format.</exception>
        /// <exception cref="IOException">
        ///     <paramref name="path"/> is opened by another application.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="path"/> refers to a non-file device, such as "con:", "com1:",
        ///     "lpt1:", etc. in a non-NTFS environment.
        /// </exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> is too long.</exception>
        /// <exception cref="SecurityException">
        ///     The caller does not have the required permission.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     The caller is not authorized to access <paramref name="path"/>.
        /// </exception>
        IEntity Load(string path);

        /// <summary>
        /// Store attributes to a path.
        /// </summary>
        /// <param name="entity">Attributes to store in this file</param>
        void Store(IEntity entity);

        /// <summary>
        /// Store just the thumbnail attribute of this entity. It will be no-op, if the entity is
        /// not stored in this storage or the storage does not support storing thumbnails. 
        /// </summary>
        /// <param name="entity">Entity who's thumbnail will be stored.</param>
        void StoreThumbnail(IEntity entity);

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

    /// <inheritdoc />
    /// <summary>
    /// Deferred attribute storage deferrs all write requests (everything but
    /// <see cref="IAttributeStorage.Load" />) for later. Specifically, until
    /// <see cref="ApplyChanges" /> is called.
    /// </summary>
    public interface IDeferredAttributeStorage : IAttributeStorage
    {
        /// <summary>
        /// Apply all changes done to the storage so far. Failure of a single operation won't cancel
        /// any other pending operation. If an operation fails due to the storage being busy, it can be
        /// re-added to the pending queue so that it will be retried next time you call this method.
        /// </summary>
        /// <exception cref="AggregateException">When one or more operation fails.</exception>
        void ApplyChanges();
    }
}
