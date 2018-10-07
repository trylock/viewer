using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Viewer.Query.Suggestions
{
    /// <summary>
    /// Captured state used for query suggestions
    /// </summary>
    public class SuggestionState
    {
        /// <summary>
        /// Current parser context
        /// </summary>
        public ParserRuleContext Context { get; private set; }

        /// <summary>
        /// Caret token
        /// </summary>
        public CaretToken Caret { get; private set; }

        /// <summary>
        /// Tokens expected at <see cref="Caret"/>
        /// </summary>
        public IntervalSet ExpectedTokens { get; private set; }

        /// <summary>
        /// Accessed attribute names in a subexpression before caret
        /// </summary>
        public List<string> AttributeNames { get; } = new List<string>();

        /// <summary>
        /// Capture state at the caret position
        /// </summary>
        /// <param name="context">Parser context at the caret</param>
        /// <param name="caret">Caret token</param>
        /// <param name="expectedTokens">Tokens expected at the caret location</param>
        public void Capture(ParserRuleContext context, CaretToken caret, IntervalSet expectedTokens)
        {
            // don't capture another state unless context is a child of the captured context
            if (Context != null)
            {
                RuleContext item = context;
                while (item.Parent != null && item.RuleIndex != Context.RuleIndex)
                {
                    item = item.Parent;
                }

                if (item.RuleIndex != Context.RuleIndex)
                {
                    return;
                }
            }

            Context = context;
            Caret = caret;
            ExpectedTokens = expectedTokens;
        }
    }

}
