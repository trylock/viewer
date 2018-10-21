using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
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

        private static bool ContainsPrefixSuffix(BaseValue value, string prefix, string suffix)
        {
            var text = value.ToString();
            var prefixIndex = text.IndexOf(prefix, StringComparison.CurrentCultureIgnoreCase);
            if (prefixIndex < 0)
            {
                return false;
            }

            var suffixIndex = text.LastIndexOf(suffix, StringComparison.CurrentCultureIgnoreCase);
            if (prefixIndex + prefix.Length > suffixIndex)
            {
                return false;
            }

            return true;
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

            var parts = state.Caret.SplitParent();
            if (parts.Suffix.Length > 0 && (
                    parts.Suffix[parts.Suffix.Length - 1] == '\n' ||
                    parts.Suffix[parts.Suffix.Length - 1] == '\r'))
            {
                parts.Suffix = parts.Suffix.Substring(0, parts.Suffix.Length - 1);
            }

            return _listener.AttributeNames
                .SelectMany(name => _attributeCache.GetValues(name))
                .Where(item => ContainsPrefixSuffix(item, parts.Prefix, parts.Suffix))
                .Select(item => 
                    new ReplaceSuggestion(
                        state.Caret, 
                        item.ToString(), 
                        item.ToString(CultureInfo.CurrentCulture), 
                        item.Type.ToString()));
        }
    }

    internal class AttributeNameListener : ISuggestionListener
    {
        private readonly HashSet<string> _attributeNames = new HashSet<string>();

        public IEnumerable<string> AttributeNames => _attributeNames;

        public void MatchToken(IToken token, IReadOnlyList<int> rules)
        {
            // TODO: distinguish attribute and function identifiers 
            if (rules[0] == QueryParser.RULE_factor)
            {
                if (token.Type == QueryLexer.ID)
                {
                    _attributeNames.Add(token.Text);
                }

                if (token.Type == QueryLexer.COMPLEX_ID)
                {
                    _attributeNames.Add(token.Text.Substring(1, token.Text.Length - 2));
                }
            }
        }

        public void EnterRule(IReadOnlyList<int> rules, IToken lookahead)
        {
            if (rules[0] == QueryParser.RULE_comparison)
            {
                _attributeNames.Clear();
            }
        }

        public void ExitRule(IReadOnlyList<int> rules, IToken lookahead)
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
