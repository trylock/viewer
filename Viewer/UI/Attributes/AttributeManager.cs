using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    public class AttributeGroup
    {
        /// <summary>
        /// Attribute value. 
        /// This can be arbitrary if there are more attribute values in the group.
        /// </summary>
        public Attribute Data { get; set; }

        /// <summary>
        /// true iff value of this attribute is mixed.
        /// Mixed value means there are 2 entities with the same attribute name but they have different value or type.
        /// </summary>
        public bool IsMixed { get; set; }

        /// <summary>
        /// true iff all entities in the group have an attribute with this name
        /// </summary>
        public bool IsGlobal { get; set; }

        /// <summary>
        /// Number of entities with an attribute with the same name
        /// </summary>
        public int EntityCount { get; set; }
    }

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
        IEnumerable<AttributeGroup> GroupAttributesInSelection();
    }
    
    [Export(typeof(IAttributeManager))]
    public class AttributeManager : IAttributeManager
    {
        private readonly ISelection _selection;
        private readonly IEntityRepository _modified;

        [ImportingConstructor]
        public AttributeManager(ISelection selection, IEntityRepository modified)
        {
            _selection = selection;
            _modified = modified;
        }

        public IEnumerable<AttributeGroup> GroupAttributesInSelection()
        {
            // find all attributes in the selection
            var attrs = new Dictionary<string, AttributeGroup>();
            foreach (var entity in _selection)
            {
                foreach (var attr in entity)
                {
                    if (attrs.TryGetValue(attr.Name, out AttributeGroup attrView))
                    {
                        ++attrView.EntityCount;
                        if (attrView.Data.Equals(attr))
                        {
                            continue; // both entities have the same attribute
                        }
                        attrView.IsMixed = true;
                    }
                    else
                    {
                        attrs.Add(attr.Name, new AttributeGroup
                        {
                            Data = attr,
                            IsMixed = false,
                            EntityCount = 1
                        });
                    }
                }
            }

            foreach (var pair in attrs)
            {
                pair.Value.IsGlobal = pair.Value.EntityCount == _selection.Count;
            }

            return attrs.Values;
        }
        
        public void SetAttribute(string oldName, Attribute attr)
        {
            foreach (var entity in _selection)
            {
                var updated = entity.RemoveAttribute(oldName).SetAttribute(attr);
                _modified.Add(updated);
            }
        }

        public void RemoveAttribute(string name)
        {
            foreach (var entity in _selection)
            { 
                var updated = entity.RemoveAttribute(name);
                _modified.Add(updated);  
            }
        }
    }
}
