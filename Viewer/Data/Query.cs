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
        /// Current directory path pattern
        /// </summary>
        string Pattern { get; }

        /// <summary>
        /// Create a new query with given directory path pattern
        /// </summary>
        /// <param name="pattern">Diretory path pattern <see cref="FileFinder"/></param>
        /// <returns>A new query with given directory path pattern</returns>
        IQuery Path(string pattern);
        
        /// <summary>
        /// Only include entities in the result if <paramref name="predicate"/> returns true
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
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
        public string Pattern { get; } = "";
        private Func<IEntity, bool> _includePredicate = entity => true;

        // dependencies
        private readonly IEntityManager _entities;
        private readonly IFileSystem _fileSystem;

        public Query(IEntityManager entities, IFileSystem fileSystem)
        {
            _entities = entities;
            _fileSystem = fileSystem;
        }

        private Query(IEntityManager entities, IFileSystem fileSystem, string pattern, Func<IEntity, bool> includePredicate)
        {
            _entities = entities;
            _fileSystem = fileSystem;
            Pattern = pattern;
            _includePredicate = includePredicate;
        }

        public IEnumerator<IEntity> GetEnumerator()
        {
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

                if (entity == null || !_includePredicate(entity))
                {
                    continue;
                }

                yield return entity;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IQuery Path(string newPattern)
        {
            return new Query(_entities, _fileSystem, newPattern, _includePredicate);
        }

        public IQuery Where(Func<IEntity, bool> predicate)
        {
            return new Query(_entities, _fileSystem, Pattern, entity => _includePredicate(entity) && predicate(entity));
        }

        private IEnumerable<string> EnumerateFiles()
        {
            if (Pattern == null)
            {
                return Enumerable.Empty<string>();
            }

            var finder = new FileFinder(_fileSystem, Pattern);
            var files = 
                from path in finder.GetFiles()
                let extension = System.IO.Path.GetExtension(path).ToLowerInvariant()
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

        [ImportingConstructor]
        public QueryFactory(IEntityManager entities, IFileSystem fileSystem)
        {
            _entities = entities;
            _fileSystem = fileSystem;
        }

        public IQuery CreateQuery()
        {
            return new Query(_entities, _fileSystem);
        }
    }
}
