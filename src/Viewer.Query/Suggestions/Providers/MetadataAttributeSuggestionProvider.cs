using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Formats;
using Viewer.Core.Collections;

namespace Viewer.Query.Suggestions.Providers
{
    /// <summary>
    /// This class provides metadata attribute suggestions (e.g., DateTaken, Directory etc.)
    /// </summary>
    internal class MetadataAttributeSuggestionProvider : ISuggestionProvider
    {
        private readonly List<string> _names;

        /// <summary>
        /// Name of the category of suggestions returned by this class
        /// </summary>
        public const string CategoryName = "Metadata";

        public MetadataAttributeSuggestionProvider(List<string> names)
        {
            _names = names;
        }

        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            var expected = state.Expected.Find(item => 
                item.Tokens.Contains(QueryLexer.ID) &&
                item.RuleIndices[0] == QueryParser.RULE_factor);

            if (expected == null)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            var value = state.Caret.ParentPrefix ?? "";
            return _names
                .Where(name => name.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Select(name => new ReplaceSuggestion(state.Caret, name, name, CategoryName));
        }
    }
    
    [Export(typeof(ISuggestionProviderFactory))]
    public class MetadataAttributeSuggestionProviderFactory : ISuggestionProviderFactory
    {
        private readonly List<string> _names;

        [ImportingConstructor]
        public MetadataAttributeSuggestionProviderFactory(
            [ImportMany] IAttributeReaderFactory[] readers)
        {
            _names = readers.SelectMany(reader => reader.MetadataAttributeNames).ToList();
        }

        public ISuggestionProvider Create(IStateCollector collector)
        {
            return new MetadataAttributeSuggestionProvider(_names);
        }
    }
}
