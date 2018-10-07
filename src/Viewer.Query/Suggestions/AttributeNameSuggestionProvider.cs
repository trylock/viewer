using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Viewer.Data;

namespace Viewer.Query.Suggestions
{
    [Export(typeof(ISuggestionProvider))]
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
            // only suggest attribute names in an expression factor
            if (!ExpressionRuleIndices.Contains(state.Context.RuleIndex))
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }
            
            if (!state.ExpectedTokens.Contains(QueryLexer.ID))
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            // if the caret is at a token which is not an identifier, don't suggest attribute names
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
}
