using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI
{
    public class EntityListEventArgs : EventArgs
    {
        /// <summary>
        /// Loaded entities
        /// </summary>
        public IEntityManager Entities { get; }

        public EntityListEventArgs(IEntityManager entities)
        {
            Entities = entities;
        }
    }

    public class EntityEventArgs : EventArgs
    {
        /// <summary>
        /// Loaded entities
        /// </summary>
        public IEntityManager Entities { get; }

        /// <summary>
        /// Index of selected entity
        /// </summary>
        public int Index { get; }

        public EntityEventArgs(IEntityManager entities, int index)
        {
            Entities = entities;
            Index = index;
        }
    }

    public interface IApplicationState
    {
        /// <summary>
        /// Event called when application tries to open a list of entities (i.e. a query result)
        /// </summary>
        event EventHandler<EntityListEventArgs> EntitiesOpened;

        /// <summary>
        /// Event called when user tries to open an entity (i.e. to open presentation)
        /// </summary>
        event EventHandler<EntityEventArgs> EntityOpened;

        /// <summary>
        /// Open entity 
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="index"></param>
        void OpenEntity(IEntityManager entities, int index);

        /// <summary>
        /// Open list of entities in a component
        /// </summary>
        /// <param name="entities">List of entities</param>
        void OpenEntities(IEntityManager entities);
    }

    [Export(typeof(IApplicationState))]
    public class ApplicationState : IApplicationState
    {
        public event EventHandler<EntityListEventArgs> EntitiesOpened;
        public event EventHandler<EntityEventArgs> EntityOpened;

        public void OpenEntity(IEntityManager entities, int index)
        {
            EntityOpened?.Invoke(this, new EntityEventArgs(entities, index));
        }

        public void OpenEntities(IEntityManager entities)
        {
            EntitiesOpened?.Invoke(this, new EntityListEventArgs(entities));
        }
    }
}
