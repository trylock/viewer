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
    /// Thread-safe storage of attributes from which it is possible to read entities.
    /// </summary>
    /// <remarks>
    /// > [!IMPORTANT]
    /// > This class implements <see cref="IDisposable"/> interface. You should call the Dispose
    /// > method once you are done with the instance.
    ///
    /// The <see cref="Load"/> method of this class is thread-safe. The Dispose method is **not**.
    /// You must not call the Dispose method from multiple threads and/or concurrently with other
    /// methods.
    /// </remarks>
    public interface IReadableAttributeStorage : IDisposable
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
    }

    /// <inheritdoc />
    /// <summary>
    /// Simple proxy which will only allow users to read attributes from given storage. 
    /// </summary>
    /// <remarks>It propagates Load method calls to given storage.</remarks>
    public class ReadableStorageProxy : IReadableAttributeStorage
    {
        private readonly IReadableAttributeStorage _storage;

        public ReadableStorageProxy(IReadableAttributeStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public IEntity Load(string path)
        {
            return _storage.Load(path);
        }

        public void Dispose()
        {
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Attribute storage manages loading and storing attributes. Implementation of this interface
    /// has to be thread safe.
    /// </summary>
    /// <example>
    /// This example loads all attributes from the file <c>C:/photo.jpeg</c>, changes/adds one
    /// attribute and stores all attributes back to the file.
    /// <code>
    ///     IAttributeStorage storage = ...;
    ///     var entity = storage.Load("C:/photo.jpeg");
    ///     entity.SetAttribute(new Attribute("place", new StringValue("Edinburgh")));
    ///     storage.Store(entity);
    /// </code>
    ///
    /// If you want to load more than 1 entity, you should use the <see cref="CreateReader"/>
    /// method. The following example shows how to efficiently load all files in a folder:
    ///
    /// <code>
    /// IAttributeStorage storage = ...;
    /// var results = new List&lt;IEntity&gt;();
    /// using (var readable = storage.CreateReader())
    /// {
    ///     foreach (var file in Directory.EnumerateFiles("C:/photos"))
    ///     {
    ///         results.Add(readable.Load(file));
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IAttributeStorage : IReadableAttributeStorage
    {
        /// <summary>
        /// Create a new reader which reads from this storage. The main purpose of this is to allow
        /// multiple load requests to share a common state. This can speed up load request times
        /// for some implementations.
        /// </summary>
        /// <remarks>
        /// > [!NOTE]
        /// > If an implementation can't do any optimizations, it still should **not** return
        /// > itself as the caller is supposed to Dispose the returned value. Return
        /// > <see cref="ReadableStorageProxy"/> instead.
        /// </remarks>
        /// <returns>
        /// Attribute reader which can load attributes from this storage. 
        /// </returns>
        IReadableAttributeStorage CreateReader();

        /// <summary>
        /// Store attributes to a file.
        /// </summary>
        /// <param name="entity">Attributes to store in this file</param>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null</exception>
        void Store(IEntity entity);

        /// <summary>
        /// Store just the thumbnail attribute of this entity. It will be no-op, if the entity is
        /// not stored in this storage or the storage does not support storing thumbnails. 
        /// </summary>
        /// <param name="entity">Entity who's thumbnail will be stored.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null</exception>
        void StoreThumbnail(IEntity entity);

        /// <summary>
        /// Permanently remove entity at given path
        /// </summary>
        /// <param name="entity">Entity to remove</param>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null</exception>
        void Delete(IEntity entity);

        /// <summary>
        /// Move an <paramref name="entity"/> to <paramref name="newPath"/>.
        /// </summary>
        /// <param name="entity">Entity to move</param>
        /// <param name="newPath">New path to entity</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entity"/> or <paramref name="newPath"/> is null
        /// </exception>
        void Move(IEntity entity, string newPath);
    }

    /// <inheritdoc />
    /// <summary>
    /// Deferred attribute storage can defer all write requests (everything but
    /// <see cref="IReadableAttributeStorage.Load" />) for later. Specifically, until
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
