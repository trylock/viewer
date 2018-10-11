using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Viewer.Query.Suggestions.Providers
{
    internal class ViewSuggestionProvider : ISuggestionProvider
    {
        private readonly IQueryViewRepository _views;

        /// <summary>
        /// Name of the category of suggestions returned by this provider
        /// </summary>
        public const string CategoryName = "Query View";
        
        public ViewSuggestionProvider(IQueryViewRepository views)
        {
            _views = views;
        }

        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            var expected = state.Expected.Find(item => 
                item.RuleIndices[0] == QueryParser.RULE_source &&
                item.Tokens.Contains(QueryLexer.ID));

            // only suggest view identifiers in the select part of a query and an identifier 
            // is expected (note, if the caret is in an identifier, it will replace it)
            if (expected == null)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            // We don't want to suggest views if the caret is in a token which is not an
            // identifier.
            if (state.Caret.ParentToken != null && state.Caret.ParentToken.Type != QueryLexer.ID)
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

    [Export(typeof(ISuggestionProviderFactory))]
    public class ViewSuggestionProviderFactory : ISuggestionProviderFactory
    {
        private readonly IQueryViewRepository _views;
        
        [ImportingConstructor]
        public ViewSuggestionProviderFactory(IQueryViewRepository views)
        {
            _views = views;
        }

        public ISuggestionProvider Create(IStateCollector stateCollector)
        {
            return new ViewSuggestionProvider(_views);
        }
    }
}
