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
using Viewer.UI.Forms;
using Viewer.UI.Suggestions;
using Viewer.UI.Tasks;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    internal class AttributesPresenter : Presenter<IAttributeView>
    {
        private readonly ITaskLoader _taskLoader;
        private readonly IAttributeManager _attributes;
        private readonly IEntityManager _entityManager;
        private readonly IErrorList _errorList;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IAttributeCache _attributeCache;
        private readonly IQueryHistory _queryHistory;
        
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
        
        public AttributesPresenter(
            IAttributeView view, 
            ITaskLoader taskLoader, 
            IAttributeManager attrManager,
            IEntityManager entityManager,
            IErrorList errorList,
            IFileSystemErrorView dialogView,
            IAttributeCache attributeCache,
            IQueryHistory queryHistory)
        {
            View = view;
            _taskLoader = taskLoader;
            _errorList = errorList;
            _entityManager = entityManager;
            _dialogView = dialogView;
            _attributeCache = attributeCache;
            _queryHistory = queryHistory;
            _attributes = attrManager;

            // register event handlers
            _queryHistory.BeforeQueryExecuted += QueryHistory_BeforeQueryExecuted;
            _attributes.SelectionChanged += Selection_Changed;

            SubscribeTo(View, "View");
            UpdateAttributes();
        }

        private void QueryHistory_BeforeQueryExecuted(object sender, BeforeQueryExecutedEventArgs e)
        {
            var decision = HandleUnsavedAttributes();
            if (decision == UnsavedDecision.Cancel)
            {
                e.Cancel();
            }
        }

        private bool _isDisposed;

        public override void Dispose()
        {
            _isDisposed = true;
            _queryHistory.BeforeQueryExecuted -= QueryHistory_BeforeQueryExecuted;
            _attributes.SelectionChanged -= Selection_Changed;

            base.Dispose();
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
                _attributePredicate = attr => attr.Value.Value.Type != TypeId.Image &&
                                              attr.Value.Source == AttributeSource.Metadata;
            }
            else
            {
                _attributePredicate = attr => attr.Value.Source == AttributeSource.Custom;
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
                Value = new Attribute("", new StringValue(""), AttributeSource.Custom),
                HasMultipleValues = false,
                IsInAllEntities = true
            };
        }

        private List<IEntity> _unsavedSelection = new List<IEntity>();
        private bool _suppressUnsavedCheck = false;

        /// <summary>
        /// Ask user what to do with unsaved attributes and then carry out the selected action
        /// </summary>
        /// <returns>Selected action</returns>
        private UnsavedDecision HandleUnsavedAttributes()
        {
            var decision = UnsavedDecision.None;
            if (_suppressUnsavedCheck || !IsEditingEnabled)
            {
                return decision;
            }

            var unsaved = _entityManager.GetModified();
            if (unsaved.Count <= 0)
            {
                return decision;
            }

            decision = View.ReportUnsavedAttributes();

            if (decision == UnsavedDecision.Save)
            {
#pragma warning disable 4014 // async is not awaited, we don't want to block the UI 
                SaveAttributesAsync(unsaved);
#pragma warning restore 4014
            }
            else if (decision == UnsavedDecision.Revert)
            {
                foreach (var entity in unsaved)
                {
                    entity.Revert();
                }
            }
            else if (decision == UnsavedDecision.Cancel)
            {
                // put all the changes back
                foreach (var entity in unsaved)
                {
                    entity.Return();
                }
                
                View.EnsureVisible();

                // reset current selection to the previous selection
                _suppressUnsavedCheck = true;
                try
                {
                    _attributes.Selection.Replace(_unsavedSelection);
                }
                finally
                {
                    _suppressUnsavedCheck = false;
                }
            }

            return decision;
        }
        
        private void Selection_Changed(object sender, EventArgs eventArgs)
        {
            if (IsEditingEnabled)
            {
                HandleUnsavedAttributes();
                _unsavedSelection = _attributes.Selection.ToList();
            }

            UpdateAttributes();
        }

        private IEnumerable<AttributeGroup> GetSelectedAttributes()
        {
            return _attributes.GroupAttributesInSelection()
                .Where(_attributePredicate)
                .OrderBy(attr => attr.Value.Name);
        }

        private bool HasAttribute(string name)
        {
            return _attributes
                .GroupAttributesInSelection()
                .Any(attr => attr.Value.Name == name);
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

            var oldAttr = e.OldValue.Value;
            var newAttr = e.NewValue.Value;
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
            e.NewValue.IsInAllEntities = true;
            View.Attributes[e.Index] = e.NewValue;
            View.UpdateAttribute(e.Index);
        }
        
        private void View_AttributeDeleted(object sender, AttributeDeletedEventArgs e)
        {
            if (!IsEditingEnabled)
            {
                return;
            }

            var namesToDelete = e.Deleted.Select(index => View.Attributes[index].Value.Name);
            foreach (var name in namesToDelete)
            {
                _attributes.RemoveAttribute(name);
            }

            ViewAttributes();
        }

        private void StoreEntity(IModifiedEntity entity)
        {
            var retry = false;
            do
            {
                retry = false;
                try
                {
                    entity.Save();
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

            await SaveAttributesAsync(unsaved);
        }

        private async Task SaveAttributesAsync(IReadOnlyList<IModifiedEntity> unsaved)
        {
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
                            entity.Return();
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
                comparer = CreateComparer(attr => attr.Value.Name, (int) _currentSortDirection);
            }
            else if (e.Column == SortColumn.Type)
            {
                comparer = CreateComparer(attr => attr.Value.Value.Type, (int)_currentSortDirection);
            }
            else // if (e.Column == SortColumn.Value)
            {
                comparer = Comparer<AttributeGroup>.Create((a, b) =>
                    (int) _currentSortDirection *
                    ValueComparer.Default.Compare(a.Value.Value, b.Value.Value));
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
                View.Attributes = attrs.Where(attr => attr.Value.Name.ToLower().Contains(filter)).ToList();
            }

            if (IsEditingEnabled)
            {
                // if editing is enabled, the last row is an empty row 
                View.Attributes.Add(lastRow);
            }

            View.UpdateAttributes();
        }

        /// <summary>
        /// Prefix of an attribute name whose suggestions are being loaded
        /// </summary>
        private string _loadingNamePrefix;

        private async void View_NameChanged(object sender, NameEventArgs e)
        {
            // reset suggestions
            View.Suggestions = new List<SuggestionItem>();

            var value = e.Value?.Trim();
            if (value == null)
            {
                return;
            }
            
            _loadingNamePrefix = value;

            var suggestions = await Task.Run(() =>
            {
                var items = new List<SuggestionItem>();

                // load suggestions
                foreach (var name in _attributeCache.GetNames(value))
                {
                    items.Add(new SuggestionItem
                    {
                        Text = name,
                        Category = "User attribute"
                    });
                }

                return items;
            });

            if (_isDisposed || _loadingNamePrefix != value)
            {
                return;
            }
            
            View.Suggestions = suggestions;
        }

        private async void View_BeginValueEdit(object sender, NameEventArgs e)
        {
            // reset suggestions
            View.Suggestions = new List<SuggestionItem>();

            var suggestions = await Task.Run(() =>
            {
                var items = new List<SuggestionItem>();
                foreach (var value in _attributeCache.GetValues(e.Value))
                {
                    items.Add(new SuggestionItem
                    {
                        Text = value.ToString(),
                        Category = value.Type.ToString(),
                        UserData = value
                    });
                }

                return items;
            });

            if (_isDisposed)
            {
                return;
            }

            View.Suggestions = suggestions;
        }

        #endregion
    }
}
