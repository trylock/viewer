using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query.Suggestions.Providers
{
    public class FunctionSuggestion : IdentifierSuggestion
    {
        public FunctionSuggestion(CaretToken caretToken, string identifier, string category) 
            : base(caretToken, identifier, category)
        {
        }

        public override QueryEditorState Apply()
        {
            var state = base.Apply();

            // we expect the caret to be after the inserted function name
            // this can be 
            if (state.Caret < 0 || state.Caret > state.Query.Length)
            {
                return state;
            }

            // add () if necessary and move caret in-between the parentheses
            int parentesisPosition = -1;
            for (int i = state.Caret; i < state.Query.Length; ++i)
            {
                if (state.Query[i] == '(') // if there is a parentesis, don't add another pair
                {
                    parentesisPosition = i;
                    break;
                }
                else if (!char.IsWhiteSpace(state.Query[i]))
                {
                    break; // we haven't found a parentheses => insert a new pair
                }
            }

            // insert parentheses if necessary
            var query = state.Query;
            if (parentesisPosition < 0)
            {
                query = state.Query.Insert(state.Caret, "()");
                parentesisPosition = state.Caret + 1;
            }
            
            // move the caret to parentheses
            return new QueryEditorState(query, parentesisPosition);
        }
    }
}
