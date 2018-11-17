using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Viewer.Data;
using Viewer.Data.Formats.Attributes;
using Viewer.Query.Expressions;
using Attribute = Viewer.Data.Attribute;
using ConstantExpression = Viewer.Query.Expressions.ConstantExpression;

namespace Viewer.Query
{
    public interface IQueryCompiler
    {
        /// <summary>
        /// Repository of views available to this compiler
        /// </summary>
        IQueryViewRepository Views { get; }

        /// <summary>
        /// Compile given query to an internal structure which can then be evaluated.
        /// </summary>
        /// <param name="input">Stream with the query</param>
        /// <param name="queryErrorListener">Error reporter</param>
        /// <returns>Compiled query</returns>
        IExecutableQuery Compile(TextReader input, IQueryErrorListener queryErrorListener);

        /// <summary>
        /// Same as Compile(new StringReader(query), defaultQueryErrorListener)
        /// </summary>
        /// <param name="query">Query to compile</param>
        /// <returns>Compiled query or null if there were errors during compilation</returns>
        IExecutableQuery Compile(string query);
    }

    internal class QueryCompilationListener : IQueryParserListener
    {
        private readonly Stack<IQuery> _queries = new Stack<IQuery>();
        private readonly Stack<ValueExpression> _expressions = new Stack<ValueExpression>();
        private readonly Stack<EntityComparer> _comparers = new Stack<EntityComparer>();
        private readonly Stack<int> _expressionsFrameStart = new Stack<int>();

        private readonly IQueryFactory _queryFactory;
        private readonly IQueryCompiler _compiler;
        private readonly IQueryErrorListener _errorListener;
        private readonly IRuntime _runtime;

        public QueryCompilationListener(
            IQueryFactory queryFactory,
            IQueryCompiler compiler,
            IQueryErrorListener errorListener,
            IRuntime runtime)
        {
            _queryFactory = queryFactory;
            _compiler = compiler;
            _errorListener = errorListener;
            _runtime = runtime;
        }

        public IQuery Finish()
        {
            var query = _queries.Pop();
            Trace.Assert(query != null, "query != null");
            Trace.Assert(_queries.Count == 0, "_queries.Count == 0");
            return query;
        }

        /// <summary>
        /// This method will compile a sequence of left associative binary
        /// <paramref name="operators"/> with the same priority. All operands are fetched from the
        /// <paramref name="stack"/>. Operator is evaluated using the
        /// <paramref name="applyOperator"/> function.
        /// </summary>
        /// <typeparam name="T">Type of operands</typeparam>
        /// <param name="operators">Operator terminals</param>
        /// <param name="stack">Stack with operands</param>
        /// <param name="applyOperator">Operator evaluation function</param>
        private void CompileLeftAssociativeOperator<T>(
            ITerminalNode[] operators,
            Stack<T> stack,
            Func<ITerminalNode, T, T, T> applyOperator)
        {
            if (operators.Length <= 0)
                return;

            // fetch operands
            var operands = new List<T>
            {
                stack.Pop()
            };
            for (var i = 0; i < operators.Length; ++i)
            {
                operands.Add(stack.Pop());
            }

            // make sure we apply operators from left to right 
            operands.Reverse();

            // apply operators
            var result = operands[0];
            for (var i = 0; i < operators.Length; ++i)
            {
                result = applyOperator(operators[i], result, operands[i + 1]);
            }
            stack.Push(result);
        }

        public void VisitTerminal(ITerminalNode node)
        {
        }

        public void VisitErrorNode(IErrorNode node)
        {
        }

        public void EnterEveryRule(ParserRuleContext ctx)
        {
        }

        public void ExitEveryRule(ParserRuleContext ctx)
        {
        }

        public void EnterEntry(QueryParser.EntryContext context)
        {
        }

        public void ExitEntry(QueryParser.EntryContext context)
        {
        }

        #region Query expression (UNION, EXCEPT and INTERSECT)

        // All methods in this group are either no-ops or they pop 1 or more queries from the
        // _queries stack and push one query to the _queries stack as a result of an operation.

        public void EnterQueryExpression(QueryParser.QueryExpressionContext context)
        {
        }

        public void ExitQueryExpression(QueryParser.QueryExpressionContext context)
        {
            var operators = context.UNION_EXCEPT();

            CompileLeftAssociativeOperator(operators, _queries, (op, left, right) =>
            {
                if (string.Equals(op.Symbol.Text, "union", StringComparison.OrdinalIgnoreCase))
                {
                    var union = left.Union(right);
                    return union;
                }

                Trace.Assert(string.Equals(
                    op.Symbol.Text, 
                    "except", 
                    StringComparison.OrdinalIgnoreCase
                ), $"Expecting UNION or EXCEPT, got {op.Symbol.Text}");

                var except = left.Except(right);
                return except;
            });
        }

        public void EnterIntersection(QueryParser.IntersectionContext context)
        {
        }

        public void ExitIntersection(QueryParser.IntersectionContext context)
        {
            CompileLeftAssociativeOperator(
                context.INTERSECT(),
                _queries, 
                (op, left, right) => left.Intersect(right));
        }

        public void EnterQueryFactor(QueryParser.QueryFactorContext context)
        {
        }

        public void ExitQueryFactor(QueryParser.QueryFactorContext context)
        {
        }

        #endregion

        #region Simple query (SELECT, WHERE, ORDER BY)

        // All methods in this group are either no-ops or they pop one query from the _queries
        // stack, transform it and push the transfromed query back to the _queries stack.
        // ExitSource is a source of queries. It only pushes 1 query to the _queries stack.
        // No queries will be removed from the stack in this method.

        public void EnterQuery(QueryParser.QueryContext context)
        {
        }

        public void ExitQuery(QueryParser.QueryContext context)
        {
        }

        public void EnterUnorderedQuery(QueryParser.UnorderedQueryContext context)
        {
        }

        public void ExitUnorderedQuery(QueryParser.UnorderedQueryContext context)
        {
        }

        public void EnterSource(QueryParser.SourceContext context)
        {
        }

        public void ExitSource(QueryParser.SourceContext context)
        {
            string viewIdentifier = null;
            var viewIdentifierToken = context.ID();
            if (viewIdentifierToken != null)
            {
                viewIdentifier = viewIdentifierToken.Symbol.Text;
            }
            else
            {
                viewIdentifierToken = context.COMPLEX_ID();
                if (viewIdentifierToken != null)
                {
                    viewIdentifier = viewIdentifierToken.Symbol.Text.Substring(
                        1, 
                        viewIdentifierToken.Symbol.Text.Length - 2);
                }
            }

            // create a query SELECT view
            if (viewIdentifier != null)
            {
                var view = _compiler.Views.Find(viewIdentifier);
                if (view == null)
                {
                    ReportError(
                        viewIdentifierToken.Symbol.Line, 
                        viewIdentifierToken.Symbol.Column,
                        $"Unknown view '{viewIdentifier}'");
                    return;
                }

                var query = _compiler.Compile(new StringReader(view.Text), _errorListener) as IQuery;
                if (query == null) // compilation of the view failed
                {
                    HaltCompilation();
                    return;
                }

                _queries.Push(query.View(viewIdentifier));
            }
            else if (context.STRING() != null) // create a query SELECT pattern
            {
                var patternSymbol = context.STRING().Symbol;
                var pattern = ParseString(
                    patternSymbol.Line, 
                    patternSymbol.Column, 
                    patternSymbol.Text);

                try
                {
                    var query = _queryFactory.CreateQuery(pattern) as IQuery;
                    _queries.Push(query);
                }
                catch (ArgumentException) // invalid pattern
                {
                    ReportError(
                        patternSymbol.Line, 
                        patternSymbol.Column, 
                        $"Invalid path pattern: {pattern}");
                }
            }

            // otherwise, this is a subquery => it will push its own tree to the stack
        }

        public void EnterOptionalWhere(QueryParser.OptionalWhereContext context)
        {
        }

        public void ExitOptionalWhere(QueryParser.OptionalWhereContext context)
        {
            if (context.WHERE() != null)
            {
                var query = _queries.Pop();
                var predicate = _expressions.Pop();
                _queries.Push(query.Where(predicate));
            }
        }

        public void EnterOptionalOrderBy(QueryParser.OptionalOrderByContext context)
        {
        }

        public void ExitOptionalOrderBy(QueryParser.OptionalOrderByContext context)
        {
            if (context.ORDER() != null)
            {
                var query = _queries.Pop();
                var comparer = _comparers.Pop();

                var startIndex = context.BY().Symbol.StopIndex + 1;
                var endIndex = context.Stop.StopIndex;
                var text = context.Stop.InputStream
                    .GetText(new Interval(startIndex, endIndex))
                    .Trim();
                _queries.Push(query.WithComparer(comparer, text));
            }
        }

        public void EnterOrderByList(QueryParser.OrderByListContext context)
        {
        }

        public void ExitOrderByList(QueryParser.OrderByListContext context)
        {
            CompileLeftAssociativeOperator(
                context.PARAM_DELIMITER(), 
                _comparers, 
                (_, left, right) => new EntityComparer(left, right));
        }
        
        public void EnterOrderByKey(QueryParser.OrderByKeyContext context)
        {
        }

        private int _sortDirection;
        private int SortDirection
        {
            get => _sortDirection;
            set
            {
                if (value != 1 && value != -1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _sortDirection = value;
            }
        }

        public void ExitOrderByKey(QueryParser.OrderByKeyContext context)
        {
            var valueExpression = _expressions.Pop();
            var key = new SortParameter
            {
                Direction = SortDirection,
                Getter = valueExpression.CompileFunction(_runtime)
            };

            _comparers.Push(new EntityComparer(new List<SortParameter>{ key }));
        }

        public void EnterOptionalDirection(QueryParser.OptionalDirectionContext context)
        {
        }

        public void ExitOptionalDirection(QueryParser.OptionalDirectionContext context)
        {
            SortDirection = 1;
            var directionString = context.DIRECTION()?.Symbol.Text;
            if (string.Equals(directionString, "desc", StringComparison.OrdinalIgnoreCase))
            {
                SortDirection = -1;
            }
        }

        #endregion

        #region Value expression

        // Methods in this group are either no-ops or they pop 1 or more expressions from the
        // _expression stack and push 1 expression as a result of some function on the removed
        // subexpressions. The ExitFactor method is a soruce of expressions. It pushes 1
        // expression to the _expressions stack. No expressions will be removed by this method.

        public void EnterPredicate(QueryParser.PredicateContext context)
        {
        }

        public void ExitPredicate(QueryParser.PredicateContext context)
        {
            CompileLeftAssociativeOperator(context.OR(), _expressions, 
                (op, left, right) => 
                    new OrExpression(op.Symbol.Line, op.Symbol.Column, left, right));
        }
        
        public void EnterConjunction(QueryParser.ConjunctionContext context)
        {
        }

        public void ExitConjunction(QueryParser.ConjunctionContext context)
        {
            CompileLeftAssociativeOperator(context.AND(), _expressions,
                (op, left, right) =>
                    new AndExpression(op.Symbol.Line, op.Symbol.Column, left, right));
        }
        
        public void EnterLiteral(QueryParser.LiteralContext context)
        {
        }

        public void ExitLiteral(QueryParser.LiteralContext context)
        {
            var op = context.NOT();
            if (op == null)
            {
                return;
            }
            
            var expr = _expressions.Pop();
            _expressions.Push(new NotExpression(op.Symbol.Line, op.Symbol.Column, expr));
        }

        public void EnterComparison(QueryParser.ComparisonContext context)
        {
        }

        public void ExitComparison(QueryParser.ComparisonContext context)
        {
        }

        public void EnterComparisonRemainder(QueryParser.ComparisonRemainderContext context)
        {
        }

        public void ExitComparisonRemainder(QueryParser.ComparisonRemainderContext context)
        {
            var opToken = context.REL_OP();
            if (opToken == null)
            {
                return;
            }

            var op = opToken.Symbol;
            var right = _expressions.Pop();
            var left = _expressions.Pop();

            BinaryOperatorExpression value = null;
            switch (op.Text)
            {
                case "<":
                    value = new LessThanOperator(op.Line, op.Column, left, right);
                    break;
                case "<=":
                    value = new LessThanOrEqualOperator(op.Line, op.Column, left, right);
                    break;
                case "!=":
                    value = new NotEqualOperator(op.Line, op.Column, left, right);
                    break;
                case "=":
                    value = new EqualOperator(op.Line, op.Column, left, right);
                    break;
                case ">":
                    value = new GreaterThanOperator(op.Line, op.Column, left, right);
                    break;
                case ">=":
                    value = new GreaterThanOrEqualOperator(op.Line, op.Column, left, right);
                    break;
            }

            Trace.Assert(value != null, "Invalid comparison operator " + op.Text);

            _expressions.Push(value);
        }

        public void EnterExpression(QueryParser.ExpressionContext context)
        {
        }

        public void ExitExpression(QueryParser.ExpressionContext context)
        {
            CompileLeftAssociativeOperator(context.ADD_SUB(), _expressions, 
                (opNode, left, right) =>
                {
                    var op = opNode.Symbol;
                    BinaryOperatorExpression value = null;
                    switch (op.Text)
                    {
                        case "+":
                            value = new AdditionExpression(op.Line, op.Column, left, right);
                            break;
                        case "-":
                            value = new SubtractionExpression(op.Line, op.Column, left, right);
                            break;
                    }

                    Trace.Assert(value != null, "Invalid addition operator " + op.Text);
                    return value;
                });
        }
        
        public void EnterMultiplication(QueryParser.MultiplicationContext context)
        {
        }

        public void ExitMultiplication(QueryParser.MultiplicationContext context)
        {
            CompileLeftAssociativeOperator(context.MULT_DIV(), _expressions,
                (opNode, left, right) =>
                {
                    var op = opNode.Symbol;
                    BinaryOperatorExpression value = null;
                    switch (op.Text)
                    {
                        case "*":
                            value = new MultiplicationExpression(op.Line, op.Column, left, right);
                            break;
                        case "/":
                            value = new DivisionExpression(op.Line, op.Column, left, right);
                            break;
                    }

                    Trace.Assert(value != null, "Invalid multiplication operator " + op.Text);

                    return value;
                });
        }
        
        public void EnterFactor(QueryParser.FactorContext context)
        {
        }

        public void ExitFactor(QueryParser.FactorContext context)
        {
            BaseValue constantValue = null;

            // parse INT
            var constantToken = context.INT();
            if (constantToken != null)
            {
                var value = int.Parse(constantToken.Symbol.Text, CultureInfo.InvariantCulture);
                constantValue = new IntValue(value);
            }
            else if (context.REAL() != null) // parse REAL
            {
                constantToken = context.REAL();
                var value = double.Parse(constantToken.Symbol.Text, CultureInfo.InvariantCulture);
                constantValue = new RealValue(value);
            }
            else if (context.STRING() != null) // parse STRING
            {
                constantToken = context.STRING();
                constantValue = new StringValue(ParseString(
                    constantToken.Symbol.Line,
                    constantToken.Symbol.Column,
                    constantToken.Symbol.Text));
            }
            
            // if this is a constant
            if (constantValue != null)
            {
                _expressions.Push(new ConstantExpression(
                    constantToken.Symbol.Line, 
                    constantToken.Symbol.Column, 
                    constantValue));
                return;
            }
            
            // parse ID
            string identifier = null;
            var identifierToken = context.ID();
            if (identifierToken != null)
            {
                identifier = identifierToken.Symbol.Text;
            }
            else if (context.COMPLEX_ID() != null) // parse COMPLEX_ID
            {
                identifierToken = context.COMPLEX_ID();
                identifier = identifierToken.Symbol.Text.Substring(
                    1,
                    identifierToken.Symbol.Text.Length - 2);
            }
            
            // if this is an attribute identifier
            if (identifierToken != null && context.LPAREN() == null)
            {
                _expressions.Push(new AttributeAccessExpression(
                    identifierToken.Symbol.Line, 
                    identifierToken.Symbol.Column, 
                    identifier));
                return;
            }

            // if this is a function identifier
            if (identifierToken != null)
            {
                var stackTop = _expressionsFrameStart.Pop();
                var parameters = new List<ValueExpression>();
                while (_expressions.Count > stackTop)
                {
                    parameters.Add(_expressions.Pop());
                }

                parameters.Reverse();
                _expressions.Push(new FunctionCallExpression(
                    identifierToken.Symbol.Line, 
                    identifierToken.Symbol.Column, 
                    identifier, 
                    parameters));
                return;
            }

            // otherwise, this is a subexpression => it is already on the stack
        }

        /// <summary>
        /// In this method we remember the "return address" of a function. That is, a place
        /// in the _expressions stack where we should return after the function call. To put
        /// it in another way, it is the index of the first argument of this function in the stack.
        /// </summary>
        /// <param name="context"></param>
        public void EnterArgumentList(QueryParser.ArgumentListContext context)
        {
            _expressionsFrameStart.Push(_expressions.Count);
        }

        public void ExitArgumentList(QueryParser.ArgumentListContext context)
        {
        }

        #endregion

        private string ParseString(int line, int column, string value)
        {
            if (value.Length <= 0 || value[0] != '"')
                throw new ArgumentOutOfRangeException(nameof(value));

            // number of characters to remove from the start and from the end
            int trimStart = 1;
            var trimEnd = 0;

            // remove the end character if the value is terminated (it won't be terminated iff
            // we have reached the EOF)
            var lastCharacter = value[value.Length - 1];
            if (value.Length > 1 && (
                    lastCharacter == '"' ||
                    lastCharacter == '\n' ||
                    lastCharacter == '\r'))
            {
                trimEnd = 1;
            }

            // check if the string is terminated correctly
            if (value.Length <= 1 || value[value.Length - 1] != '"')
            {
                _errorListener.OnCompilerError(line, column, "Unterminated string literal");
            }

            return value.Substring(trimStart, value.Length - trimStart - trimEnd);
        }

        private void ReportError(int line, int column, string message)
        {
            _errorListener.OnCompilerError(line, column, message);
            HaltCompilation();
        }

        private void HaltCompilation()
        {
            throw new ParseCanceledException();
        }
    }
    
    internal class ParserErrorListener : IAntlrErrorListener<IToken>
    {
        private readonly IQueryErrorListener _queryErrorListener;

        public ParserErrorListener(IQueryErrorListener listener)
        {
            _queryErrorListener = listener;
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
            _queryErrorListener.OnCompilerError(line, charPositionInLine, msg);
            throw new ParseCanceledException(e);
        }
    }

    internal class LexerErrorListener : IAntlrErrorListener<int>
    {
        private readonly IQueryErrorListener _queryErrorListener;

        public LexerErrorListener(IQueryErrorListener listener)
        {
            _queryErrorListener = listener;
        }

        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer, 
            int offendingSymbol, 
            int line, 
            int charPositionInLine,
            string msg, 
            RecognitionException e)
        {
            _queryErrorListener.OnCompilerError(line, charPositionInLine, msg);
        }
    }

    [Export(typeof(IQueryCompiler))]
    public class QueryCompiler : IQueryCompiler
    {
        private readonly IQueryErrorListener _queryQueryErrorListener;
        private readonly IQueryFactory _queryFactory;
        private readonly IRuntime _runtime;

        public IQueryViewRepository Views { get; }

        [ImportingConstructor]
        public QueryCompiler(
            IQueryFactory queryFactory, 
            IRuntime runtime, 
            IQueryViewRepository queryViewRepository, 
            IQueryErrorListener queryErrorListener)
        {
            _queryFactory = queryFactory;
            _runtime = runtime;
            _queryQueryErrorListener = queryErrorListener;
            Views = queryViewRepository;
        }

        public IExecutableQuery Compile(TextReader inputQuery, IQueryErrorListener queryErrorListener)
        {
            // create all necessary components to parse a query
            var input = new AntlrInputStream(inputQuery);
            var lexer = new QueryLexer(input);
            lexer.AddErrorListener(new LexerErrorListener(queryErrorListener));

            var compiler = new QueryCompilationListener(
                _queryFactory,
                this,
                queryErrorListener,
                _runtime);

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new QueryParser(tokenStream);
            parser.BuildParseTree = false;
            parser.AddErrorListener(new ParserErrorListener(queryErrorListener));
            parser.AddParseListener(compiler);

            // parse and compile the query
            queryErrorListener.BeforeCompilation();
            IQuery result;
            try
            {
                parser.entry();
                result = compiler.Finish();
                result = result.WithText(input.ToString());
            }
            catch (ParseCanceledException)
            {
                // an error has already been reported 
                return null;
            }
            finally
            {
                queryErrorListener.AfterCompilation();
            }

            return result;
        }

        public IExecutableQuery Compile(string query)
        {
            return Compile(new StringReader(query), _queryQueryErrorListener);
        }
    }
}
