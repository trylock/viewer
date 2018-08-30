using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Properties;
using Viewer.UI.Errors;
using Viewer.UI.Explorer;
using Viewer.UI.Tasks;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AttributesPresenter : Presenter<IAttributeView>
    {
        private readonly ITaskLoader _taskLoader;
        private readonly IAttributeManager _attributes;
        private readonly IAttributeStorage _storage;
        private readonly IEntityManager _entityManager;
        private readonly IErrorList _errorList;
        private readonly IFileSystemErrorView _dialogView;

        protected override ExportLifetimeContext<IAttributeView> ViewLifetime { get; }

        /// <summary>
        /// Funtion which determines for each attribute whether it should be managed by this presenter.
        /// </summary>
        private Func<AttributeGroup, bool> _attributePredicate = attr => true;

        /// <summary>
        /// true iff it is enabled to remove and update attributes
        /// </summary>
        public bool IsEditingEnabled => View.ViewType == AttributeViewType.Custom;

        private SortColumn _currentSortColumn = SortColumn.Name;
        private SortDirection _currentSortDirection = SortDirection.Ascending;

        [ImportingConstructor]
        public AttributesPresenter(
            ExportFactory<IAttributeView> viewFactory, 
            ITaskLoader taskLoader, 
            IAttributeManager attrManager,
            IAttributeStorage storage,
            IEntityManager entityManager,
            IErrorList errorList,
            IFileSystemErrorView dialogView)
        {
            ViewLifetime = viewFactory.CreateExport();
            _taskLoader = taskLoader;
            _errorList = errorList;
            _storage = storage;
            _entityManager = entityManager;
            _dialogView = dialogView;
            _attributes = attrManager;
            _attributes.SelectionChanged += Selection_Changed;

            SubscribeTo(View, "View");
            UpdateAttributes();
        }

        public override void Dispose()
        {
            base.Dispose();
            _attributes.SelectionChanged -= Selection_Changed;
        }
        
        private void UpdateAttributes()
        {
            ViewAttributes();
            View_Search(this, EventArgs.Empty);
        }

        public void SetType(AttributeViewType type)
        {
            View.ViewType = type;

            if (type == AttributeViewType.Exif)
            {
                _attributePredicate = attr => attr.Data.Value.Type != TypeId.Image &&
                                              attr.Data.Source == AttributeSource.Metadata;
            }
            else
            {
                _attributePredicate = attr => attr.Data.Source == AttributeSource.Custom;
            }

            UpdateAttributes();
        }

        private void Report(string message, LogType type, Retry retry)
        {
            var entry = new ErrorListEntry
            {
                Group = "Attributes",
                Message = message,
                Type = type,
                RetryOperation = retry
            };
            _errorList.Add(entry);
        }

        private static AttributeGroup CreateAddAttributeView()
        {
            return new AttributeGroup
            {
                Data = new Attribute("", new StringValue(""), AttributeSource.Custom),
                IsMixed = false,
                IsGlobal = true
            };
        }
        
        private void Selection_Changed(object sender, EventArgs eventArgs)
        {
            UpdateAttributes();
        }

        private IEnumerable<AttributeGroup> GetSelectedAttributes()
        {
            return _attributes.GroupAttributesInSelection()
                .Where(_attributePredicate)
                .OrderBy(attr => attr.Data.Name);
        }

        private bool HasAttribute(string name)
        {
            return _attributes
                .GroupAttributesInSelection()
                .Any(attr => attr.Data.Name == name);
        }
        
        private void ViewAttributes()
        {
            // add existing attributes + an empty row for a new attribute
            View.Attributes = GetSelectedAttributes().ToList();
            if (!_attributes.IsSelectionEmpty && IsEditingEnabled)
            {
                View.Attributes.Add(CreateAddAttributeView());
            }

            // update attributes view
            View.UpdateAttributes();
        }

        #region View
        
        private void View_AttributeChanged(object sender, AttributeChangedEventArgs e)
        {
            if (!IsEditingEnabled)
            {
                // revert changes
                View.UpdateAttribute(e.Index);
                return;
            }

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
                    View.AttributeNameIsEmpty();

                    // revert changes
                    View.Attributes[e.Index] = e.OldValue;
                    View.UpdateAttribute(e.Index);
                    return;
                }

                if (HasAttribute(newAttr.Name))
                {
                    View.AttributeNameIsNotUnique(newAttr.Name);

                    // revert changes
                    View.Attributes[e.Index] = e.OldValue;
                    View.UpdateAttribute(e.Index);
                    return;
                }
            }

            _attributes.SetAttribute(oldAttr.Name, newAttr);

            // if we added a new attribute, add a new empty row
            if (oldAttr.Name == "")
            {
                View.Attributes.Add(CreateAddAttributeView());
                View.UpdateAttribute(View.Attributes.Count - 1);
            }

            // show changes
            e.NewValue.IsGlobal = true;
            View.Attributes[e.Index] = e.NewValue;
            View.UpdateAttribute(e.Index);
        }
        
        private void View_AttributeDeleted(object sender, AttributeDeletedEventArgs e)
        {
            if (!IsEditingEnabled)
            {
                return;
            }

            var namesToDelete = e.Deleted.Select(index => View.Attributes[index].Data.Name);
            foreach (var name in namesToDelete)
            {
                _attributes.RemoveAttribute(name);
            }

            ViewAttributes();
        }

        private void StoreEntity(IEntity entity)
        {
            var retry = false;
            do
            {
                retry = false;
                try
                {
                    _storage.Store(entity);
                }
                catch (FileNotFoundException)
                {
                    Report($"Attribute file {entity.Path} was not found.", LogType.Error, null);
                }
                catch (IOException e)
                {
                    View.Invoke(new Action(() =>
                    {
                        retry = _dialogView.FailedToOpenFile(entity.Path, e.Message);
                    }));
                }
                catch (UnauthorizedAccessException)
                {
                    Report($"Unauthorized access to attribute file {entity.Path}.", LogType.Error,
                        null);
                }
                catch (SecurityException)
                {
                    Report($"Unauthorized access to attribute file {entity.Path}.", LogType.Error,
                        null);
                }
            } while (retry);
        }
        
        private async void View_SaveAttributes(object sender, EventArgs args)
        {
            var unsaved = _entityManager.GetModified();
            if (unsaved.Count <= 0)
            {
                return;
            }
            
            var cancellation = new CancellationTokenSource();
            var progress = _taskLoader.CreateLoader(Resources.SavingChanges_Label, cancellation);
            progress.TotalTaskCount = unsaved.Count;
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    foreach (var entity in unsaved)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            // put the changes back to the entity manager
                            _entityManager.SetEntity(entity, false);
                        }
                        else
                        {
                            progress.Report(new LoadingProgress(entity.Path));
                            StoreEntity(entity);
                        }
                    }
                }, cancellation.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            finally
            {
                progress.Close();
                cancellation.Dispose();
            }
        }

        private static IComparer<AttributeGroup> CreateComparer<TKey>(Func<AttributeGroup, TKey> keySelector, int direction)
        {
            return Comparer<AttributeGroup>.Create((a, b) => direction * Comparer<TKey>.Default.Compare(keySelector(a), keySelector(b)));
        }
        
        private void View_SortAttributes(object sender, SortEventArgs e)
        {
            if (_attributes.IsSelectionEmpty)
            {
                return;
            }

            // remove the last row temporarily 
            var lastRow = View.Attributes[View.Attributes.Count - 1];
            if (IsEditingEnabled)
            {
                View.Attributes.RemoveAt(View.Attributes.Count - 1);
            }

            // determine sort column and direction
            _currentSortColumn = e.Column;
            if (e.Column != SortColumn.None)
            {
                if (_currentSortColumn != e.Column || _currentSortDirection == SortDirection.Descending)
                {
                    _currentSortDirection = SortDirection.Ascending;
                }
                else
                {
                    _currentSortDirection = SortDirection.Descending;
                }
            }

            // sort the values 
            IComparer<AttributeGroup> comparer;
            if (e.Column == SortColumn.Name)
            {
                comparer = CreateComparer(attr => attr.Data.Name, (int) _currentSortDirection);
            }
            else if (e.Column == SortColumn.Type)
            {
                comparer = CreateComparer(attr => attr.Data.Value.Type, (int)_currentSortDirection);
            }
            else // if (e.Column == SortColumn.Value)
            {
                comparer = Comparer<AttributeGroup>.Create((a, b) =>
                    (int) _currentSortDirection *
                    ValueComparer.Default.Compare(a.Data.Value, b.Data.Value));
            }
            View.Attributes.Sort(comparer);

            // add back the last row and update the view
            if (IsEditingEnabled)
            {
                View.Attributes.Add(lastRow);
            }

            View.UpdateAttributes();
        }

        private void View_Search(object sender, EventArgs e)
        {
            if (_attributes.IsSelectionEmpty)
            {
                return;
            }

            var lastRow = View.Attributes.LastOrDefault();
            var attrs = GetSelectedAttributes();
            if (View.SearchQuery.Length == 0)
            {
                View.Attributes = attrs.ToList();
            }
            else
            {
                var filter = View.SearchQuery.ToLower();
                View.Attributes = attrs.Where(attr => attr.Data.Name.ToLower().Contains(filter)).ToList();
            }

            if (IsEditingEnabled)
            {
                // if editing is enabled, the last row is an empty row 
                View.Attributes.Add(lastRow);
            }

            View.UpdateAttributes();
        }

        #endregion
    }
}
