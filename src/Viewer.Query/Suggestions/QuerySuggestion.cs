using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;

namespace Viewer.Query.Suggestions
{
    public struct QueryEditorState
    {
        /// <summary>
        /// Text of the query
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// Caret position in <see cref="Query"/> ([0..<see cref="Query"/>.<see cref="string.Length"/>])
        /// </summary>
        public int Caret { get; }

        public QueryEditorState(string query, int caret)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));

            if (caret < 0 || caret > Query.Length)
                throw new ArgumentOutOfRangeException(nameof(caret));
            Caret = caret;
        }
    }

    public interface IQuerySuggestion
    {
        /// <summary>
        /// Suggestion name shown to the user
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Suggestion category name
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Apply this suggestion to the query for which it has been generated and return the
        /// transformed query.
        /// </summary>
        /// <returns>Transformed query</returns>
        QueryEditorState Apply();
    }

    /// <summary>
    /// Compare suggestions based on their names
    /// </summary>
    public class QuerySuggestionNameComparer : 
        IComparer<IQuerySuggestion>, 
        IEqualityComparer<IQuerySuggestion>
    {
        public static QuerySuggestionNameComparer Default = new QuerySuggestionNameComparer();

        public int Compare(IQuerySuggestion x, IQuerySuggestion y)
        {
            return StringComparer.CurrentCulture.Compare(x?.Name, y?.Name);
        }

        public bool Equals(IQuerySuggestion x, IQuerySuggestion y)
        {
            return StringComparer.CurrentCulture.Equals(x?.Name, y?.Name);
        }

        public int GetHashCode(IQuerySuggestion obj)
        {
            return StringComparer.CurrentCulture.GetHashCode(obj);
        }
    }

    /// <summary>
    /// <see cref="Apply"/> method of a no-op suggestion does not do anything. It just displys
    /// <see cref="Name"/>.
    /// </summary>
    public class NopSuggestion : IQuerySuggestion
    {
        private readonly CaretToken _caret;

        public string Name { get; }
        public string Category { get; }

        public NopSuggestion(CaretToken caret, string name, string category)
        {
            _caret = caret;
            Name = name;
            Category = category;
        }

        public QueryEditorState Apply()
        {
            var query = _caret.InputStream.GetText(new Interval(0, _caret.InputStream.Size));
            return new QueryEditorState(query, _caret.StartIndex);
        }
    }
    
    /// <summary>
    /// Replace suggestion replaces the whole container token with given value. If the caret is not
    /// inside a token, the value will be inserted at the caret position.
    /// </summary>
    public class ReplaceSuggestion : IQuerySuggestion
    {
        private readonly CaretToken _caret;
        private readonly string _value;

        public string Name { get; }
        public string Category { get; }

        public ReplaceSuggestion(CaretToken caretToken, string value, string name, string category)
        {
            _caret = caretToken;
            _value = value;
            Name = name;
            Category = category;
        }

        public QueryEditorState Apply()
        {
            var query = _caret.InputStream.GetText(new Interval(0, _caret.InputStream.Size));
            var insertIndex = _caret.StartIndex;
            var container = _caret.ParentToken;
            if (container != null)
            {
                query = query.Remove(
                    container.StartIndex, 
                    container.StopIndex - container.StartIndex + 1);

                Trace.Assert(insertIndex >= container.StartIndex);
                insertIndex = container.StartIndex;
            }

            query = query.Insert(insertIndex, _value);
            return new QueryEditorState(query, insertIndex + _value.Length);
        }
    }
}
