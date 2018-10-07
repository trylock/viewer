using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using NLog;

namespace Viewer.Query.Suggestions
{
    public interface IQuerySuggestions
    {
        /// <summary>
        /// Find all suggestions for <paramref name="query"/> at position <paramref name="index"/>.
        /// </summary>
        /// <param name="query">Query for which you want to get suggestions</param>
        /// <param name="index">Position in <paramref name="query"/> of a cursor</param>
        /// <returns>List of suggestions</returns>
        IEnumerable<IQuerySuggestion> Compute(string query, int index);
    }

    [Export(typeof(IQuerySuggestions))]
    internal class QuerySuggestions : IQuerySuggestions
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IEnumerable<ISuggestionProvider> _providers;
        
        [ImportingConstructor]
        public QuerySuggestions([ImportMany] ISuggestionProvider[] providers)
        {
            const string keyword = "Keyword";

            _providers = new ISuggestionProvider[]
            {
                // keyword suggestions
                new StaticTokenSuggestionProvider(QueryLexer.SELECT, keyword, new []{ "select" }),
                new StaticTokenSuggestionProvider(QueryLexer.WHERE, keyword, new []{ "where" }),
                new StaticTokenSuggestionProvider(QueryLexer.ORDER, keyword, new []{ "order" }),
                new StaticTokenSuggestionProvider(QueryLexer.BY, keyword, new []{ "by" }),
                new StaticTokenSuggestionProvider(QueryLexer.UNION_EXCEPT, keyword, new []{ "union", "except" }),
                new StaticTokenSuggestionProvider(QueryLexer.INTERSECT, keyword, new []{ "intersect" }),
                new StaticTokenSuggestionProvider(QueryLexer.AND, keyword, new []{ "and" }),
                new StaticTokenSuggestionProvider(QueryLexer.OR, keyword, new []{ "or" }),
                new StaticTokenSuggestionProvider(QueryLexer.NOT, keyword, new []{ "not" }),
                new StaticTokenSuggestionProvider(QueryLexer.DIRECTION, keyword, new []{ "desc", "asc" }),
            }.Concat(providers).ToList();
        }

        public IEnumerable<IQuerySuggestion> Compute(string query, int index)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            if (index < 0 || index > query.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            var input = new AntlrInputStream(new StringReader(query));
            var lexer = new QueryLexer(input);
            var tokenStream = new CommonTokenStream(new CaretTokenSource(lexer, index));
            
            // parse everything up to the caret
            var state = new SuggestionState();
            var parser = new QueryParser(tokenStream)
            {
                BuildParseTree = false
            };
            parser.AddParseListener(new ParseListener(parser, state));
            parser.AddErrorListener(new ErrorListener(state));

            try
            {
                parser.entry();
            }
            catch (RecognitionException e)
            {
                Logger.Debug(e, "Syntax error during suggestion computation.");
            }

            // If there is an error before caret, we can't do anything at this point. Suggestion 
            // computation should not fail though. We are expecting an invalid input.
            if (state.Context == null)
            {
                return Enumerable.Empty<IQuerySuggestion>();
            }
            
            // compute suggestions based on current state
            var suggestions = _providers.SelectMany(provider => provider.Compute(state));
            return suggestions;
        }

        private class ParseListener : QueryParserBaseListener
        {
            private readonly Parser _parser;
            private readonly SuggestionState _state;

            public ParseListener(Parser parser, SuggestionState state)
            {
                _parser = parser;
                _state = state;
            }

            public override void EnterLiteral(QueryParser.LiteralContext context)
            {
                base.EnterLiteral(context);
                _state.AttributeNames.Clear();
            }

            public override void EnterComparison(QueryParser.ComparisonContext context)
            {
                base.EnterComparison(context);
                _state.AttributeNames.Clear();
            }
            
            public override void ExitFactor(QueryParser.FactorContext context)
            {
                base.ExitFactor(context);

                if (context.ID() != null && context.LPAREN() == null) // attribute identifier
                {
                    _state.AttributeNames.Add(context.ID().Symbol.Text);
                }
            }
            
            public override void EnterEveryRule(ParserRuleContext context)
            {
                base.EnterEveryRule(context);
                
                // This handles the case where the caret token is found in a production of type:
                // `P -> A1 | ... | An | epsilon` (epsilon is an empty string)
                var lookahead = _parser.TokenStream.LT(1);
                if (lookahead.Type == CaretToken.TokenType)
                {
                    var caret = (CaretToken) lookahead;
                    var expectedTokens = _parser.GetExpectedTokens();
                    _state.Capture(context, caret, expectedTokens);
                }
            }
        }

        private class ErrorListener : IAntlrErrorListener<IToken>
        {
            private readonly SuggestionState _state;

            public ErrorListener(SuggestionState state)
            {
                _state = state;
            }

            public void SyntaxError(
                TextWriter output, 
                IRecognizer recognizer, 
                IToken offendingSymbol, 
                int line, 
                int charPositionInLine,
                string msg, 
                RecognitionException e)
            {
                // we only care about errors at the caret
                if (offendingSymbol.Type != CaretToken.TokenType)
                {
                    return;
                }

                var parser = (Parser) recognizer;
                var caret = (CaretToken) offendingSymbol;
                _state.Capture(parser.Context, caret, parser.GetExpectedTokens());
            }
        }
    }
}
