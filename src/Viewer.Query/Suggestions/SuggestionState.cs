using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Viewer.Query.Suggestions
{
    public class FollowList
    {
        /// <summary>
        /// Indices of rules which lead to the caret token. They are ordered from the newest
        /// to the oldest (i.e., rule at index <c>i</c> is the parent of the rule at index
        /// <c>i + 1</c>)
        /// </summary>
        public List<int> RuleIndices { get; set; } = new List<int>();

        /// <summary>
        /// List of expected token types
        /// </summary>
        public IntervalSet Tokens { get; set; } = new IntervalSet();
    }

    /// <summary>
    /// Captured state used for query suggestions
    /// </summary>
    public class SuggestionState
    {
        /// <summary>
        /// Caret token
        /// </summary>
        public CaretToken Caret { get; }

        /// <summary>
        /// List of expected tokens with context (parser rule indices) in which they are expected.
        /// </summary>
        public List<FollowList> Expected { get; }

        /// <summary>
        /// List of all expected tokens without a context (union of the
        /// <see cref="FollowList.Tokens"/> properties from the <see cref="Expected"/> list)
        /// </summary>
        public IntervalSet ExpectedTokens { get; }

        public SuggestionState(CaretToken caret, List<FollowList> expected)
        {
            Caret = caret ?? throw new ArgumentNullException(nameof(caret));
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            ExpectedTokens = new IntervalSet();
            foreach (var item in Expected)
            {
                ExpectedTokens.AddAll(item.Tokens);
            }
        }
    }

}
