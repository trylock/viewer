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

    /// <summary>
    /// Provide suggestions for a single token type from a fixed list of values.
    /// </summary>
    internal class StaticTokenSuggestionProvider : ISuggestionProvider
    {
        private readonly string _category;
        private readonly List<string> _values;
        private readonly int _tokenType;

        public StaticTokenSuggestionProvider(
            int tokenType, 
            string category, 
            IEnumerable<string> values)
        {
            _tokenType = tokenType;
            _category = category;
            _values = values.ToList();
        }

        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            if (!state.ExpectedTokens.Contains(_tokenType))
            {
                yield break;
            }

            // the caret is not in any other token
            if (state.Caret.ParentToken == null)
            {
                foreach (var value in _values)
                {
                    yield return new ReplaceSuggestion(state.Caret, value, value, _category);
                }
            }
            else // the caret is in some token
            {
                var partialInput = state.Caret.ParentToken.Text;
                foreach (var value in _values)
                {
                    if (value != partialInput &&
                        value.IndexOf(partialInput, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        yield return new ReplaceSuggestion(state.Caret, value, value, _category);
                    }
                }
            }
        }
    }
}
