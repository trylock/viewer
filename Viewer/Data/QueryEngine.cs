using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Storage;
using Viewer.IO;

namespace Viewer.Data
{
    public interface IQueryEngine
    {
        /// <summary>
        /// Create an empty query
        /// </summary>
        /// <returns>Empty query</returns>
        IQuery CreateQuery();

        /// <summary>
        /// Create an empty entity manager
        /// </summary>
        /// <returns>Empty entity manager</returns>
        IEntityManager CreateEntityManager();
    }

    [Export(typeof(IQueryEngine))]
    public class QueryEngine : IQueryEngine
    {
        private readonly IFileSystem _fileSystem;
        private readonly IAttributeStorage _storage;
        private readonly IEntityRepository _modifiedEntities;

        [ImportingConstructor]
        public QueryEngine(IFileSystem fileSystem, IAttributeStorage storage, IEntityRepository modifiedEntities)
        {
            _fileSystem = fileSystem;
            _storage = storage;
            _modifiedEntities = modifiedEntities;
        }

        public IQuery CreateQuery()
        {
            return new Query(_storage, _fileSystem);
        }

        public IEntityManager CreateEntityManager()
        {
            return new EntityManager(_modifiedEntities);
        }
    }
}
