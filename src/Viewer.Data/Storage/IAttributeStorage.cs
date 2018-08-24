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
        ///     If there are no attributes, the collection will be empty.
        ///     It will be null if the file <paramref name="path"/> does not exist.
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
    /// Represents a collection of pending changes. No change will be done to data unless
    /// <see cref="Commit" /> is called. Batches are recursive. 
    /// </summary>
    public interface IRecursiveBatch : IDisposable
    {
        /// <summary>
        /// Parent batch. If there is a parent batch, no changes will be commited on
        /// <see cref="Commit"/>. <see cref="Commit"/>ted nested batches cannot be
        /// <see cref="Rollback"/>ed.
        /// </summary>
        IRecursiveBatch Parent { get; }

        /// <summary>
        /// Commit all changes in the batch and all nested batches which have not been rollbacked.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If this batch has been rollbacked or its <see cref="Parent"/> has been committed/rollbacked.
        /// </exception>
        void Commit();

        /// <summary>
        /// Rollback all changes done in this batch and all nested batches (even if they have been
        /// commited). This is automatically called when the object is <see cref="IDisposable.Dispose"/>ed
        /// and <see cref="Commit"/> wasn't called.
        /// </summary>
        /// <remarks>
        /// Changes in <see cref="Parent"/> batch won't be <see cref="Rollback"/>ed.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// If this batch or its <see cref="Parent"/> is commited.
        /// </exception>
        void Rollback();
    }

    public interface ICacheAttributeStorage : IAttributeStorage
    {
        /// <summary>
        /// Update last access time of <paramref name="entity"/>. Last access times determines which
        /// entities are deleted from the storage. Note, this does *not* change access time of the
        /// entity's file in file system.
        /// </summary>
        /// <param name="entity">Entity whose access time will be modified in this storage.</param>
        void Touch(IEntity entity);

        /// <summary>
        /// Remove all entities with access time older than (NOW - <paramref name="lastAccessTimeThreshold"/>)
        /// </summary>
        /// <param name="lastAccessTimeThreshold">
        ///     Entities which have not been accessed for this period of time will be deleted.
        /// </param>
        /// <param name="fileCountThreshold">
        ///     The files in the cache are sorted by the last access time. Only some records will be kept.
        /// </param>
        void Clean(TimeSpan lastAccessTimeThreshold, int fileCountThreshold);

        /// <summary>
        /// Begin a new batch on the data stored in this storage.
        /// </summary>
        /// <returns>Newly create batch.</returns>
        /// <seealso cref="IRecursiveBatch"/>
        IRecursiveBatch BeginBatch();
    }
}
