using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Storage;

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
        private readonly IEntityRepository _modifiedEntities;
        private readonly IAttributeStorage _storage;

        [ImportingConstructor]
        public QueryEvaluator(IEntityRepository modifiedEntities, IAttributeStorage storage)
        {
            _modifiedEntities = modifiedEntities;
            _storage = storage;
        }

        public Query CreateQuery()
        {
            return new Query();
        }

        public IEntityManager Evaluate(Query query)
        {
            var entities = new EntityManager(_modifiedEntities);
            foreach (var file in EnumerateFiles(query.SelectPattern))
            {
                var entity = _storage.Load(file);
                entities.Add(entity);
            }

            return entities;
        }

        private IEnumerable<string> EnumerateFiles(string pattern)
        {
            if (pattern == null)
            {
                yield break;
            }
            
            foreach (var path in Directory.EnumerateFiles(pattern))
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
