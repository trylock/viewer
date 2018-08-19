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
    /// Result of the <see cref="IAttributeStorage.Load"/> operation.
    /// </summary>
    public struct LoadResult
    {
        /// <summary>
        /// Loaded entity. This value can be null if the entity has not been found.
        /// If the entity has been found but it does not contain any attributes, this value won't be null.
        /// Instead, its attributes collection will be empty. 
        /// </summary>
        public IEntity Entity { get; }

        /// <summary>
        /// This is a lower bound on the number or bytes read from a file.
        /// It can be 0, if no I/O was necessary to retrieve the file.
        /// It can be 0, if the storage cannot estimate performed I/O (e.g. because it has been done using some library)
        /// </summary>
        public long BytesRead { get; }
        
        /// <summary>
        /// Create a new load result
        /// </summary>
        /// <param name="entity">Loaded entity</param>
        /// <param name="bytesRead"></param>
        public LoadResult(IEntity entity, long bytesRead)
        {
            Entity = entity;
            BytesRead = bytesRead;
        }
    }
    
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
        ///     If the file does not exist, <see cref="LoadResult.Entity"/> will be null.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an invalid file path.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="InvalidDataFormatException">File is in an invalid format.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is opened by another application.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in a non-NTFS environment.</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> is too long.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller is not authorized to access <paramref name="path"/>.</exception>
        LoadResult Load(string path);

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
        /// Remove all entities with access time older than (NOW - <paramref name="threshold"/>)
        /// </summary>
        /// <param name="threshold"></param>
        void Clean(TimeSpan threshold);
    }
}
