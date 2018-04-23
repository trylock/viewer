using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Storage;
using Viewer.IO;

namespace Viewer.Data
{
    /// <inheritdoc />
    /// <summary>
    /// Immutable structure which describes a query
    /// </summary>
    public interface IQuery : IEnumerable<IEntity>
    {
        /// <summary>
        /// Create a new query with given directory path pattern
        /// </summary>
        /// <param name="pattern">Diretory path pattern <see cref="FileFinder"/></param>
        /// <returns>A new query with given directory path pattern</returns>
        IQuery Select(string pattern);

        /// <summary>
        /// Create a new query with additional condition.
        /// Entity will be included in the query result iff <paramref name="predicate"/> returns true
        /// </summary>
        /// <param name="predicate">Entity predicate</param>
        /// <returns>A new query with additional predicate</returns>
        IQuery Where(Func<IEntity, bool> predicate);
    }
    
    public class Query : IQuery
    {
        // query description
        private readonly string _pattern = "";
        private readonly ImmutableList<Func<IEntity, bool>> _filter = ImmutableList<Func<IEntity, bool>>.Empty;

        // dependencies
        private readonly IAttributeStorage _storage;
        private readonly IFileSystem _fileSystem;

        public Query(IAttributeStorage storage, IFileSystem fileSystem)
        {
            _storage = storage;
            _fileSystem = fileSystem;
        }

        private Query(IAttributeStorage storage, IFileSystem fileSystem, string pattern, ImmutableList<Func<IEntity, bool>> filter)
        {
            _storage = storage;
            _fileSystem = fileSystem;
            _pattern = pattern;
            _filter = filter;
        }

        public IEnumerator<IEntity> GetEnumerator()
        {
            var filter = CreateFilter();
            foreach (var file in EnumerateFiles())
            {
                var entity = _storage.Load(file);
                if (filter(entity))
                {
                    yield return entity;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IQuery Select(string newPattern)
        {
            return new Query(_storage, _fileSystem, newPattern, _filter);
        }

        public IQuery Where(Func<IEntity, bool> predicate)
        {
            return new Query(_storage, _fileSystem, _pattern, _filter.Add(predicate));
        }
        
        private Func<IEntity, bool> CreateFilter()
        {
            return entity =>
            {
                return _filter.All(condition => condition(entity));
            };
        }

        private IEnumerable<string> EnumerateFiles()
        {
            if (_pattern == null)
            {
                yield break;
            }

            var finder = new FileFinder(_fileSystem, _pattern);
            foreach (var path in finder.GetFiles())
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".jpeg" || extension == ".jpg")
                {
                    yield return path;
                }
            }
        }
    }
}
