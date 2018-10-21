using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Viewer.IO;

namespace Viewer.Query.Suggestions.Providers
{
    internal class DirectorySuggestion : IQuerySuggestion
    {
        /// <summary>
        /// Directory name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Suggestion category (this will always be <c>"Directory"</c>)
        /// </summary>
        public string Category => "Directory";

        private readonly CaretToken _caret;

        public DirectorySuggestion(CaretToken caret, string name)
        {
            _caret = caret;
            Name = name;
        }

        private static bool IsSeparator(char c)
        {
            return c == '"' || PathUtils.PathSeparators.Contains(c);
        }

        private (int StartIndex, int StopIndex) GetDirectoryAtCaret(string input)
        {
            var startIndex = _caret.StartIndex;
            var endIndex = startIndex;

            while (startIndex > 0 && !IsSeparator(input[startIndex - 1]))
            {
                --startIndex;
            }
            
            var patternStopIndex = Math.Min(_caret.ParentToken.StopIndex, input.Length);
            while (endIndex < patternStopIndex && !IsSeparator(input[endIndex]))
            {
                ++endIndex;
            }

            return (startIndex, endIndex);
        }

        public QueryEditorState Apply()
        {
            var input = _caret.InputStream.GetText(new Interval(0, _caret.InputStream.Size));
            
            var range = GetDirectoryAtCaret(input);
            var transformedQuery = input.Remove(
                range.StartIndex, 
                range.StopIndex - range.StartIndex);
            transformedQuery = transformedQuery.Insert(range.StartIndex, Name);

            return new QueryEditorState(transformedQuery, range.StartIndex + Name.Length);
        }
    }
    
    internal class DirectorySuggestionProvider : ISuggestionProvider
    {
        private readonly IFileSystem _fileSystem;
        
        public DirectorySuggestionProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        private static string RangeSubstring(string value, int beginIndex, int endIndex)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (endIndex > value.Length)
                throw new ArgumentOutOfRangeException(nameof(endIndex));

            if (beginIndex > endIndex)
            {
                return "";
            }
            
            return value.Substring(beginIndex, endIndex - beginIndex);
        }

        private static (string Prefix, string LastPart) Split(string pattern)
        {
            if (pattern.Length <= 1)
            {
                return ("", "");
            }

            var patternEndsWithQuote = pattern[pattern.Length - 1] == '\"';
            var lastSeparatorIndex = pattern.LastIndexOfAny(PathUtils.PathSeparators);
            var prefix = RangeSubstring(pattern, 1, lastSeparatorIndex + 1);
            var lastPart = RangeSubstring(pattern, 
                Math.Max(lastSeparatorIndex + 1, 1), 
                pattern.Length - (patternEndsWithQuote ? 1 : 0));
            return (prefix, lastPart);
        }

        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            var expected = state.Expected.Find(item => 
                item.RuleIndices[0] == QueryParser.RULE_source);

            // make sure we are in the select part of a query
            if (expected == null)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            // make sure we are in a path pattern
            if (state.Caret.ParentToken == null || 
                state.Caret.ParentToken.Type != QueryLexer.STRING)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }
            
            // make sure the caret is in the pattern (not after the last quote)
            if (state.Caret.ParentOffset >= state.Caret.ParentToken.Text.Length &&
                state.Caret.ParentToken.Text[state.Caret.ParentOffset - 1] == '\"')
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }
            
            var patternParts = Split(state.Caret.ParentPrefix);

            // if the directory name at the caret is a pattern, don't suggest anything
            if (PathPattern.ContainsSpecialCharacters(patternParts.LastPart))
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            if (patternParts.LastPart.Length == 0)
            {
                patternParts.LastPart = "*";
            }
            else
            {
                patternParts.LastPart = "*" + patternParts.LastPart + "*";
            }

            try
            {
                var pattern = Path.Combine(patternParts.Prefix, patternParts.LastPart);
                var finder = _fileSystem.CreateFileFinder(pattern);
                var result = finder
                    .GetDirectories()
                    .Select(PathUtils.GetLastPart)
                    .Distinct()
                    .Select(name => new DirectorySuggestion(state.Caret, name));
                return result;
            }
            catch (ArgumentException) // the pattern contains invalid characters
            {
                // suggestions should not throw for invalid input
                return Enumerable.Empty<IQuerySuggestion>();
            }
        }
    }

    [Export(typeof(ISuggestionProviderFactory))]
    public class DirectorySuggestionProviderFactory : ISuggestionProviderFactory
    {
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public DirectorySuggestionProviderFactory(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public ISuggestionProvider Create(IStateCollector stateCollector)
        {
            return new DirectorySuggestionProvider(_fileSystem);
        }
    }
}
