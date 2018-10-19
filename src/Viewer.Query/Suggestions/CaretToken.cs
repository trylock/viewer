using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Viewer.Query.Suggestions
{
    /// <summary>
    /// Token which represents caret in the input. 
    /// </summary>
    /// <remarks>
    /// This token is not defined or used in the lexer. Instead, it is added programmatically
    /// during query suggestion computation.
    /// </remarks>
    public class CaretToken : CommonToken
    {
        /// <summary>
        /// Type of the caret token
        /// </summary>
        public const int TokenType = 9000;

        /// <summary>
        /// Token in which the caret is. This can be null if the caret is not in any token.
        /// </summary>
        public IToken ParentToken { get; }

        /// <summary>
        /// Index of caret within <see cref="ParentToken"/> or -1 if
        /// <see cref="ParentToken"/> is null.
        /// </summary>
        public int ParentOffset { get; }

        /// <summary>
        /// Get prefix of the container token which ends at the caret or null if this caret does
        /// not have a parent token.
        /// </summary>
        public string ParentPrefix => ParentToken?.Text?.Substring(0, ParentOffset);

        /// <summary>
        /// Create a caret token without container token
        /// </summary>
        public CaretToken(
            ITokenSource source,
            ICharStream stream,
            int position) 
            : base(
                new Antlr4.Runtime.Sharpen.Tuple<ITokenSource, ICharStream>(source, stream), 
                TokenType, 
                Lexer.DefaultTokenChannel,
                position, 
                position)
        {
            ParentToken = null;
            ParentOffset = -1;
        }

        /// <summary>
        /// Create caret token with container token
        /// </summary>
        /// <param name="source"></param>
        /// <param name="stream"></param>
        /// <param name="position">Index of the caret in the input</param>
        /// <param name="parentToken">Container in which the caret is</param>
        public CaretToken(
            ITokenSource source, 
            ICharStream stream,
            int position,
            IToken parentToken) 
            : base(
                new Antlr4.Runtime.Sharpen.Tuple<ITokenSource, ICharStream>(source, stream), 
                TokenType, 
                Lexer.DefaultTokenChannel,
                position, 
                position)
        {
            if (parentToken == null)
                throw new ArgumentNullException(nameof(parentToken));
            if (position < parentToken.StartIndex)
                throw new ArgumentOutOfRangeException(nameof(position));

            ParentToken = parentToken;
            ParentOffset = position - parentToken.StartIndex;
        }

        /// <summary>
        /// Split parent token to the part before caret and the part after caret. 
        /// </summary>
        /// <returns>
        /// Parent token text before caret and parent token text after caret. No part will be null.
        /// If the caret does not have a parent token, both parts will be empty strings.
        /// </returns>
        public (string Prefix, string Suffix) SplitParent()
        {
            if (ParentToken == null)
            {
                return ("", "");
            }

            var beforeCaret = ParentToken.Text.Substring(0, ParentOffset);
            var afterCaret = ParentToken.Text.Substring(ParentOffset);
            return (beforeCaret, afterCaret);
        }
    }
}
