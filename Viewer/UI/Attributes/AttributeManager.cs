using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    /// <summary>
    /// Attribute manager manages attributes of entities in selection
    /// </summary>
    public interface IAttributeManager
    {
        /// <summary>
        /// Collection of unsaved entities.
        /// An entity is put into this collection if the user sets an attribute or 
        /// deletes an attribute on an entity in selection.
        /// </summary>
        ICollection<IEntity> Unsaved { get; }

        /// <summary>
        /// Set attribute to all entities in selection
        /// </summary>
        /// <param name="oldName">Old name of the attribute</param>
        /// <param name="attr">Attribute</param>
        void SetAttribute(string oldName, Attribute attr);

        /// <summary>
        /// Remove attribute named <paramref name="name"/> from all entities in selection.
        /// </summary>
        /// <param name="name">Name of an attribute to remove</param>
        void RemoveAttribute(string name);

        /// <summary>
        /// Find selected attributes.
        /// </summary>
        /// <returns></returns>
        IEnumerable<AttributeView> GetSelectedAttributes();
    }

    public class AttributeManager : IAttributeManager
    {
        private IEntityManager _entities;
        private ISelection _selection;
        private Dictionary<string, IEntity> _changed = new Dictionary<string, IEntity>();
        
        public ICollection<IEntity> Unsaved => _changed.Values;

        public AttributeManager(IEntityManager entities, ISelection selection)
        {
            _entities = entities;
            _selection = selection;
        }

        public IEnumerable<AttributeView> GetSelectedAttributes()
        {
            // find all attributes in the selection
            var attrs = new Dictionary<string, AttributeView>();
            foreach (var item in _selection)
            {
                var entity = _entities.GetEntity(item);
                foreach (var attr in entity)
                {
                    if (attr.Source != AttributeSource.Custom)
                        continue;

                    if (attrs.TryGetValue(attr.Name, out AttributeView attrView))
                    {
                        if (attrView.Data.Equals(attr))
                        {
                            continue; // both entities have the same attribute
                        }
                        attrView.IsMixed = true;
                    }
                    else
                    {
                        attrs.Add(attr.Name, new AttributeView
                        {
                            Data = attr,
                            IsMixed = false
                        });
                    }
                }
            }

            return attrs.Values;
        }

        public void SetAttribute(string oldName, Attribute attr)
        {
            foreach (var item in _selection)
            {
                var updated = _entities.GetEntity(item)
                    .RemoveAttribute(oldName)
                    .SetAttribute(attr);
                _entities.SetEntity(updated);
                _changed[updated.Path] = updated;
            }
        }

        public void RemoveAttribute(string name)
        {
            foreach (var item in _selection)
            {
                var updated = _entities.GetEntity(item).RemoveAttribute(name);
                _entities.SetEntity(updated);
                _changed[updated.Path] = updated;
            }
        }
    }
}
