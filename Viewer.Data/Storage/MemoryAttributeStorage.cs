using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Storage
{
    public class MemoryAttributeStorage : IAttributeStorage
    {
        private Dictionary<string, IEntity> _files = new Dictionary<string, IEntity>();

        public IEnumerable<IEntity> Files
        {
            get
            {
                foreach (var pair in _files)
                {
                    yield return pair.Value;
                }
            }
        }

        public IEntity Load(string path)
        {
            if (!_files.TryGetValue(path, out IEntity collection))
            {
                return null;
            }

            return collection;
        }

        public void Store(IEntity attrs)
        {
            if (_files.ContainsKey(attrs.Path))
            {
                _files[attrs.Path] = attrs;
            }
            else
            {
                _files.Add(attrs.Path, attrs);
            }
        }

        public void Remove(string path)
        {
            _files.Remove(path);
        }

        public void Move(string oldPath, string newPath)
        {
            if (!_files.TryGetValue(oldPath, out IEntity entity))
            {
                return;
            }

            entity = entity.ChangePath(newPath);
            _files.Remove(oldPath);
            _files.Add(newPath, entity);
        }
        
        public void Add(IEntity entity)
        {
            _files.Add(entity.Path, entity);
        }
    }
}
