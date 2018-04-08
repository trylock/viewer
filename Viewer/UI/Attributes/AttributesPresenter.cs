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
        private IProgressViewFactory _progressViewFactory;
        private ISelection _selection;
        private IEntityManager _entities;
        private IAttributeManager _attributes;
        
        public AttributesPresenter(
            IAttributeView attrView, 
            IProgressViewFactory progressViewFactory,
            ISelection selection,
            IEntityManager entities,
            IAttributeManager attributes)
        {
            _selection = selection;
            _selection.Changed += Selection_Changed;
            _entities = entities;
            _attributes = attributes;
            _progressViewFactory = progressViewFactory;

            _attrView = attrView;
            _attrView.EditingEnabled = false;
            PresenterUtils.SubscribeTo(_attrView, this, "View");
        }
        
        private AttributeView CreateAddAttributeView()
        {
            return new AttributeView
            {
                Data = new StringAttribute("", ""),
                IsMixed = false
            };
        }
        
        private void Selection_Changed(object sender, EventArgs eventArgs)
        {
            ViewAttributes();
        }

        private bool HasAttribute(string name)
        {
            return _attrView.Attributes.Any(attr => attr.Data.Name == name);
        }

        private void ViewAttributes()
        {
            // add existing attributes + an empty row for a new attribute
            _attrView.Attributes = _attributes.GetSelectedAttributes().ToList();
            _attrView.Attributes.Add(CreateAddAttributeView());

            // update attributes view
            _attrView.EditingEnabled = _selection.Count > 0;
            _attrView.UpdateAttributes();
        }

        #region View

        private void View_AttributeChanged(object sender, AttributeChangedEventArgs e)
        {
            var oldAttr = e.OldValue.Data;
            var newAttr = e.NewValue.Data;
            if (oldAttr.Name == "") // add a new attribute
            {
                if (string.IsNullOrEmpty(newAttr.Name))
                {
                    return; // wait for the user to add a name
                }
            }
            
            if (oldAttr.Name != newAttr.Name) // edit name
            {
                if (string.IsNullOrEmpty(newAttr.Name))
                {
                    _attrView.AttributeNameIsEmpty();

                    // revert changes
                    _attrView.Attributes[e.Index] = e.OldValue;
                    _attrView.UpdateAttribute(e.Index);
                    return;
                }

                if (HasAttribute(newAttr.Name))
                {
                    _attrView.AttributeNameIsNotUnique(newAttr.Name);

                    // revert changes
                    _attrView.Attributes[e.Index] = e.OldValue;
                    _attrView.UpdateAttribute(e.Index);
                    return;
                }
            }

            _attributes.SetAttribute(oldAttr.Name, newAttr);

            // if we added a new attribute, add a new empty row
            if (oldAttr.Name == "")
            {
                _attrView.Attributes.Add(CreateAddAttributeView());
                _attrView.UpdateAttribute(_attrView.Attributes.Count - 1);
            }

            // show changes
            _attrView.Attributes[e.Index] = e.NewValue;
            _attrView.UpdateAttribute(e.Index);
        }
        
        private void View_AttributeDeleted(object sender, AttributeDeletedEventArgs e)
        {
            var namesToDelete = e.Deleted.Select(index => _attrView.Attributes[index].Data.Name);
            foreach (var name in namesToDelete)
            {
                _attributes.RemoveAttribute(name);
            }

            ViewAttributes();
        }

        private void View_SaveAttributes(object sender, EventArgs e)
        {
            if (_attributes.Unsaved.Count <= 0)
            {
                return;
            }

            _progressViewFactory
                .Create()
                .Show("Saving Changes", _attributes.Unsaved.Count, view =>
                {
                    Task.Run(() =>
                    {
                        foreach (var entity in _attributes.Unsaved)
                        {
                            view.CancellationToken.ThrowIfCancellationRequested();
                            view.StartWork(entity.Path);
                            _entities.Save(entity);
                            view.FinishWork();
                        }
                        _attributes.Unsaved.Clear();
                    }, view.CancellationToken);
                });
        }

        #endregion
    }
}
