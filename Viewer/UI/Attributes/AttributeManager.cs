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
                    if (attr.Flags.HasFlag(AttributeFlags.ReadOnly))
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
                var entity = _entities.GetEntity(item);
                var updated = entity.RemoveAttribute(oldName).SetAttribute(attr);
                _entities.Stage(updated);
            }
        }

        public void RemoveAttribute(string name)
        {
            foreach (var item in _selection)
            {
                var entity = _entities.GetEntity(item);
                var updated = entity.RemoveAttribute(name);
                _entities.Stage(updated);
            }
        }
    }
}
