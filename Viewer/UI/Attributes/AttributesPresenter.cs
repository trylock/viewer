using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    public class AttributesPresenter
    {
        private IAttributeView _attrView;
        private ISelection _selection;
        private IEntityManager _entities;
        private IProgressViewFactory _progressViewFactory;

        public AttributesPresenter(IAttributeView attrView, IProgressViewFactory progressViewFactory, ISelection selection, IEntityManager entities)
        {
            _entities = entities;
            _selection = selection;
            _selection.Changed += Selection_Changed;

            _progressViewFactory = progressViewFactory;

            _attrView = attrView;
            _attrView.EditingEnabled = false;
            PresenterUtils.SubscribeTo(_attrView, this, "View");
        }
        
        private void Selection_Changed(object sender, EventArgs eventArgs)
        {
            // find all attributes in the selection
            var views = new Dictionary<string, AttributeView>();
            foreach (var entity in _selection)
            {
                foreach (var attr in entity)
                {
                    if (attr.Source != AttributeSource.Custom)
                        continue;

                    if (views.TryGetValue(attr.Name, out AttributeView attrView))
                    {
                        if (attrView.Data.Equals(attr))
                        {
                            continue; // both entities have the same attribute
                        }
                        attrView.IsMixed = true;
                    }
                    else
                    {
                        views.Add(attr.Name, new AttributeView
                        {
                            Data = attr,
                            IsMixed = false
                        });
                    }
                }
            }

            // update attributes view
            _attrView.Attributes = views.Values.ToList();
            _attrView.EditingEnabled = _selection.Count > 0;
            _attrView.UpdateAttributes();
        }

        private bool HasAttribute(string name)
        {
            return _selection.Any(entity => entity.GetAttribute(name) != null);
        }

        private void AddAttribute(int id, AttributeView attr)
        {
            if (string.IsNullOrEmpty(attr.Data.Name))
            {
                return; // wait for the user to add attribute name
            }

            if (HasAttribute(attr.Data.Name))
            {
                _attrView.AttributeNameIsNotUnique(attr.Data.Name);
                
                _attrView.Attributes.Add(new AttributeView
                {
                    Data = attr.Data
                });
                _attrView.UpdateAttribute(id);
            }
            else
            {
                foreach (var entity in _selection)
                {
                    entity.SetAttribute(attr.Data);
                }

                _attrView.Attributes.Add(new AttributeView
                {
                    Data = attr.Data,
                });
                _attrView.UpdateAttribute(id);
            }
        }

        private void EditAttribute(int id, AttributeView oldValue, AttributeView newValue)
        {
            if (oldValue.Data.Name != newValue.Data.Name) // edit name
            {
                if (string.IsNullOrEmpty(newValue.Data.Name))
                {
                    _attrView.AttributeNameIsEmpty();
                    _attrView.Attributes[id] = oldValue;
                    _attrView.UpdateAttribute(id);
                    return;
                }

                if (HasAttribute(newValue.Data.Name))
                {
                    _attrView.AttributeNameIsNotUnique(newValue.Data.Name);
                    _attrView.Attributes[id] = oldValue;
                    _attrView.UpdateAttribute(id);
                    return;
                }
            }

            foreach (var entity in _selection)
            {
                entity.RemoveAttribute(oldValue.Data.Name);
                entity.SetAttribute(newValue.Data);
            }

            _attrView.Attributes[id] = newValue;
            _attrView.UpdateAttribute(id);
        }

        #region View

        private void View_AttributeChanged(object sender, AttributeChangedEventArgs e)
        {
            if (e.OldValue == null) // add a new attribute
            {
                AddAttribute(e.Index, e.NewValue);
            }
            else // edit an existing attribute
            {
                EditAttribute(e.Index, e.OldValue, e.NewValue);
            }
        }

        private void View_SaveAttributes(object sender, EventArgs e)
        {
        }

        #endregion
    }
}
