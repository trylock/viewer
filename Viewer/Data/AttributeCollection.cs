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
    public class AttributeCollection : IDictionary<string, Attribute>
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

        public AttributeCollection(string path, DateTime lastWriteTime, DateTime lastAccessTime)
        {
            Path = path;
            LastWriteTime = lastWriteTime;
            LastAccessTime = lastAccessTime;
        }

        public AttributeCollection(string path) 
        {
            var fi = new FileInfo(path);
            Path = path;
            LastWriteTime = fi.LastWriteTime;
            LastAccessTime = fi.LastAccessTime;
        }

        /// <summary>
        /// Check whether the attributes are valid (same as in their file)
        /// </summary>
        /// <returns>true iff the attributes are valid</returns>
        public bool CheckValidity()
        {
            var fi = new FileInfo(Path);
            return fi.LastWriteTime != LastWriteTime;
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
        /// Add a new attribute or set an existing attribute
        /// </summary>
        /// <param name="attr"></param>
        public void Add(Attribute attr)
        {
            SetAttribute(attr);
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
    }
}
