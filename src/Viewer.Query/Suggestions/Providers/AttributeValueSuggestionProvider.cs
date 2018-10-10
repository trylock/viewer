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
    internal class AttributeNameListener : QueryParserBaseListener
    {
        private readonly List<string> _names = new List<string>();

        /// <summary>
        /// List of rules which clear the _names list 
        /// </summary>
        private static readonly int[] ClearNamesRules =
        {
            QueryParser.RULE_comparison,
            QueryParser.RULE_literal,
            QueryParser.RULE_conjunction,
            QueryParser.RULE_predicate
        };

        public IEnumerable<string> AttributeNames => _names;
        
        public override void EnterEveryRule(ParserRuleContext context)
        {
            base.EnterEveryRule(context);

            if (ClearNamesRules.Contains(context.RuleIndex))
            {
                _names.Clear();
            }
        }

        public override void ExitFactor(QueryParser.FactorContext context)
        {
            base.ExitFactor(context);

            // find an attribute identifier and add it to current range
            if (context.ID() != null && context.LPAREN() == null)
            {
                _names.Add(context.ID().GetText());
            }
        }
    }
    
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

    [Export(typeof(ISuggestionProviderFactory))]
    public class AttributeValueSuggestionProviderFactory : ISuggestionProviderFactory
    {
        private readonly IAttributeCache _attributeCache;

        [ImportingConstructor]
        public AttributeValueSuggestionProviderFactory(IAttributeCache attributeCache)
        {
            _attributeCache = attributeCache;
        }

        public ISuggestionProvider Create(Parser parser)
        {
            var listener = new AttributeNameListener();
            parser.AddParseListener(listener);

            return new AttributeValueSuggestionProvider(_attributeCache, listener);
        }
    }
}
