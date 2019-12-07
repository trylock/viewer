using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats.Exif;
using Viewer.IO;

namespace Viewer.Data.Storage
{
    /// <summary>
    /// This class queues write requests in memory. Load method can serve data from this
    /// queue using the <see cref="TryLoad"/> method. Multiple request on the same file
    /// are serialized. <see cref="ConsumeRequests"/> method returns at most 1 operation
    /// per file.
    /// </summary>
    public abstract class DeferredAttributeStorage : IDeferredAttributeStorage
    {
        protected abstract class Request
        {
        }

        protected sealed class StoreRequest : Request
        {
            public IEntity Entity { get; }

            public DateTime LastWriteTime { get; }

            public StoreRequest(IEntity entity, DateTime lastWriteTime)
            {
                Entity = entity;
                LastWriteTime = lastWriteTime;
            }
        }

        protected sealed class StoreThumbnailRequest : Request
        {
            public byte[] Thumbnail { get; }

            public StoreThumbnailRequest(byte[] thumbnail)
            {
                Thumbnail = thumbnail;
            }
        }

        protected sealed class TouchRequest : Request
        {
            public DateTime AccessTime { get; }

            public TouchRequest(DateTime accessTime)
            {
                AccessTime = accessTime;
            }
        }

        protected sealed class DeleteRequest : Request
        {
        }

        /// <summary>
        /// Pending requests. Use lock on this property.
        /// </summary>
        private readonly Dictionary<string, Request> _requests =
            new Dictionary<string, Request>(StringComparer.CurrentCultureIgnoreCase);

        public abstract void Dispose();

        public abstract IEntity Load(string path);

        public abstract IReadableAttributeStorage CreateReader();

        public abstract void Move(IEntity entity, string newPath);

        public abstract void ApplyChanges();

        /// <summary>
        /// Remove all pending requests from the queue and return them.
        /// </summary>
        /// <returns>Pending requests.</returns>
        protected KeyValuePair<string, Request>[] ConsumeRequests()
        {
            KeyValuePair<string, Request>[] requests;
            lock (_requests)
            {
                requests = _requests.ToArray();
                _requests.Clear();
            }

            return requests;
        }

        /// <summary>
        /// Try to load entity from the memory queue
        /// </summary>
        /// <param name="path">Path to entity</param>
        /// <param name="entity">
        /// Loaded entity. This can be null if it was deleted. If this
        /// function returns false, <paramref name="entity"/> is set
        /// to a new <see cref="FileEntity"/>.
        /// </param>
        /// <returns>
        /// true iff <see cref="Load"/> should return <paramref name="entity"/>. Note,
        /// <paramref name="entity"/> can be null even if this function returns true.
        /// </returns>
        protected bool TryLoad(string path, out IEntity entity)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            path = PathUtils.NormalizePath(path);

            // check if there is a pending change in main memory
            lock (_requests)
            {
                if (_requests.TryGetValue(path, out var req))
                {
                    if (req is StoreRequest store)
                    {
                        entity = store.Entity;
                        return true;
                    }

                    if (req is DeleteRequest)
                    {
                        entity = null;
                        return true;
                    }

                    if (req is TouchRequest)
                    {
                        _requests[path] = new TouchRequest(DateTime.Now);
                    }
                }
                else
                {
                    _requests[path] = new TouchRequest(DateTime.Now);
                }
            }

            entity = new FileEntity(path); 
            return false;
        }

        private DateTime GetLastWriteTime(string path)
        {
            var lastWriteTime = DateTime.MinValue;
            try
            {
                var fi = new FileInfo(path);
                lastWriteTime = fi.LastWriteTime;
            }
            catch (IOException)
            {
                // ignore IO errors
            }
            return lastWriteTime;
        }

        /// <summary>
        /// Replace current request with <paramref name="entity"/> to store request with new data.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Store(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var request = new StoreRequest(entity.Clone(), GetLastWriteTime(entity.Path));
            lock (_requests)
            {
                _requests[entity.Path] = request;
            }
        }

        /// <summary>
        /// Add store thumbnail request to the queue. If there is a pending delete request,
        /// it won't be replaced.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void StoreThumbnail(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var thumbnail = entity.GetAttribute(ExifAttributeReaderFactory.Thumbnail);
            var thumbnailValue = thumbnail?.Value as ImageValue;
            if (thumbnailValue == null || thumbnailValue.IsNull)
            {
                return;
            }

            lock (_requests)
            {
                if (_requests.TryGetValue(entity.Path, out var req))
                {
                    if (req is DeleteRequest)
                    {
                        return; // storing thumbnail of a deleted file, this is no-op
                    }

                    if (req is StoreRequest store)
                    {
                        // update pending entity's thumbnail
                        store.Entity.SetAttribute(thumbnail);
                        return;
                    }
                }
                _requests[entity.Path] = new StoreThumbnailRequest(thumbnailValue.Value);
            }
        }

        public virtual void Delete(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            lock (_requests)
            {
                _requests[entity.Path] = new DeleteRequest();
            }
        }

        /// <summary>
        /// Change file path for all requests on <paramref name="entity"/> to <paramref name="newPath"/>.
        /// Note, there is no move request. Implementation is supposed to move the record on the spot.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newPath">
        /// New path of <paramref name="entity"/>. This function normalizes the path using
        /// <see cref="PathUtils.NormalizePath"/>
        /// </param>
        /// <returns>
        /// If this function returns true, <paramref name="entity"/> has been deleted.
        /// </returns>
        protected bool TryMove(IEntity entity, ref string newPath)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (newPath == null)
                throw new ArgumentNullException(nameof(newPath));

            newPath = PathUtils.NormalizePath(newPath);

            lock (_requests)
            {
                if (_requests.TryGetValue(entity.Path, out var req))
                {
                    if (req is DeleteRequest)
                    {
                        // the entity has been deleted, this is no-op
                        return true;
                    }

                    // apply the request to the entity at the new location
                    _requests.Remove(entity.Path);
                    _requests[newPath] = req;
                }
            }

            return false;
        }
    }

}
