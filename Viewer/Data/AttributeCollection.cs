using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public class AttributeCollection : ICollection<Attribute>
    {
        private Dictionary<string, Attribute> _attrs = new Dictionary<string, Attribute>();

        /// <summary>
        /// Path to the file where the attributes are (or will be) stored
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Last time these attributes were written to a file
        /// </summary>
        public DateTime LastWriteTime { get; }

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

        /// <summary>
        /// Remove all attributes in the collection
        /// </summary>
        public void Clear()
        {
            _attrs.Clear();
            IsDirty = true;
        }

        /// <summary>
        /// Check whether there is an attribute
        /// </summary>
        /// <param name="attr"></param>
        /// <returns>true iff given attribute is in the collection</returns>
        public bool Contains(Attribute attr)
        {
            if (!_attrs.TryGetValue(attr.Name, out Attribute attrInCollection))
            {
                return false;
            }

            return attr == attrInCollection;
        }

        /// <summary>
        /// Copy the attributes to an array
        /// </summary>
        /// <param name="array">Array of attributes where the attributes will be copied</param>
        /// <param name="arrayIndex">Index in the array at which copying begins</param>
        public void CopyTo(Attribute[] array, int arrayIndex)
        {
            _attrs.Values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Remove attribute with the same name and value
        /// </summary>
        /// <param name="item">Attribute</param>
        /// <returns>true iff the attribute has been removed</returns>
        public bool Remove(Attribute item)
        {
            if (!_attrs.TryGetValue(item.Name, out Attribute attrInCollection))
            {
                return false;
            }

            if (item != attrInCollection)
            {
                return false;
            }

            IsDirty = true;
            _attrs.Remove(item.Name);
            return true;
        }

        public IEnumerator<Attribute> GetEnumerator()
        {
            foreach (var pair in _attrs)
            {
                yield return pair.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
