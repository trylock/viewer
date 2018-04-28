using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats;
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.UI.Log;

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

    public interface IQueryFactory
    {
        /// <summary>
        /// Create an empty query
        /// </summary>
        /// <returns>Empty query</returns>
        IQuery CreateQuery();
    }
    
    public class Query : IQuery
    {
        // query description
        private readonly string _pattern = "";
        private readonly ImmutableList<Func<IEntity, bool>> _filter = ImmutableList<Func<IEntity, bool>>.Empty;

        // dependencies
        private readonly IEntityManager _entities;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _log;

        public Query(IEntityManager entities, IFileSystem fileSystem, ILogger log)
        {
            _entities = entities;
            _fileSystem = fileSystem;
            _log = log;
        }

        private Query(IEntityManager entities, IFileSystem fileSystem, ILogger log, string pattern, ImmutableList<Func<IEntity, bool>> filter)
        {
            _entities = entities;
            _fileSystem = fileSystem;
            _log = log;
            _pattern = pattern;
            _filter = filter;
        }

        public IEnumerator<IEntity> GetEnumerator()
        {
            var filter = CreateFilter();
            foreach (var file in EnumerateFiles())
            {
                IEntity entity = null;
                try
                {
                    entity = _entities.GetEntity(file);
                }
                catch (InvalidDataFormatException)
                {
                }

                if (entity == null)
                {
                    continue;
                }
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
            return new Query(_entities, _fileSystem, _log, newPattern, _filter);
        }

        public IQuery Where(Func<IEntity, bool> predicate)
        {
            return new Query(_entities, _fileSystem, _log, _pattern, _filter.Add(predicate));
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
                return Enumerable.Empty<string>();
            }

            var finder = new FileFinder(_fileSystem, _pattern);
            var files = 
                from path in finder.GetFiles()
                let extension = Path.GetExtension(path).ToLowerInvariant()
                where extension == ".jpeg" || extension == ".jpg"
                select path;
            return files;
        }
    }

    [Export(typeof(IQueryFactory))]
    public class QueryFactory : IQueryFactory
    {
        private readonly IEntityManager _entities;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _log;

        [ImportingConstructor]
        public QueryFactory(IEntityManager entities, IFileSystem fileSystem, ILogger log)
        {
            _entities = entities;
            _fileSystem = fileSystem;
            _log = log;
        }

        public IQuery CreateQuery()
        {
            return new Query(_entities, _fileSystem, _log);
        }
    }
}
