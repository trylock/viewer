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
        
        public IReadableAttributeStorage CreateReader()
        {
            return new ReadableStorageProxy(this);
        }

        public IEntity Load(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            lock (_files)
            {
                if (_files.TryGetValue(path, out var entity))
                {
                    return entity;
                }
            }

            return null;
        }

        public void Store(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            lock (_files)
            {
                _files[entity.Path] = entity.Clone();
            }
        }

        public void StoreThumbnail(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            lock (_files)
            {
                if (!_files.TryGetValue(entity.Path, out var storedEntity))
                {
                    return;
                }

                var thumbnail = entity.GetAttribute("thumbnail");
                if (thumbnail == null)
                {
                    return;
                }
                storedEntity = storedEntity.SetAttribute(thumbnail);
                _files[storedEntity.Path] = storedEntity;
            }
        }

        public void Delete(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            lock (_files)
            {
                _files.Remove(entity.Path);
            }
        }

        public void Move(IEntity entity, string newPath)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (newPath == null)
                throw new ArgumentNullException(nameof(newPath));

            lock (_files)
            {
                _files[newPath] = entity;
                _files.Remove(entity.Path);
            }
        }

        public void Dispose()
        {
        }
    }
}
