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
        /// <param name="collector">
        /// Collector which will be used to collect suggestion state. Providers can register their
        /// listeners to collect additional metadata.
        /// </param>
        /// <returns></returns>
        ISuggestionProvider Create(IStateCollector collector);
    }
}
