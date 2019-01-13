using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query.Suggestions.Providers
{
    /// <summary>
    /// Suggest function names
    /// </summary>
    internal class FunctionSuggestionProvider : ISuggestionProvider
    {
        public const string Category = "Function";

        private readonly IRuntime _runtime;

        /// <summary>
        /// Function names which will not be suggested
        /// </summary>
        private readonly List<string> _blackList = new List<string>
        {
            "and", "or", "not", "+", "-", "*", "/", "<", "<=", "=", "==", ">=", ">", "!="
        };

        public FunctionSuggestionProvider(IRuntime runtime)
        {
            _runtime = runtime;
        }

        public IEnumerable<IQuerySuggestion> Compute(SuggestionState state)
        {
            var expected = state.Expected.Find(item =>
                item.RuleIndices[0] == QueryParser.RULE_factor &&
                item.Tokens.Contains(QueryLexer.ID));

            // only suggestion function names in a query factor where a function name is expected
            if (expected == null)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }

            // return all function names which contain caret token prefix
            var prefix = state.Caret.ParentPrefix ?? "";
            return _runtime.Functions
                .Select(func => func.Name)
                .Where(name => !_blackList.Contains(name))
                .Distinct()
                .Where(name => name.IndexOf(prefix, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Select(name => new FunctionSuggestion(state.Caret, name, Category));
        }
    }

    [Export(typeof(ISuggestionProviderFactory))]
    public class FunctionSuggestionProviderFactory : ISuggestionProviderFactory
    {
        private readonly IRuntime _runtime;

        [ImportingConstructor]
        public FunctionSuggestionProviderFactory(IRuntime runtime)
        {
            _runtime = runtime;
        }

        public ISuggestionProvider Create(IStateCollector collector)
        {
            return new FunctionSuggestionProvider(_runtime);
        }
    }
}
