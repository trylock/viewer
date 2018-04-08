using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.UI.Attributes;
using Attribute = Viewer.Data.Attribute;

namespace ViewerTest.UI.Attributes
{
    class AttributeManagerMock : IAttributeManager
    {
        private Dictionary<string, Attribute> _attrs = new Dictionary<string, Attribute>();
        private Dictionary<string, IEntity> _unsaved = new Dictionary<string, IEntity>();
        
        public void SetAttribute(string oldName, Attribute attr)
        {
            _attrs.Remove(oldName);
            _attrs[attr.Name] = attr;
        }

        public void RemoveAttribute(string name)
        {
            _attrs.Remove(name);
        }

        public IEnumerable<AttributeView> GetSelectedAttributes()
        {
            return _attrs.Values.Select(attr => new AttributeView
            {
                Data = attr,
                IsMixed = false
            });
        }

        public IReadOnlyList<IEntity> ConsumeChanged()
        {
            var changed = _unsaved.Values.ToList();
            _unsaved.Clear();
            return changed;
        }
    }
}
