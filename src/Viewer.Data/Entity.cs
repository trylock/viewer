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
    /// <inheritdoc />
    /// <summary>
    /// Collection of attributes. It represents a file on a disk.
    /// </summary>
    public interface IEntity : IEnumerable<Attribute>
    {
        /// <summary>
        /// Path to the entity. It is unified using the <see cref="PathUtils.NormalizePath"/> function.
        /// </summary>
        string Path { get; }

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
        /// <returns>Modified entity</returns>
        IEntity SetAttribute(Attribute attr);

        /// <summary>
        /// Remove attribute with given name.
        /// It won't trown an exception if there is no attribute with given name.
        /// It is thread-safe.
        /// </summary>
        /// <param name="name">Name of an attribute to remove.</param>
        /// <returns>Modified entity</returns>
        IEntity RemoveAttribute(string name);

        /// <summary>
        /// Change path of the entity
        /// </summary>
        /// <param name="path">New path</param>
        /// <returns>Modified entity</returns>
        IEntity ChangePath(string path);

        /// <summary>
        /// Create copy of this entity.
        /// </summary>
        /// <returns>Cloned entity</returns>
        IEntity Clone();

        /// <summary>
        /// Remove all attributes of this entity and copy attributes from <paramref name="entity"/>
        /// </summary>
        /// <param name="entity">Entity which will be copied</param>
        /// <returns>This entity</returns>
        IEntity Set(IEntity entity);
    }

    /// <inheritdoc />
    /// <summary>
    /// An entity which represents a folder in file system.
    /// </summary>
    public sealed class DirectoryEntity : IEntity
    {
        /// <summary>
        /// Name of the attribute which returns path to the parent directory
        /// </summary>
        public const string DirectoryName = "Directory";

        public string Path { get; private set; }

        public DirectoryEntity(string path)
        {
            Path = PathUtils.NormalizePath(path);
        }

        public Attribute GetAttribute(string name)
        {
            if (name == DirectoryName)
            {
                var parent = System.IO.Path.GetDirectoryName(Path) ?? Path;
                var parentName = PathUtils.GetLastPart(parent);
                return new Attribute(
                    name,
                    new StringValue(parentName), 
                    AttributeSource.Metadata);
            }
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
            return new DirectoryEntity(Path);
        }

        public IEntity Clone()
        {
            return new DirectoryEntity(Path);
        }

        public IEntity Set(IEntity entity)
        {
            return this;
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
        private readonly Dictionary<string, Attribute> _attrs = new Dictionary<string, Attribute>();

        public string Path { get; private set; }

        public FileEntity(string path)
        {
            Path = PathUtils.NormalizePath(path);
        }
        
        public Attribute GetAttribute(string name)
        {
            lock (_attrs)
            {
                return _attrs.TryGetValue(name, out var entity) ? entity : null;
            }
        }

        public T GetValue<T>(string name) where T : BaseValue
        {
            return GetAttribute(name)?.Value as T;
        }
        
        public IEntity SetAttribute(Attribute attr)
        {
            if (attr.Value.IsNull)
            {
                return RemoveAttribute(attr.Name);
            }

            lock (_attrs)
            {
                _attrs[attr.Name] = attr;
            }
            return this;
        }
        
        public IEntity RemoveAttribute(string name)
        {
            lock (_attrs)
            {
                _attrs.Remove(name);
            }
            return this;
        }

        public IEntity ChangePath(string path)
        {
            Path = PathUtils.NormalizePath(path);
            return this;
        }
        
        public IEnumerator<Attribute> GetEnumerator()
        {
            lock (_attrs)
            {
                IEnumerable<Attribute> attrs = _attrs.Values.ToArray();
                return attrs.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEntity Clone()
        {
            var copy = new FileEntity(Path);
            lock (_attrs)
            {
                foreach (var attr in _attrs.Values)
                {
                    copy.SetAttribute(attr);
                }
            }

            return copy;
        }

        public IEntity Set(IEntity entity)
        {
            lock (_attrs)
            {
                _attrs.Clear();
                foreach (var attr in entity)
                {
                    _attrs.Add(attr.Name, attr);
                }
            }

            return this;
        }
    }
}
