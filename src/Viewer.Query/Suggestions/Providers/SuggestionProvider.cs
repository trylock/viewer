using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Viewer.Query.Suggestions.Providers
{
    public interface ISuggestionProvider
    {
        /// <summary>
        /// Compute suggestions based on current parser <paramref name="state"/>.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        IEnumerable<IQuerySuggestion> Compute(SuggestionState state);
    }

    public interface ISuggestionProviderFactory
    {
        /// <summary>
        /// Create a suggestion provider.
        /// </summary>
        /// <param name="parser">
        /// Parser which will be used to parse the query. The factory can register its own
        /// listeners to gather additional semantic information for example.
        /// </param>
        /// <returns></returns>
        ISuggestionProvider Create(Parser parser);
    }

    public class SuggestionProviderFactory<T> : ISuggestionProviderFactory where T : ISuggestionProvider, new()
    {
        public ISuggestionProvider Create(Parser parser)
        {
            return new T();
        }
    }
}
