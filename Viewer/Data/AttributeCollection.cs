using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public interface IAttributeCollectionFactory
    {
        /// <summary>
        /// Create an empty attribute collection from path to a file.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <returns>Attribute collection with filled metadata</returns>
        AttributeCollection CreateFromPath(string path);
    }

    public class AttributeCollectionFactory : IAttributeCollectionFactory
    {
        public AttributeCollection CreateFromPath(string path)
        {
            var fi = new FileInfo(path);
            return new AttributeCollection(path, fi.LastWriteTime, fi.LastAccessTime);
        }
    }

    public class AttributeCollection
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

        public AttributeCollection(string path, DateTime lastWriteTime, DateTime lastAccessTime)
        {
            Path = path;
            LastWriteTime = lastWriteTime;
            LastAccessTime = lastAccessTime;
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
    }
}
