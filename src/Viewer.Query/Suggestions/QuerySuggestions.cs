using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using NLog;
using Viewer.Core.Collections;
using Viewer.Query.Suggestions.Providers;

namespace Viewer.Query.Suggestions
{
    public interface ISuggestionListener
    {
        /// <summary>
        /// Method called whenever a token is matched.
        /// </summary>
        /// <param name="token">Matched token</param>
        /// <param name="rules">
        /// Path in the parse tree on which <paramref name="token"/> has been matched.
        /// </param>
        void MatchToken(IToken token, IReadOnlyList<int> rules);

        /// <summary>
        /// Method called whenever the algorithm enters a rule. 
        /// </summary>
        /// <param name="rules">
        /// Path in the parse tree. The first rule is the rule which we have just entered.
        /// </param>
        /// <param name="lookahead">Next token in the input</param>
        void EnterRule(IReadOnlyList<int> rules, IToken lookahead);

        /// <summary>
        /// Method called whenever the algorithm exits a rule.
        /// </summary>
        /// <param name="rules">
        /// Path in the parse tree. The first rule is the rule which we have just exited.
        /// </param>
        /// <param name="lookahead">Next token in the input</param>
        void ExitRule(IReadOnlyList<int> rules, IToken lookahead);
    }

    public interface IQuerySuggestions
    {
        /// <summary>
        /// Find all suggestions for <paramref name="query"/> at position <paramref name="index"/>.
        /// </summary>
        /// <param name="query">Query for which you want to get suggestions</param>
        /// <param name="index">Position in <paramref name="query"/> of a cursor</param>
        /// <returns>List of suggestions</returns>
        IEnumerable<IQuerySuggestion> Compute(string query, int index);
    }
    
    [Export(typeof(IQuerySuggestions))]
    public class QuerySuggestions : IQuerySuggestions
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IEnumerable<ISuggestionProvider> _providers;
        private readonly IEnumerable<ISuggestionProviderFactory> _providerFactories;
        private readonly IStateCollectorFactory _stateCollectorFactory;

        [ImportingConstructor]
        public QuerySuggestions(
            [ImportMany] ISuggestionProviderFactory[] providerFactories,
            IStateCollectorFactory stateCollectorFactory)
        {
            const string keyword = "Keyword";

            _stateCollectorFactory = stateCollectorFactory;
            _providers = new ISuggestionProvider[]
            {
                // keyword suggestions
                new StaticTokenSuggestionProvider(QueryLexer.SELECT, keyword, new []{ "select" }),
                new StaticTokenSuggestionProvider(QueryLexer.WHERE, keyword, new []{ "where" }),
                new StaticTokenSuggestionProvider(QueryLexer.ORDER, keyword, new []{ "order by" }),
                new StaticTokenSuggestionProvider(QueryLexer.BY, keyword, new []{ "by" }),
                new StaticTokenSuggestionProvider(QueryLexer.UNION_EXCEPT, keyword, new []{ "union", "except" }),
                new StaticTokenSuggestionProvider(QueryLexer.INTERSECT, keyword, new []{ "intersect" }),
                new StaticTokenSuggestionProvider(QueryLexer.AND, keyword, new []{ "and" }),
                new StaticTokenSuggestionProvider(QueryLexer.OR, keyword, new []{ "or" }),
                new StaticTokenSuggestionProvider(QueryLexer.NOT, keyword, new []{ "not" }),
                new StaticTokenSuggestionProvider(QueryLexer.DIRECTION, keyword, new []{ "desc", "asc" }),
            };
            _providerFactories = providerFactories;
        }

        public IEnumerable<IQuerySuggestion> Compute(string query, int index)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            if (index < 0 || index > query.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var stateCollector = _stateCollectorFactory.Create(query, index);

            // create suggestion providers
            var providers = _providerFactories
                .Select(factory => factory.Create(stateCollector))
                .Concat(_providers)
                .ToList(); // create providers before collecting sates

            // compute suggestions based on collected state
            var state = stateCollector.Collect();
            var text = state.Caret.ParentToken?.Text ?? "";
            var suggestions = providers
                .SelectMany(provider => provider.Compute(state))
                .SkipSingletonWith(
                    new NopSuggestion(state.Caret, text, text), 
                    QuerySuggestionNameComparer.Default);
            return suggestions;
        }
        
    }
}
