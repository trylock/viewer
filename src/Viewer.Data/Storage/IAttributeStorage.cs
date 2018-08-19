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

    [Flags]
    public enum StoreFlags
    {
        /// <summary>
        /// Noting will be stored. Calling <see cref="IAttributeStorage.Store"/> with this flag
        /// is basically a nop.
        /// </summary>
        None = 0,

        /// <summary>
        /// Update the access time of this entity.
        /// </summary>
        Touch = 0x1,

        /// <summary>
        /// Store metadata attributes like Exif attributes and thumbnail.
        /// </summary>
        Metadata = 0x2,

        /// <summary>
        /// Store custom attributes assigned by user.
        /// </summary>
        Attribute = 0x4,

        /// <summary>
        /// Store everything.
        /// </summary>
        Everything = Touch | Metadata | Attribute
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
        /// <param name="flags">
        ///     Determines what will be stored. Storage implementation is free to ignore some
        ///     values in <paramref name="flags"/> if it does not support storing these values.
        ///     It must not, however, store anything which is not specified in <paramref name="flags"/>
        /// </param>
        void Store(IEntity entity, StoreFlags flags);

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
        /// Remove all entities with access time older than (NOW - <paramref name="threshold"/>)
        /// </summary>
        /// <param name="threshold"></param>
        void Clean(TimeSpan threshold);
    }
}
