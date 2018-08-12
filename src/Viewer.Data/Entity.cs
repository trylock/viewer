using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.IO;

namespace Viewer.Data
{
    public interface IEntity : IEnumerable<Attribute>
    {
        /// <summary>
        /// Path to the entity. It is unified using the <see cref="PathUtils.UnifyPath"/> function.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Number of attributes
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Find attribute in the collection.
        /// It is thread-safe.
        /// </summary>
        /// <param name="name">Name of the attribute</param>
        /// <returns>Found attribute or null if it does not exist</returns>
        Attribute GetAttribute(string name);

        /// <summary>
        /// Get value of an attribute named <paramref name="name"/>.
        /// It is thread-safe.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="name">Name of the attribute</param>
        /// <returns>Attribute value or null if there is no such attribute or its type is not T</returns>
        T GetValue<T>(string name) where T : BaseValue;

        /// <summary>
        /// Set attribute value.
        /// It is thread-safe.
        /// </summary>
        /// <param name="attr">Attribute to set</param>
        /// <returns>This Entity</returns>
        IEntity SetAttribute(Attribute attr);

        /// <summary>
        /// Remove attribute with given name.
        /// It won't trown an exception if there is no attribute with given name.
        /// It is thread-safe.
        /// </summary>
        /// <param name="name">Name of an attribute to remove.</param>
        /// <returns>This Entity</returns>
        IEntity RemoveAttribute(string name);

        /// <summary>
        /// Change path of the entity
        /// </summary>
        /// <param name="path">New path</param>
        /// <returns>This Entity</returns>
        IEntity ChangePath(string path);

        /// <summary>
        /// Copy this entity
        /// </summary>
        /// <returns>New copied entity</returns>
        IEntity Clone();
    }

    public sealed class DirectoryEntity : IEntity
    {
        public string Path { get; private set; }

        public int Count => 0;

        public DirectoryEntity(string path)
        {
            Path = PathUtils.UnifyPath(path);
        }

        public Attribute GetAttribute(string name)
        {
            return null;
        }

        public T GetValue<T>(string name) where T : BaseValue
        {
            return null;
        }

        public IEntity SetAttribute(Attribute attr)
        {
            throw new NotSupportedException();
        }

        public IEntity RemoveAttribute(string name)
        {
            throw new NotSupportedException();
        }

        public IEntity ChangePath(string path)
        {
            Path = PathUtils.UnifyPath(path);
            return this;
        }

        public IEntity Clone()
        {
            return new DirectoryEntity(Path);
        }

        public IEnumerator<Attribute> GetEnumerator()
        {
            return Enumerable.Empty<Attribute>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class FileEntity : IEntity
    {
        private readonly ReaderWriterLockSlim _attrsLock = new ReaderWriterLockSlim();
        private readonly Dictionary<string, Attribute> _attrs = new Dictionary<string, Attribute>();

        /// <summary>
        /// Path to the file where the attributes are (or will be) stored
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Last time these attributes were written to a file
        /// </summary>
        public DateTime LastWriteTime { get; }

        /// <summary>
        /// Last time these attributes were accessed (read from a file)
        /// </summary>
        public DateTime LastAccessTime { get; }
        
        /// <summary>
        /// Number or attributes in the collection
        /// </summary>
        public int Count => _attrs.Count;

        public FileEntity(string path, DateTime lastWriteTime, DateTime lastAccessTime)
        {
            Path = PathUtils.UnifyPath(path);
            LastWriteTime = lastWriteTime;
            LastAccessTime = lastAccessTime;
        }

        public FileEntity(string path) : this(path, DateTime.Now, DateTime.Now)
        {
        }
        
        public Attribute GetAttribute(string name)
        {
            _attrsLock.EnterReadLock();
            try
            {
                if (!_attrs.TryGetValue(name, out Attribute attr))
                {
                    return null;
                }

                return attr;
            }
            finally
            {
                _attrsLock.ExitReadLock();
            }
        }

        public T GetValue<T>(string name) where T : BaseValue
        {
            return GetAttribute(name)?.Value as T;
        }
        
        public IEntity SetAttribute(Attribute attr)
        {
            _attrsLock.EnterWriteLock();
            try
            {
                if (attr.Value.IsNull)
                {
                    _attrs.Remove(attr.Name);
                }
                else
                {
                    _attrs[attr.Name] = attr;
                }
            }
            finally
            {
                _attrsLock.ExitWriteLock();
            }

            return this;
        }
        
        public IEntity RemoveAttribute(string name)
        {
            _attrsLock.EnterWriteLock();
            try
            {
                _attrs.Remove(name);
            }
            finally
            {
                _attrsLock.ExitWriteLock();
            }

            return this;
        }

        public IEntity ChangePath(string path)
        {
            Path = PathUtils.UnifyPath(path);
            return this;
        }

        public IEntity Clone()
        {
            var clone = new FileEntity(Path, LastWriteTime, LastAccessTime);
            _attrsLock.EnterReadLock();
            try
            {
                foreach (var attr in _attrs)
                {
                    clone.SetAttribute(attr.Value);
                }
            }
            finally
            {
                _attrsLock.ExitReadLock();
            }

            return clone;
        }

        public IEnumerator<Attribute> GetEnumerator()
        {
            _attrsLock.EnterReadLock();
            try
            {
                return _attrs.Values.ToList().GetEnumerator();
            }
            finally
            {
                _attrsLock.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
