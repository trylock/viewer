using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    /// <summary>
    /// Cached attribute storage uses 2 attribute storage implementations_
    /// faster cache and slower main storage.
    /// </summary>
    /// <remarks>
    ///     Unlike other implementations, users have to call the Store method after reading 
    ///     to store attributes in cache. If the attributes haven't been changed, they won't 
    ///     be stored in the main storage just in the cache.
    /// </remarks>
    public class CachedAttributeStorage : IAttributeStorage
    {
        private IAttributeStorage _mainStorage;
        private IAttributeStorage _cacheStorage;
        private Dictionary<string, Entity> _pending = new Dictionary<string, Entity>();

        /// <summary>
        /// Create cached attribute storage.
        /// </summary>
        /// <param name="mainStorage">Main attribute storage</param>
        /// <param name="cacheStorage">
        ///     Cache attribute storage. 
        ///     This should be faster than the main storage
        /// </param>
        public CachedAttributeStorage(
            IAttributeStorage mainStorage, 
            IAttributeStorage cacheStorage)
        {
            _mainStorage = mainStorage;
            _cacheStorage = cacheStorage;
        }

        /// <summary>
        /// Load attributes from cache and then from file.
        /// Main storage will be used iff entries in the cache are not valid.
        /// </summary>
        /// <param name="path">Path to a file with attributes</param>
        /// <returns>Collection of attributes in the file</returns>
        public Entity Load(string path)
        {
            var attrs = _cacheStorage.Load(path);
            if (attrs == null)
            {
                attrs = _mainStorage.Load(path);
                AddPendingWrite(attrs);
            }

            return attrs;
        }

        /// <summary>
        /// Store the attributes to both cache and the main storage.
        /// If the attributes are not dirty, they won't be written to the main storage.
        /// </summary>
        /// <param name="attrs">Attributes to store</param>
        public void Store(Entity attrs)
        {
            // only write the attributes to the main storage if it's necessary
            if (attrs.IsDirty)
            {
                _mainStorage.Store(attrs);
                attrs.Reset();
            }

            _cacheStorage.Store(attrs);
        }
        
        public void Remove(string path)
        {
            _mainStorage.Remove(path);
            _cacheStorage.Remove(path);
        }

        public void Move(string oldPath, string newPath)
        {
            _mainStorage.Move(oldPath, newPath);
            _cacheStorage.Move(oldPath, newPath);
        }

        /// <summary>
        /// All pending attributes are written to the cache.
        /// </summary>
        public void Flush()
        {
            foreach (var pair in _pending)
            {
                var attrs = pair.Value;

                // store the attributes in cache
                _cacheStorage.Store(attrs);
            }
            _pending.Clear();
        }

        private void AddPendingWrite(Entity attrs)
        {
            if (_pending.ContainsKey(attrs.Path))
            {
                _pending[attrs.Path] = attrs;
            }
            else
            {
                _pending.Add(attrs.Path, attrs);
            }
        }
    }
}
