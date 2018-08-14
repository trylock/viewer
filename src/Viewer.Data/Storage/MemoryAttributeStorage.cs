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

        public void Store(IEntity entity)
        {
            lock (_files)
            {
                _files[entity.Path] = entity.Clone();
            }
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

        public IReadOnlyList<IEntity> Consume()
        {
            lock (_files)
            {
                var items = _files.Values.ToArray();
                _files.Clear();
                return items;
            }
        }
    }
}
