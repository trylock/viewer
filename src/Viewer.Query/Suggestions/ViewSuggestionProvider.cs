using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Viewer.Query.Suggestions
{
    [Export(typeof(ISuggestionProvider))]
    internal class ViewSuggestionProvider : ISuggestionProvider
    {
        private readonly IQueryViewRepository _views;

        /// <summary>
        /// Name of the category of suggestions returned by this provider
        /// </summary>
        public const string CategoryName = "Query View";

        [ImportingConstructor]
        public ViewSuggestionProvider(IQueryViewRepository views)
        {
            _views = views;
        }

        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            // only suggest view identifiers in the select part of a query
            if (state.Context.RuleIndex != QueryParser.RULE_source)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            // We don't want to suggest views if it is not possilbe to suggest identifiers or 
            // the caret is in a token which is not an identifier.
            if (!state.ExpectedTokens.Contains(QueryLexer.ID) || (
                    state.Caret.ParentToken != null &&
                    state.Caret.ParentToken.Type != QueryLexer.ID))
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            var prefix = state.Caret.ParentPrefix ?? "";
            return 
                from view in _views
                where view.Name.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase)
                select new ReplaceSuggestion(state.Caret, view.Name, view.Name, CategoryName);
        }
    }
}
