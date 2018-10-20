using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
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

            // only suggest attribute names in an expression factor where an identifier is
            // expected
            if (expected == null)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            var prefix = state.Caret.ParentPrefix ?? "";
            return _attributeCache
                .GetNames(prefix)
                .Select(name => new IdentifierSuggestion(state.Caret, name, CategoryName));
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
