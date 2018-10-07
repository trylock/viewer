using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.Suggestions.Providers
{
    [Export(typeof(ISuggestionProvider))]
    public class AttributeValueSuggestionProvider : ISuggestionProvider
    {
        private readonly IAttributeCache _attributeCache;
        
        [ImportingConstructor]
        public AttributeValueSuggestionProvider(IAttributeCache attributeCache)
        {
            _attributeCache = attributeCache;
        }

        /// <summary>
        /// Indices of rules which generate a (sub)expression
        /// </summary>
        private static readonly int[] ExpressionRuleIndices =
        {
            QueryParser.RULE_factor,
            QueryParser.RULE_multiplication,
            QueryParser.RULE_expression,
            QueryParser.RULE_comparison,
            QueryParser.RULE_literal,
            QueryParser.RULE_conjunction,
            QueryParser.RULE_predicate,
            QueryParser.RULE_argumentList,
        };

        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            // only suggest attribute values in an expression factor
            if (!ExpressionRuleIndices.Contains(state.Context.RuleIndex))
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }
            
            // a value has to be expected at the caret
            if (!state.ExpectedTokens.Contains(QueryLexer.STRING) &&
                !state.ExpectedTokens.Contains(QueryLexer.INT) &&
                !state.ExpectedTokens.Contains(QueryLexer.REAL))
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            var text = state.Caret.ParentToken?.Text ?? "";
            return state.AttributeNames
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
}
