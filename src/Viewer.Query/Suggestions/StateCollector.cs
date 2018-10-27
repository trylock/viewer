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

namespace Viewer.Query.Suggestions
{
    public interface IStateCollector
    {
        /// <summary>
        /// Add a listener used during the <see cref="Collect"/> method.
        /// </summary>
        /// <param name="listener"></param>
        void AddListener(ISuggestionListener listener);

        /// <summary>
        /// Collect suggestion state
        /// </summary>
        /// <returns>Collected suggestion state</returns>
        SuggestionState Collect();
    }

    public interface IStateCollectorFactory
    {
        /// <summary>
        /// Create a new suggestion state collector
        /// </summary>
        /// <param name="query">Query</param>
        /// <param name="position">Caret position in query</param>
        /// <returns>State collector</returns>
        IStateCollector Create(string query, int position);
    }
    
    public class StateCollector : IStateCollector, ISuggestionListener
    {
        private readonly List<ISuggestionListener> _listeners = new List<ISuggestionListener>();

        private readonly Parser _parser;
        private readonly List<IToken> _tokens;

        private ISuggestionListener Listener => this;

        public StateCollector(List<IToken> tokens, Parser parser)
        {
            _tokens = tokens;
            _parser = parser;
        }

        public void AddListener(ISuggestionListener listener)
        {
            _listeners.Add(listener);
        }

        void ISuggestionListener.MatchToken(IToken token, IReadOnlyList<int> rules)
        {
            foreach (var listener in _listeners)
            {
                listener.MatchToken(token, rules);
            }
        }

        void ISuggestionListener.EnterRule(IReadOnlyList<int> rules, IToken lookahead)
        {
            foreach (var listener in _listeners)
            {
                listener.EnterRule(rules, lookahead);
            }
        }

        void ISuggestionListener.ExitRule(IReadOnlyList<int> rules, IToken lookahead)
        {
            foreach (var listener in _listeners)
            {
                listener.ExitRule(rules, lookahead);
            }
        }

        /// <summary>
        /// Cache of the <see cref="TraverseState"/> method results.
        /// </summary>
        private readonly Dictionary<(int StateNumber, int TokenIndex), List<int>> 
            _traverseStateCache = new Dictionary<(int StateNumber, int TokenIndex), List<int>>();

        /// <summary>
        /// Collection of results (expected tokens and parse tree path)
        /// </summary>
        private List<FollowList> _result;

        /// <summary>
        /// Current parse tree path
        /// </summary>
        private Stack<int> _rulePath;

        public SuggestionState Collect()
        {
            var caret = (CaretToken) _tokens[_tokens.Count - 1];
            
            _result = new List<FollowList>();
            _rulePath = new Stack<int>();
            _traverseStateCache.Clear();

            TraverseState(_parser.Atn.ruleToStartState[0], 0);

            return new SuggestionState(caret, _result);
        }

        private IEnumerable<int> TraverseState(ATNState state, int tokenIndex)
        {
            // don't traverse the same state at the same position twice
            if (_traverseStateCache.TryGetValue((state.stateNumber, tokenIndex),
                out var values))
            {
                return values;
            }

            _rulePath.Push(state.ruleIndex);
            try
            {

                var result = TraverseStateImpl(state, tokenIndex);
                _traverseStateCache.Add((state.stateNumber, tokenIndex), result);
                return result;
            }
            finally
            {
                _rulePath.Pop();
            }
        }

        private List<int> TraverseStateImpl(ATNState startState, int tokenIndex)
        {
            if (_tokens.Count <= tokenIndex)
            {
                return new List<int>();
            }
            
            Listener.EnterRule(_rulePath.ToList(), _tokens[tokenIndex]);

            // traverse subtree of this rule
            var tokens = new List<int>();
            var stack = new Stack<(ATNState State, int TokenIndex)>();
            stack.Push((startState, tokenIndex));
            
            // TODO: traversing all alternatives is overkill for (almost) an LL(1) language
            while (stack.Count > 0)
            {
                var (state, inputPosition) = stack.Pop();
                var lookahead = _tokens[inputPosition];

                // if we have reached the end of this rule
                if (state.StateType == StateType.RuleStop)
                {
                    // capture position where other rules can start
                    tokens.Add(inputPosition);
                    Listener.ExitRule(_rulePath.ToList(), lookahead);
                    continue;
                }
                
                foreach (var transition in state.TransitionsArray)
                {
                    if (transition is RuleTransition ruleTransition)
                    {
                        var endPosition = TraverseState(transition.target, inputPosition);
                        foreach (var position in endPosition)
                        {
                            stack.Push((ruleTransition.followState, position));
                        }
                    }
                    else if (transition is PredicateTransition predicateTransition)
                    {
                        if (predicateTransition.Predicate.Eval(_parser, ParserRuleContext.EMPTY))
                        {
                            stack.Push((predicateTransition.target, inputPosition));
                        }
                    }
                    else if (transition is WildcardTransition)
                    {
                        if (lookahead.Type == CaretToken.TokenType)
                        {
                            _result.Add(new FollowList
                            {
                                Tokens = IntervalSet.Of(0, _parser.Atn.maxTokenType),
                                RuleIndices = _rulePath.ToList()
                            });
                            continue;
                        }
                        stack.Push((transition.target, inputPosition + 1));
                        Listener.MatchToken(lookahead, _rulePath.ToList());
                    }
                    else if (transition.IsEpsilon)
                    {
                        stack.Push((transition.target, inputPosition));
                    }
                    else
                    {
                        var label = transition.Label;
                        if (label.Count <= 0)
                        {
                            continue;
                        }

                        // report transitions at the caret token
                        if (lookahead.Type == CaretToken.TokenType)
                        {
                            _result.Add(new FollowList
                            {
                                Tokens = label,
                                RuleIndices = _rulePath.ToList()
                            });
                            continue;
                        }

                        // follow transition
                        if (transition is NotSetTransition)
                        {
                            label = label.Complement(IntervalSet.Of(0, _parser.Atn.maxTokenType));
                        }

                        if (label.Contains(lookahead.Type))
                        {
                            stack.Push((transition.target, inputPosition + 1));
                            Listener.MatchToken(lookahead, _rulePath.ToList());
                        }
                    }
                }
            }

            return tokens;
        }
    }
    
    [Export(typeof(IStateCollectorFactory))]
    public class StateCollectorFactory : IStateCollectorFactory
    {
        public IStateCollector Create(string query, int position)
        {
            var lexer = new QueryLexer(new AntlrInputStream(new StringReader(query)));
            var tokenStream = new CommonTokenStream(new CaretTokenSource(lexer, position, new[]
            {
                QueryLexer.ADD_SUB, QueryLexer.MULT_DIV, QueryLexer.REL_OP, QueryLexer.LPAREN,
                QueryLexer.RPAREN
            }));

            // we will need a random access to the input as we traverse the ATN
            var tokens = new List<IToken>();
            for (; ; )
            {
                var token = tokenStream.LT(1);
                tokenStream.Consume();
                tokens.Add(token);
                if (token.Type == Lexer.Eof || token.Type == CaretToken.TokenType)
                {
                    break;
                }
            }

            Trace.Assert(tokens.Count >= 1, "tokens.Count >= 1"); // CARET
            Trace.Assert(
                tokens.Last().Type == CaretToken.TokenType,
                "tokens.Last().Type == CaretToken.TokenType");

            var parser = new QueryParser(tokenStream);
            return new StateCollector(tokens, parser);
        }
    }
}
