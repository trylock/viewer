using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Viewer.Query.Suggestions.Providers
{
    /// <summary>
    /// Identifier suggestion is a <see cref="ReplaceSuggestion"/> which correctly wraps given
    /// identifier in quotes if it contains special characters like whitespace etc.
    /// </summary>
    public class IdentifierSuggestion : ReplaceSuggestion
    {
        /// <summary>
        /// Check if <paramref name="identifier"/> is a simple identifier (i.e., token ID)
        /// </summary>
        /// <param name="identifier">Tested identifier</param>
        /// <returns>
        /// true iff <paramref name="identifier"/> is a simple identifier (it matches the ID token)
        /// </returns>
        private static bool IsSimpleIdentifier(string identifier)
        {
            // this is not a very nice way to do it but at least we don't have to duplicate
            // identifier regex in the code
            var lexer = new QueryLexer(new AntlrInputStream(new StringReader(identifier)));
            var id = lexer.NextToken().Type;
            var eof = lexer.NextToken().Type;
            return id == QueryLexer.ID && eof == QueryLexer.Eof;
        }

        /// <summary>
        /// Make sure <paramref name="identifier"/> is a valid identifier token (i.e., if it
        /// contains whitespace characters or special characters, it will be wrapped in `` quotes
        /// to make it a COMPLEX_ID)
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private static string GetIdentifierText(string identifier)
        {
            if (IsSimpleIdentifier(identifier))
                return identifier;
            return '`' + identifier + '`';
        }
        
        public IdentifierSuggestion(CaretToken caretToken, string identifier, string category)
            : base(caretToken, GetIdentifierText(identifier), identifier, category)   
        {
        }
    }
}
