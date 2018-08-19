using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Storage
{
    [Export(typeof(MemoryAttributeStorage))]
    public class MemoryAttributeStorage : IAttributeStorage
    {
        private readonly Dictionary<string, IEntity> _files = new Dictionary<string, IEntity>();
        
        public LoadResult Load(string path)
        {
            lock (_files)
            {
                if (_files.TryGetValue(path, out var entity))
                {
                    return new LoadResult(entity, 0);
                }
            }

            return new LoadResult(null, 0);
        }

        public void Store(IEntity entity, StoreFlags flags)
        {
            lock (_files)
            {
                // add an empty entity if it's not in the storage
                if (!_files.TryGetValue(entity.Path, out var storedEntity))
                {
                    storedEntity = new FileEntity(entity.Path);
                    _files.Add(storedEntity.Path, storedEntity);
                }
                
                // update attributes in the flags value
                var attributes = entity.Where(CreateAttributePredicate(flags));
                foreach (var attr in attributes)
                {
                    storedEntity.SetAttribute(attr);
                }
            }
        }

        private static Func<Attribute, bool> CreateAttributePredicate(StoreFlags flags)
        {
            return attr => (flags.HasFlag(StoreFlags.Metadata) && attr.Source == AttributeSource.Metadata) ||
                           (flags.HasFlag(StoreFlags.Attribute) && attr.Source == AttributeSource.Custom);
        }

        public void Remove(IEntity entity)
        {
            lock (_files)
            {
                _files.Remove(entity.Path);
            }
        }

        public void Move(IEntity entity, string newPath)
        {
            lock (_files)
            {
                _files[newPath] = entity;
                _files.Remove(entity.Path);
            }
        }
    }
}
