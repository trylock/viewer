﻿using System;
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
            lock (_files)
            {
                if (_files.TryGetValue(path, out IEntity entity))
                {
                    return entity;
                }
            }

            return null;
        }

        public void Store(IEntity entity)
        {
            lock (_files)
            {
                _files[entity.Path] = entity.Clone();
            }
        }

        public void Remove(string path)
        {
            lock (_files)
            {
                _files.Remove(path);
            }
        }

        public void Move(string oldPath, string newPath)
        {
            lock (_files)
            {
                if (_files.TryGetValue(oldPath, out var entity))
                {
                    _files[newPath] = entity;
                    _files.Remove(oldPath);
                }
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
