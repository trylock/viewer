using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Query;

namespace Viewer.UI
{
    public class QueryEventArgs : EventArgs
    {
        public IQuery Query { get; }

        public QueryEventArgs(IQuery query)
        {
            Query = query;
        }
    }

    public class EntityEventArgs : EventArgs
    {
        /// <summary>
        /// Loaded entities
        /// </summary>
        public IEnumerable<IEntity> Entities { get; }

        /// <summary>
        /// Index of selected entity
        /// </summary>
        public int Index { get; }

        public EntityEventArgs(IEnumerable<IEntity> entities, int index)
        {
            Entities = entities;
            Index = index;
        }
    }

    public interface IApplicationState
    {
        /// <summary>
        /// Event called when user tries to execute a query.
        /// </summary>
        event EventHandler<QueryEventArgs> QueryExecuted;
        
        /// <summary>
        /// Event called when user tries to open an entity (i.e. to open presentation)
        /// </summary>
        event EventHandler<EntityEventArgs> EntityOpened;

        /// <summary>
        /// Open entity 
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="index"></param>
        void OpenEntity(IEnumerable<IEntity> entities, int index);

        /// <summary>
        /// Execute a query
        /// </summary>
        /// <param name="query">Query to execute</param>
        void ExecuteQuery(IQuery query);
    }

    [Export(typeof(IApplicationState))]
    public class ApplicationState : IApplicationState
    {
        public event EventHandler<QueryEventArgs> QueryExecuted;
        public event EventHandler<EntityEventArgs> EntityOpened;

        public void OpenEntity(IEnumerable<IEntity> entities, int index)
        {
            if (index < 0 || index >= entities.Count())
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            EntityOpened?.Invoke(this, new EntityEventArgs(entities, index));
        }

        public void ExecuteQuery(IQuery query)
        {
            QueryExecuted?.Invoke(this, new QueryEventArgs(query));
        }
    }
}
