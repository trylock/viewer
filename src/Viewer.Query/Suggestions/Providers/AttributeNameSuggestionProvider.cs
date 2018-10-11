using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Viewer.Data;

namespace Viewer.Query.Suggestions.Providers
{
    internal class AttributeNameSuggestionProvider : ISuggestionProvider
    {
        private readonly IAttributeCache _attributeCache;

        /// <summary>
        /// Name of the categroy of suggestions returned by this provider
        /// </summary>
        public const string CategoryName = "User attribute";

        [ImportingConstructor]
        public AttributeNameSuggestionProvider(IAttributeCache attributeCache)
        {
            _attributeCache = attributeCache;
        }
        
        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            var expected = state.Expected.Find(item => 
                item.RuleIndices[0] == QueryParser.RULE_factor &&
                item.Tokens.Contains(QueryLexer.ID));

            // only suggest attribute names in an expression factor where and identifier is
            // expected
            if (expected == null)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            // if the caret is in a token which is not an identifier, don't suggest attribute names
            if (state.Caret.ParentToken != null && state.Caret.ParentToken.Type != QueryLexer.ID)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            var prefix = state.Caret.ParentPrefix ?? "";
            return _attributeCache
                .GetNames(prefix)
                .Select(name => new ReplaceSuggestion(state.Caret, name, name, CategoryName));
        }
    }

    [Export(typeof(ISuggestionProviderFactory))]
    public class AttributeNameSuggestionProviderFactory : ISuggestionProviderFactory
    {
        private readonly IAttributeCache _attributeCache;

        [ImportingConstructor]
        public AttributeNameSuggestionProviderFactory(IAttributeCache attributeCache)
        {
            _attributeCache = attributeCache;
        }

        public ISuggestionProvider Create(IStateCollector stateCollector)
        {
            return new AttributeNameSuggestionProvider(_attributeCache);
        }
    }
}
