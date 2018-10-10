using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Viewer.Query.Suggestions
{
    /// <summary>
    /// Caret token source returns all tokens up to a certain point (the caret position). After
    /// this point, it returns <see cref="CaretToken"/> instead.
    /// </summary>
    internal class CaretTokenSource : ITokenSource
    {
        private readonly Lexer _lexer;
        private readonly int _caretPosition; 

        public CaretTokenSource(Lexer lexer, int caretPosition)
        {
            _lexer = lexer;
            _caretPosition = caretPosition;
        }

        private bool _caretTokenEmitted;

        public IToken NextToken()
        {
            var token = _lexer.NextToken();

            Trace.Assert(token.StartIndex >= 0);

            // emit caret token
            if (!_caretTokenEmitted)
            {
                if (token.Type != Lexer.Eof &&
                    token.StartIndex <= _caretPosition &&
                    token.StopIndex + 1 >= _caretPosition)
                {
                    _caretTokenEmitted = true;
                    return new CaretToken(_lexer, InputStream, _caretPosition, token);
                }
                else if (token.Type == Lexer.Eof || token.StartIndex > _caretPosition)
                {
                    _caretTokenEmitted = true;
                    return new CaretToken(_lexer, InputStream, _caretPosition);
                }
            }

            // emit EOF token after the caret
            if (_caretTokenEmitted)
            {
                return new CommonToken(Lexer.Eof);
            }
            return token;
        }

        public int Line => _lexer.Line;
        public int Column => _lexer.Column;
        public ICharStream InputStream => ((ITokenSource)_lexer).InputStream;
        public string SourceName => _lexer.SourceName;
        public ITokenFactory TokenFactory
        {
            get => _lexer.TokenFactory;
            set => _lexer.TokenFactory = value;
        }
    }

}
