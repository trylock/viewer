using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Viewer.Data;

namespace Viewer.Query.Suggestions.Providers
{
    internal class AttributeValueSuggestionProvider : ISuggestionProvider
    {
        private readonly IAttributeCache _attributeCache;
        private readonly AttributeNameListener _listener;
        
        public AttributeValueSuggestionProvider(
            IAttributeCache attributeCache, 
            AttributeNameListener listener)
        {
            _attributeCache = attributeCache;
            _listener = listener;
        }
        
        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            var expected = state.Expected.Find(item =>
                item.RuleIndices[0] == QueryParser.RULE_factor && (
                    item.Tokens.Contains(QueryLexer.STRING) ||
                    item.Tokens.Contains(QueryLexer.INT) ||
                    item.Tokens.Contains(QueryLexer.REAL)
                ));

            // only suggest attribute values in an expression factor
            if (expected == null)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }
            
            var text = state.Caret.ParentToken?.Text ?? "";
            return _listener.AttributeNames
                .SelectMany(name => _attributeCache.GetValues(name))
                .Where(item => 
                    item.ToString()
                        .IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Select(item => 
                    new ReplaceSuggestion(
                        state.Caret, 
                        item.ToString(), 
                        item.ToString(), 
                        item.Type.ToString()));
        }
    }

    internal class AttributeNameListener : ISuggestionListener
    {
        // TODO: names should be isolated to their comparison rule
        private readonly HashSet<string> _attributeNames = new HashSet<string>();

        public IEnumerable<string> AttributeNames => _attributeNames;

        public void MatchToken(IToken token, IReadOnlyList<int> rules)
        {
            // TODO: distinguish attribute and function identifiers 
            if (token.Type == QueryLexer.ID && rules[0] == QueryParser.RULE_factor)
            {
                _attributeNames.Add(token.Text);
            }
        }

        public void EnterRule(IReadOnlyList<int> rules)
        {
        }

        public void ExitRule(IReadOnlyList<int> rules)
        {
        }
    }

    [Export(typeof(ISuggestionProviderFactory))]
    public class AttributeValueSuggestionProviderFactory : ISuggestionProviderFactory
    {
        private readonly IAttributeCache _attributeCache;

        [ImportingConstructor]
        public AttributeValueSuggestionProviderFactory(IAttributeCache attributeCache)
        {
            _attributeCache = attributeCache;
        }

        public ISuggestionProvider Create(IStateCollector stateCollector)
        {
            var listener = new AttributeNameListener();
            stateCollector.AddListener(listener);
            return new AttributeValueSuggestionProvider(_attributeCache, listener);
        }
    }
}
