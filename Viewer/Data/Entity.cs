using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public interface IEntity : IEnumerable<Attribute>, IDisposable
    {
        /// <summary>
        /// Path to the entity
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// true iff there are some unsaved changes (i.g. the entity should be written back to a file)
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Find attribute in the collection
        /// </summary>
        /// <param name="name">Name of the attribute</param>
        /// <returns>Found attribute or null if it does not exist</returns>
        Attribute GetAttribute(string name);

        /// <summary>
        /// Set attribute value
        /// </summary>
        /// <param name="attr">Attribute to set</param>
        void SetAttribute(Attribute attr);

        /// <summary>
        /// Remove attribute with given name.
        /// It won't trown an exception if there is no attribute with given name
        /// </summary>
        /// <param name="name">Name of an attribute to remove.</param>
        void RemoveAttribute(string name);

        /// <summary>
        /// Reset the dirty flag
        /// </summary>
        void ResetDirty();

        /// <summary>
        /// Set the dirty flag
        /// </summary>
        void SetDirty();
    }

    public class Entity : IEntity, IDictionary<string, Attribute>
    {
        private IDictionary<string, Attribute> _attrs = new Dictionary<string, Attribute>();

        /// <summary>
        /// Path to the file where the attributes are (or will be) stored
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Last time these attributes were written to a file
        /// </summary>
        public DateTime LastWriteTime { get; private set; }

        /// <summary>
        /// Last time these attributes were accessed (read from a file)
        /// </summary>
        public DateTime LastAccessTime { get; }

        /// <summary>
        /// true iff the attributes have changed and it is neccessary to write 
        ///          them back to a file
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Number or attributes in the collection
        /// </summary>
        public int Count => _attrs.Count;

        /// <summary>
        /// false, the collection is never read-only
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Collection of attribute names
        /// </summary>
        public ICollection<string> Keys => _attrs.Keys;

        /// <summary>
        /// Collection of attributes
        /// </summary>
        public ICollection<Attribute> Values => _attrs.Values;

        public Entity(string path, DateTime lastWriteTime, DateTime lastAccessTime)
        {
            Path = path;
            LastWriteTime = lastWriteTime;
            LastAccessTime = lastAccessTime;
        }

        public Entity(string path) 
        {
            Path = path;
            LastWriteTime = DateTime.Now;
            LastAccessTime = DateTime.Now;
        }
        
        /// <summary>
        /// Find attribute in the collection
        /// </summary>
        /// <param name="name">Name of the attribute</param>
        /// <returns>Found attribute or null if it does not exist</returns>
        public Attribute GetAttribute(string name)
        {
            if (!_attrs.TryGetValue(name, out Attribute attr))
            {
                return null;
            }
            return attr;
        }

        /// <summary>
        /// Set attribute value
        /// </summary>
        /// <param name="attr">Attribute to set</param>
        public void SetAttribute(Attribute attr)
        {
            IsDirty = true;
            if (_attrs.ContainsKey(attr.Name))
            {
                _attrs[attr.Name] = attr;
            }
            else
            {
                _attrs.Add(attr.Name, attr);
            }
        }

        /// <summary>
        /// Remove attribute
        /// </summary>
        /// <param name="name">Name of an attribute to remove</param>
        public void RemoveAttribute(string name)
        {
            _attrs.Remove(name);
        }

        /// <summary>
        /// Call this after the collection has been stored in a file.
        /// </summary>
        public void ResetDirty()
        {
            IsDirty = false;
            LastWriteTime = DateTime.Now;
        }

        /// <summary>
        /// Make this entity dirty
        /// </summary>
        public void SetDirty()
        {
            IsDirty = true;
        }

        #region IDictionary
        
        public void Clear()
        {
            if (Count != 0)
            {
                IsDirty = true;
            }
            _attrs.Clear();
        }

        void ICollection<KeyValuePair<string, Attribute>>.Add(KeyValuePair<string, Attribute> item)
        {
            IsDirty = true;
            _attrs.Add(item);
        }

        bool ICollection<KeyValuePair<string, Attribute>>.Contains(KeyValuePair<string, Attribute> item)
        {
            return _attrs.Contains(item);
        }

        void ICollection<KeyValuePair<string, Attribute>>.CopyTo(KeyValuePair<string, Attribute>[] array, int arrayIndex)
        {
            _attrs.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, Attribute>>.Remove(KeyValuePair<string, Attribute> item)
        {
            if (_attrs.Remove(item))
            {
                IsDirty = true;
                return true;
            }

            return false;
        }

        IEnumerator<Attribute> IEnumerable<Attribute>.GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, Attribute>> GetEnumerator()
        {
            return _attrs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(string key)
        {
            return _attrs.ContainsKey(key);
        }

        public void Add(string key, Attribute value)
        {
            IsDirty = true;
            _attrs.Add(key, value);
        }

        public bool Remove(string key)
        {
            if (_attrs.Remove(key))
            {
                IsDirty = true;
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out Attribute value)
        {
            return _attrs.TryGetValue(key, out value);
        }

        public Attribute this[string key]
        {
            get => _attrs[key];
            set
            {
                IsDirty = true;
                _attrs[key] = value;
            }
        }

        #endregion

        public void Dispose()
        {
            foreach (var attr in _attrs)
            {
                attr.Value.Dispose();
            }
        }
    }
}
