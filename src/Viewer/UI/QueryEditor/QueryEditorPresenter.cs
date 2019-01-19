using System;
using System.Collections.Concurrent;
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
using Viewer.Core.Collections;
using Viewer.Core.UI;
using Viewer.Query;
using Viewer.Query.Suggestions;
using Viewer.UI.Explorer;
using Viewer.UI.Suggestions;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.QueryEditor
{
    internal class QueryEditorPresenter : Presenter<IQueryEditorView>
    {
        private readonly IQueryHistory _appHistory;
        private readonly IFileSystemErrorView _dialogErrorView;
        private readonly IQueryCompiler _queryCompiler;
        private readonly IQueryErrorListener _queryErrorListener;
        private readonly IQuerySuggestions _querySuggestions;
        private readonly IEditor _editor;
        
        private bool _isUnsaved = false;
        
        public QueryEditorPresenter(
            IQueryEditorView view, 
            IFileSystemErrorView dialogErrorView, 
            IQueryHistory appHistory, 
            IQueryCompiler queryCompiler, 
            IQueryErrorListener queryErrorListener,
            IQuerySuggestions querySuggestions,
            IEditor editor)
        {
            View = view;
            _dialogErrorView = dialogErrorView;
            _queryCompiler = queryCompiler;
            _queryErrorListener = queryErrorListener;
            _querySuggestions = querySuggestions;
            _appHistory = appHistory;
            _editor = editor;

            SubscribeTo(View, "View");

            _queryCompiler.Views.Changed += QueryViewsOnChanged;
            UpdateViews();
        }

        private bool _isDisposed;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
                _queryCompiler.Views.Changed -= QueryViewsOnChanged;
            }

            base.Dispose(disposing);
        }

        private void QueryViewsOnChanged(object sender, EventArgs e)
        {
            if (View.InvokeRequired)
            {
                View.BeginInvoke(new Action(UpdateViews));
            }
            else
            {
                UpdateViews();
            }
        }

        private void UpdateViews()
        {
            View.Views = _queryCompiler.Views.ToArray();
        }

        /// <summary>
        /// Set content of this editor window
        /// </summary>
        /// <param name="path">Location of a file which contains the query or null</param>
        /// <param name="content">Query</param>
        public void SetContent(string path, string content)
        {
            View.FullPath = path;
            if (View.FullPath != null)
            {
                View.Text = Path.GetFileName(View.FullPath);
            }

            View.Query = content;
            MarkSaved();
        }

        /// <summary>
        /// Save content of this editor window to its file.
        /// If the editor does not have a file assigned, this will open a dialog to select a file
        /// where the query will be saved.
        /// </summary>
        /// <returns>Task finished after the query is saved to a file.</returns>
        public async Task SaveAsync()
        {
            // if this query is saved in a file and it hasn't changed
            if (View.FullPath != null && !_isUnsaved) 
            {
                return;
            }

            // this query is not saved in a file
            if (View.FullPath == null)
            {
                View.FullPath = View.PickFileForWrite();
                if (View.FullPath == null)
                {
                    // user has not picked a file
                    return;
                }
            }

            // save the query to its file
            try
            {
                await _editor.SaveAsync(View.FullPath, View.Query);
                if (_isDisposed)
                {
                    return;
                }

                MarkSaved();

                View.Text = Path.GetFileName(View.FullPath);
            }
            catch (UnauthorizedAccessException)
            {
                _dialogErrorView.UnauthorizedAccess(View.FullPath);
            }
            catch (SecurityException)
            {
                _dialogErrorView.UnauthorizedAccess(View.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _dialogErrorView.FileNotFound(View.FullPath);
            }
        }

        /// <summary>
        /// Run this query.
        /// </summary>
        /// <returns>Task completed after query compilation.</returns>
        public async Task RunAsync()
        {
            var input = View.Query;
            var query = await Task.Run(() => 
                _queryCompiler.Compile(new StringReader(input), _queryErrorListener));
            if (query != null)
            {
                _appHistory.ExecuteQuery(query);
            }
        }

        /// <summary>
        /// Mark this file unsaved
        /// </summary>
        private void MarkUnsaved()
        {
            _isUnsaved = true;
            if (!View.Text.EndsWith("*"))
            {
                View.Text += '*';
            }
        }

        /// <summary>
        /// Inverse operation to MarkUnsaved
        /// </summary>
        private void MarkSaved()
        {
            _isUnsaved = false;
            if (View.Text.EndsWith("*"))
            {
                View.Text = View.Text.Substring(0, View.Text.Length - 1);
            }
        }
        
        private async void View_SaveQuery(object sender, EventArgs e)
        {
            await SaveAsync();
        }

        private async Task OpenFileAsync(string path)
        {
            var window = await _editor.OpenAsync(path);
            if (window != null && !_isDisposed)
            {
                window.Show(View.DockPanel, DockState.Document);
            }
        }

        private async void View_OpenQuery(object sender, OpenQueryEventArgs e)
        {
            await OpenFileAsync(e.FullPath);
        }

        private async void View_RunQuery(object sender, EventArgs e)
        {
            await RunAsync();
        }

        private void View_QueryChanged(object sender, EventArgs e)
        {
            MarkUnsaved();
        }
        
        private class QuerySuggestion : IComparable<QuerySuggestion>
        {
            public int Version { get; set; }
            public IQuerySuggestion Suggestion { get; set; }

            public int CompareTo(QuerySuggestion other)
            {
                if (ReferenceEquals(this, other))
                    return 0;
                if (ReferenceEquals(null, other))
                    return 1;

                var versionComparison = Version.CompareTo(other.Version);
                if (versionComparison != 0)
                {
                    return versionComparison;
                }
                return QuerySuggestionComparer.Default.Compare(Suggestion, other.Suggestion);
            }
        }

        private int _suggestionVersion;
        private readonly ConcurrentQueue<QuerySuggestion> _waitingSuggestions = 
            new ConcurrentQueue<QuerySuggestion>();

        private void LoadSuggestions(string query, int position, int version)
        {
            if (version != _suggestionVersion)
            {
                return;
            }

            foreach (var item in _querySuggestions.Compute(query, position))
            {
                if (version != _suggestionVersion)
                {
                    return;
                }

                _waitingSuggestions.Enqueue(new QuerySuggestion
                {
                    Suggestion = item,
                    Version = version
                });
            }
        }

        private int _lastVersion;
        
        private void SynchronizeSuggestions()
        {
            var lastVersion = _lastVersion;
            _lastVersion = _suggestionVersion;

            // get all new suggestions which have been generated for current input
            var suggestions = new List<Suggestion>();
            while (_waitingSuggestions.TryDequeue(out var data))
            {
                if (data.Version != _suggestionVersion)
                {
                    continue;
                }

                suggestions.Add(new Suggestion(
                    data.Suggestion.Name, 
                    data.Suggestion.Category, 
                    data));
            }

            if (suggestions.Count == 0)
            {
                if (lastVersion != _suggestionVersion)
                {
                    View.Suggestions = Enumerable.Empty<Suggestion>();
                }
                return;
            }
            
            // combine new and old suggestions
            if (lastVersion == _suggestionVersion)
            {
                suggestions.AddRange(View.Suggestions);
            }

            suggestions.Sort(new SuggestionComparer<QuerySuggestion>());

            // update view
            View.Suggestions = suggestions;
        }

        private void View_Poll(object sender, EventArgs e)
        {
            SynchronizeSuggestions();
        }

        private async void View_SuggestionsRequested(object sender, EventArgs e)
        {
            // load suggestions 
            var query = View.Query;
            var position = Math.Min(View.CaretPosition, query.Length);
            var version = Interlocked.Increment(ref _suggestionVersion);

            await Task.Run(() => LoadSuggestions(query, position, version));
        }

        private void View_SuggestionAccepted(object sender, SuggestionEventArgs e)
        {
            var data = (QuerySuggestion) e.Value.UserData;
            if (data.Version != _suggestionVersion)
            {
                return; // reject outdated suggestions
            }

            var result = data.Suggestion.Apply();
            View.Query = result.Query;
            View.CaretPosition = result.Caret;
        }

        private async void View_OnDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                if (Path.GetExtension(file)?.ToLowerInvariant() == ".vql")
                {
                    await OpenFileAsync(file);
                }

                if (_isDisposed)
                {
                    break;
                }
            }
        }
        
        private async void View_OpenQueryView(object sender, QueryViewEventArgs e)
        {
            await OpenFileAsync(e.View.Path);
        }
    }
}
