﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public interface IEntity : IEnumerable<Attribute>
    {
        /// <summary>
        /// Path to the entity
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Number of attributes
        /// </summary>
        int Count { get; }

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
        IEntity SetAttribute(Attribute attr);

        /// <summary>
        /// Remove attribute with given name.
        /// It won't trown an exception if there is no attribute with given name
        /// </summary>
        /// <param name="name">Name of an attribute to remove.</param>
        IEntity RemoveAttribute(string name);

        /// <summary>
        /// Change path of the entity
        /// </summary>
        /// <param name="path">New path</param>
        /// <returns></returns>
        IEntity ChangePath(string path);

        /// <summary>
        /// Copy this entity
        /// </summary>
        /// <returns>New copied entity</returns>
        IEntity Clone();
    }

    public class Entity : IEntity
    {
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
        
        /// <summary>
        /// Collection of attribute names
        /// </summary>
        public IEnumerable<string> Keys => _attrs.Keys;

        /// <summary>
        /// Collection of attributes
        /// </summary>
        public IEnumerable<Attribute> Values => _attrs.Values;

        public Entity(string path, DateTime lastWriteTime, DateTime lastAccessTime)
        {
            Path = path;
            LastWriteTime = lastWriteTime;
            LastAccessTime = lastAccessTime;
        }

        public Entity(string path) : this(path, DateTime.Now, DateTime.Now)
        {
        }
        
        public Attribute GetAttribute(string name)
        {
            if (!_attrs.TryGetValue(name, out Attribute attr))
            {
                return null;
            }
            return attr;
        }
        
        public IEntity SetAttribute(Attribute attr)
        {
            _attrs[attr.Name] = attr;
            return this;
        }
        
        public IEntity RemoveAttribute(string name)
        {
            _attrs.Remove(name);
            return this;
        }

        public IEntity ChangePath(string path)
        {
            Path = path;
            return this;
        }

        public IEntity Clone()
        {
            var clone = new Entity(Path, LastWriteTime, LastAccessTime);
            foreach (var attr in _attrs)
            {
                clone.SetAttribute(attr.Value);
            }

            return clone;
        }

        public IEnumerator<Attribute> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}