using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.UI.Tasks;
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

        public Func<AttributeGroup, bool> AttributePredicate { get; set; } = attr => true;

        private SortColumn _currentSortColumn = SortColumn.None;
        private SortDirection _currentSortDirection = SortDirection.Ascending;

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
        
        private AttributeGroup CreateAddAttributeView()
        {
            return new AttributeGroup
            {
                Data = new StringAttribute("", ""),
                IsMixed = false,
                IsGlobal = true
            };
        }
        
        private void Selection_Changed(object sender, EventArgs eventArgs)
        {
            ViewAttributes();
        }

        private IEnumerable<AttributeGroup> GetSelectedAttributes()
        {
            return _attributes.GetSelectedAttributes().Where(AttributePredicate);
        }

        private bool HasAttribute(string name)
        {
            return GetSelectedAttributes().Any(attr => attr.Data.Name == name);
        }

        private void ViewAttributes()
        {
            // add existing attributes + an empty row for a new attribute
            _attrView.Attributes = GetSelectedAttributes().ToList();
            if (_selection.Count > 0)
            {
                _attrView.Attributes.Add(CreateAddAttributeView());
            }

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
            e.NewValue.IsGlobal = true;
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
            var unsaved = _entities.ConsumeStaged();
            if (unsaved.Count <= 0)
            {
                return;
            }

            _progressViewFactory
                .Create()
                .Show("Saving Changes", unsaved.Count, view =>
                {
                    var progress = view.CreateProgress<CommitProgress>(
                        commit => commit.IsFinished, 
                        commit => commit.Entity.Path);
                    Task.Run(() => unsaved.Commit(view.CancellationToken, progress), view.CancellationToken);
                });
        }
        
        private void View_SortAttributes(object sender, SortEventArgs e)
        {
            if (_attrView.Attributes.Count <= 0)
            {
                return;
            }

            // remove the new row temporarily 
            var lastRow = _attrView.Attributes[_attrView.Attributes.Count - 1];
            _attrView.Attributes.RemoveAt(_attrView.Attributes.Count - 1);

            // function which retrieves a key to sort the attributes by
            string KeySelector(AttributeGroup attr)
            {
                if (e.Column == SortColumn.Name)
                    return attr.Data.Name;
                else if (e.Column == SortColumn.Type)
                    return attr.Data.GetType().Name;
                else // if (e.Column == SortColumn.Value)
                    return attr.Data.ToString();
            }

            // sort the values by given column
            if (e.Column != SortColumn.None)
            {
                if (_currentSortColumn != e.Column || _currentSortDirection == SortDirection.Descending)
                {
                    _attrView.Attributes = _attrView.Attributes.OrderBy(KeySelector).ToList();
                    _currentSortDirection = SortDirection.Ascending;
                }
                else
                {
                    _attrView.Attributes = _attrView.Attributes.OrderByDescending(KeySelector).ToList();
                    _currentSortDirection = SortDirection.Descending;
                }
            }

            _currentSortColumn = e.Column;

            // add back the last row and update the view
            _attrView.Attributes.Add(lastRow);
            _attrView.UpdateAttributes();
        }

        private void View_FilterAttributes(object sender, FilterEventArgs e)
        {
            if (_attrView.Attributes.Count <= 0)
            {
                return;
            }

            var attrs = GetSelectedAttributes();
            var lastRow = _attrView.Attributes.Last();
            if (e.FilterText.Length == 0)
            {
                _attrView.Attributes = attrs.ToList();
            }
            else
            {
                var filter = e.FilterText.ToLower();
                _attrView.Attributes = attrs.Where(attr => attr.Data.Name.ToLower().StartsWith(filter)).ToList();
            }
            _attrView.Attributes.Add(lastRow);
            _attrView.UpdateAttributes();
        }

        #endregion
    }
}
