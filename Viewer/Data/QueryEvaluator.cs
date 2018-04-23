using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Storage;
using Viewer.IO;

namespace Viewer.Data
{
    public interface IQueryEvaluator
    {
        /// <summary>
        /// Create a new empty query
        /// </summary>
        /// <returns>New empty query</returns>
        Query CreateQuery();

        /// <summary>
        /// Execute given query
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <returns>Found entities</returns>
        IEntityManager Evaluate(Query query);
    }

    [Export(typeof(IQueryEvaluator))]
    public class QueryEvaluator : IQueryEvaluator
    {
        private readonly IFileSystem _fileSystem;
        private readonly IEntityRepository _modifiedEntities;
        private readonly IAttributeStorage _storage;

        [ImportingConstructor]
        public QueryEvaluator(IFileSystem fileSystem, IEntityRepository modifiedEntities, IAttributeStorage storage)
        {
            _storage = storage;
            _fileSystem = fileSystem;
            _modifiedEntities = modifiedEntities;
        }

        public Query CreateQuery()
        {
            return new Query();
        }

        public IEntityManager Evaluate(Query query)
        {
            var entities = new EntityManager(_modifiedEntities);
            var filter = CreateFilter(query);
            foreach (var file in EnumerateFiles(query.Pattern))
            {
                var entity = _storage.Load(file);
                if (filter(entity))
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        private Func<IEntity, bool> CreateFilter(Query query)
        {
            return entity =>
            {
                return query.Filter.All(condition => condition(entity));
            };
        }

        private IEnumerable<string> EnumerateFiles(string pattern)
        {
            if (pattern == null)
            {
                yield break;
            }

            var finder = new FileFinder(_fileSystem, pattern);
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
