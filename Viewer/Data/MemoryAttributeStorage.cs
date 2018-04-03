using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public class MemoryAttributeStorage : IAttributeStorage
    {
        private Dictionary<string, Entity> _files = new Dictionary<string, Entity>();

        public IEnumerable<Entity> Files
        {
            get
            {
                foreach (var pair in _files)
                {
                    yield return pair.Value;
                }
            }
        }

        public Entity Load(string path)
        {
            if (!_files.TryGetValue(path, out Entity collection))
            {
                return new Entity(path, DateTime.Now, DateTime.Now);
            }

            return collection;
        }

        public void Store(Entity attrs)
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
            if (!_files.TryGetValue(oldPath, out Entity entity))
            {
                return;
            }

            entity.Path = newPath;
            _files.Remove(oldPath);
            _files.Add(newPath, entity);
        }

        public void Flush()
        {
        }
    }
}
