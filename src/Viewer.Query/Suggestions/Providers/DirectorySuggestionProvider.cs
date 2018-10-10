using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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

        public static string TrimQuotes(string pattern)
        {
            pattern = pattern.Substring(1);
            if (pattern.Length > 0 && pattern[pattern.Length - 1] == '"')
            {
                pattern = pattern.Remove(pattern.Length - 1, 1);
            }

            return pattern;
        }

        private (int StartIndex, int StopIndex) GetDirectoryAtCaret(string input)
        {
            var startIndex = _caret.StartIndex;
            var endIndex = startIndex;

            while (startIndex > 0 && 
                   input[startIndex - 1] != '"' &&
                   !PathUtils.PathSeparators.Contains(input[startIndex - 1]))
            {
                --startIndex;
            }

            var patternStopIndex = _caret.ParentToken.StopIndex;
            while (endIndex < patternStopIndex &&
                   input[endIndex + 1] != '"' &&
                   !PathUtils.PathSeparators.Contains(input[endIndex + 1]))
            {
                ++endIndex;
            }

            return (startIndex, endIndex);
        }

        public QueryEditorState Apply()
        {
            var input = _caret.InputStream.GetText(new Interval(0, _caret.InputStream.Size));
            
            var range = GetDirectoryAtCaret(input);
            var transformedQuery = input.Remove(range.StartIndex, range.StopIndex - range.StartIndex);
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

        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            // make sure we are in the select part of a query
            if (state.Context.RuleIndex != QueryParser.RULE_source)
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

            var patternPrefix = DirectorySuggestion.TrimQuotes(state.Caret.ParentPrefix);

            // if the directory name at the caret is a pattern, don't suggest anything
            var lastPart = Path.GetFileName(patternPrefix);
            if (PathPattern.ContainsSpecialCharacters(lastPart))
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            patternPrefix += '*';

            try
            {
                var finder = _fileSystem.CreateFileFinder(patternPrefix);
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

        public ISuggestionProvider Create(Parser parser)
        {
            return new DirectorySuggestionProvider(_fileSystem);
        }
    }
}
