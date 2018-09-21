using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
        IQuery Compile(TextReader input, IQueryErrorListener queryErrorListener);

        /// <summary>
        /// Same as Compile(new StringReader(query), defaultQueryErrorListener)
        /// </summary>
        /// <param name="query">Query to compile</param>
        /// <returns>Compiled query or null if there were errors during compilation</returns>
        IQuery Compile(string query);
    }

    internal class CompilationResult
    {
        /// <summary>
        /// Expression which computes a value
        /// </summary>
        public ValueExpression Value { get; set; }

        /// <summary>
        /// List of values (returned by the argument list rule)
        /// </summary>
        public List<ValueExpression> Values { get; set; }

        /// <summary>
        /// Subquery
        /// </summary>
        public IQuery Query { get; set; }

        /// <summary>
        /// Textual representation of this part of the query. It can be null. Only some rules
        /// provide a textual representation.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Value getter for construction of a comparer
        /// </summary>
        public SortParameter Order { get; set; }

        /// <summary>
        /// Entity comparer
        /// </summary>
        public IComparer<IEntity> Comparer { get; set; }
    }
    
    internal class QueryCompilerVisitor : IQueryVisitor<CompilationResult>
    {
        private readonly IQueryCompiler _queryCompiler;
        private readonly IQueryErrorListener _queryErrorListener;
        private readonly IQueryFactory _queryFactory;
        private readonly IRuntime _runtime;
        
        public QueryCompilerVisitor(
            IQueryFactory queryFactory, 
            IRuntime runtime, 
            IQueryCompiler compiler, 
            IQueryErrorListener queryErrorListener)
        {
            _queryFactory = queryFactory;
            _runtime = runtime;
            _queryCompiler = compiler;
            _queryErrorListener = queryErrorListener;
        }

        public IQuery Compile(IParseTree tree)
        {
            var query = Visit(tree).Query;
            return query;
        }

        public CompilationResult Visit(IParseTree tree)
        {
            return tree.Accept(this);
        }

        public CompilationResult VisitChildren(IRuleNode node)
        {
            throw new NotImplementedException();
        }

        public CompilationResult VisitErrorNode(IErrorNode node)
        {
            throw new NotImplementedException();
        }

        public CompilationResult VisitTerminal(ITerminalNode node)
        {
            var line = node.Symbol.Line;
            var column = node.Symbol.Column;
            ValueExpression expr = null;
            switch (node.Symbol.Type)
            {
                case QueryLexer.INT:
                    var intValue = int.Parse(node.Symbol.Text);
                    expr = new ConstantExpression(line, column, new IntValue(intValue));
                    break;
                case QueryLexer.REAL:
                    var doubleValue = double.Parse(node.Symbol.Text);
                    expr = new ConstantExpression(line, column, new RealValue(doubleValue));
                    break;
                case QueryLexer.STRING:
                    var stringValue = node.Symbol.Text.Substring(1, node.Symbol.Text.Length - 2);
                    expr = new ConstantExpression(line, column, new StringValue(stringValue));
                    break;
                case QueryLexer.COMPLEX_ID:
                    expr = new AttributeAccessExpression(
                        line, 
                        column, 
                        node.Symbol.Text.Substring(1, node.Symbol.Text.Length - 2));
                    break;
                case QueryLexer.ID:
                    expr = new AttributeAccessExpression(line, column, node.Symbol.Text);
                    break;
            }

            return new CompilationResult{ Value = expr };
        }

        public CompilationResult VisitEntry(QueryParser.EntryContext context)
        {
            return context.queryExpression().Accept(this);
        }

        public CompilationResult VisitQueryExpression(QueryParser.QueryExpressionContext context)
        {
            if (context.queryExpression() == null)
            {
                return context.intersection().Accept(this);
            }

            // parse UNION, EXCEPT
            var left = context.queryExpression().Accept(this);
            var right = context.intersection().Accept(this);
            var op = context.UNION_EXCEPT().Symbol.Text;

            var result = StringComparer.InvariantCultureIgnoreCase.Compare(op, "union") == 0 ? 
                left.Query.Union(right.Query) : 
                left.Query.Except(right.Query);
            return new CompilationResult{ Query = result };
        }

        public CompilationResult VisitIntersection(QueryParser.IntersectionContext context)
        {
            // parse INTERSECT
            IQuery query = null;
            foreach (var result in context.queryFactor())
            {
                var subquery = result.Accept(this).Query;

                query = query == null ? 
                    subquery : 
                    query.Intersect(subquery);
            }
            return new CompilationResult{ Query = query };
        }

        public CompilationResult VisitQueryFactor(QueryParser.QueryFactorContext context)
        {
            var query = context.query();
            if (query != null)
            {
                return query.Accept(this);
            }

            return context.queryExpression().Accept(this);
        }

        public CompilationResult VisitQuery(QueryParser.QueryContext context)
        {
            // parse unordered query
            var query = context.unorderedQuery().Accept(this).Query;

            // parse ORDER BY
            var orderBy = context.optionalOrderBy();
            if (orderBy != null)
            {
                var result = context.optionalOrderBy().Accept(this);
                if (result != null)
                {
                    query = query.WithComparer(result.Comparer, result.Text);
                }
            }

            return new CompilationResult{ Query = query };
        }

        public CompilationResult VisitUnorderedQuery(QueryParser.UnorderedQueryContext context)
        {
            var source = context.source();
            var optionalWhere = context.optionalWhere();

            // visit children
            var sourceResult = source.Accept(this);
            var optionalWhereResult = optionalWhere.Accept(this);
            if (optionalWhereResult == null)
            {
                return sourceResult; // the query does not have a WHERE part
            }

            // compile the where condition
            var entityPredicate = optionalWhereResult.Value.CompilePredicate(_runtime, _queryErrorListener);
            return new CompilationResult
            {
                Query = sourceResult.Query.Where(entityPredicate, optionalWhereResult.Text)
            };
        }

        public CompilationResult VisitSource(QueryParser.SourceContext context)
        {
            IQuery query = null;
            var subquery = context.queryExpression();
            if (subquery != null)
            {
                // compile subquery
                query = subquery.Accept(this).Query;
                return new CompilationResult{ Query = query };
            }

            var viewId = context.ID();
            if (viewId != null)
            {
                var view = _queryCompiler.Views.Find(viewId.GetText());
                query = _queryCompiler.Compile(new StringReader(view.Text), _queryErrorListener);
                if (query == null)
                {
                    // compilation of the subquery failed
                    throw new ParseCanceledException();
                }

                return new CompilationResult
                {
                    Query = query.View(view.Name)
                };
            }
            
            // set path pattern
            var pattern = context.STRING();
            var pathPattern = pattern.GetText().Substring(1, pattern.GetText().Length - 2);
            try
            {
                query = _queryFactory.CreateQuery(pathPattern);
                return new CompilationResult {Query = query};
            }
            catch (ArgumentException e) // pathPattern contains invalid characters
            {
                _queryErrorListener.OnCompilerError(
                    pattern.Symbol.Line, 
                    pattern.Symbol.Column, 
                    "Invalid characters in path pattern.");
                throw new ParseCanceledException(e);
            }
        }

        public CompilationResult VisitOptionalWhere(QueryParser.OptionalWhereContext context)
        {
            var condition = context.predicate();
            if (condition == null)
            {
                return null;
            }

            var predicate = condition.Accept(this).Value;
            return new CompilationResult
            {
                Value = predicate,
                Text = condition.Start.InputStream.GetText(new Interval(
                    condition.Start.StartIndex,
                    condition.Stop.StopIndex
                ))
            };
        }

        public CompilationResult VisitOptionalOrderBy(QueryParser.OptionalOrderByContext context)
        {
            var orderByList = context.orderByList();
            if (orderByList != null)
            {
                return orderByList.Accept(this);
            }

            return null;
        }

        public CompilationResult VisitOrderByList(QueryParser.OrderByListContext context)
        {
            var orderByList = new List<SortParameter>();
            foreach (var child in context.orderByKey())
            {
                var item = child.Accept(this).Order;
                orderByList.Add(item);
            }

            return new CompilationResult
            {
                Comparer = new EntityComparer(orderByList),
                Text = context.Start.InputStream.GetText(new Interval(
                    context.Start.StartIndex,
                    context.Stop.StopIndex
                ))
            };
        }

        public CompilationResult VisitOrderByKey(QueryParser.OrderByKeyContext context)
        {
            var valueExpr = context.comparison().Accept(this).Value;
            var valueGetter = valueExpr.CompileFunction(_runtime, _queryErrorListener);
            var direction = context.optionalDirection().GetText().ToLowerInvariant() == "desc" ? -1 : 1;

            return new CompilationResult
            {
                Order = new SortParameter
                {
                    Getter = valueGetter,
                    Direction = direction
                }
            };
        }

        public CompilationResult VisitOptionalDirection(QueryParser.OptionalDirectionContext context)
        {
            return null;
        }

        public CompilationResult VisitPredicate(QueryParser.PredicateContext context)
        {
            var right = context.conjunction();
            var rhs = right.Accept(this).Value;

            // just return the computed value if this is a production: predicate -> conjunction
            var left = context.predicate();
            if (left == null)
            {
                return new CompilationResult{ Value = rhs };
            }

            // compile OR
            var symbol = context.OR().Symbol;
            var lhs = left.Accept(this).Value;
            return new CompilationResult
            {
                Value = new OrExpression(symbol.Line, symbol.Column, lhs, rhs)
            };
        }

        public CompilationResult VisitConjunction(QueryParser.ConjunctionContext context)
        {
            // compile right subexpression
            var right = context.literal();
            var rhs = right.Accept(this).Value;

            // if this is a production: conjunction -> comparison
            var left = context.conjunction();
            if (left == null)
            {
                return new CompilationResult{ Value = rhs };
            }
            
            // compile AND
            var symbol = context.AND().Symbol;
            var lhs = left.Accept(this).Value;
            return new CompilationResult
            {
                Value = new AndExpression(symbol.Line, symbol.Column, lhs, rhs)
            };
        }

        public CompilationResult VisitLiteral(QueryParser.LiteralContext context)
        {
            var value = context.comparison().Accept(this).Value;
            if (context.NOT() != null)
            {
                var symbol = context.NOT().Symbol;
                return new CompilationResult
                {
                    Value = new NotExpression(symbol.Line, symbol.Column, value)
                };
            }

            return new CompilationResult{ Value = value };
        }

        public CompilationResult VisitComparison(QueryParser.ComparisonContext context)
        {
            var lhs = context.expression(0);
            var rhs = context.expression(1);
            if (rhs == null)
            {
                return lhs.Accept(this);
            }

            var left = lhs.Accept(this);
            var right = rhs.Accept(this);
            var op = context.REL_OP().Symbol;
            BinaryOperatorExpression value = null;
            switch (op.Text)
            {
                case "<":
                    value = new LessThanOperator(op.Line, op.Column, left.Value, right.Value);
                    break;
                case "<=":
                    value = new LessThanOrEqualOperator(op.Line, op.Column, left.Value, right.Value);
                    break;
                case "!=":
                    value = new NotEqualOperator(op.Line, op.Column, left.Value, right.Value);
                    break;
                case "=":
                    value = new EqualOperatorOperator(op.Line, op.Column, left.Value, right.Value);
                    break;
                case ">":
                    value = new GreaterThanOperator(op.Line, op.Column, left.Value, right.Value);
                    break;
                case ">=":
                    value = new GreaterThanOrEqualOperator(op.Line, op.Column, left.Value, right.Value);
                    break;
            }

            Trace.Assert(value != null, "Invalid comparison operator " + op.Text);

            return new CompilationResult
            {
                Value = value
            };
        }

        public CompilationResult VisitExpression(QueryParser.ExpressionContext context)
        {
            var lhs = context.expression();
            var rhs = context.multiplication();
            if (lhs == null)
            {
                return rhs.Accept(this);
            }

            var left = lhs.Accept(this);
            var right = rhs.Accept(this);
            var op = context.ADD_SUB().Symbol;

            BinaryOperatorExpression value = null;
            switch (op.Text)
            {
                case "+":
                    value = new AdditionExpression(op.Line, op.Column, left.Value, right.Value);
                    break;
                case "-":
                    value = new SubtractionExpression(op.Line, op.Column, left.Value, right.Value);
                    break;
            }

            Trace.Assert(value != null, "Invalid addition operator " + op.Text);

            return new CompilationResult
            {
                Value = value
            };
        }

        public CompilationResult VisitMultiplication(QueryParser.MultiplicationContext context)
        {
            var lhs = context.multiplication();
            var rhs = context.factor();
            if (lhs == null)
            {
                return rhs.Accept(this);
            }
            
            var left = lhs.Accept(this);
            var right = rhs.Accept(this);
            var op = context.MULT_DIV().Symbol;

            BinaryOperatorExpression value = null;
            switch (op.Text)
            {
                case "*":
                    value = new MultiplicationExpression(op.Line, op.Column, left.Value, right.Value);
                    break;
                case "/":
                    value = new DivisionExpression(op.Line, op.Column, left.Value, right.Value);
                    break;
            }

            Trace.Assert(value != null, "Invalid multiplication operator " + op.Text);

            return new CompilationResult
            {
                Value = value
            };
        }

        public CompilationResult VisitFactor(QueryParser.FactorContext context)
        {
            var comparison = context.predicate();
            if (comparison != null)
            {
                return comparison.Accept(this);
            }

            var intValue = context.INT();
            if (intValue != null)
            {
                return intValue.Accept(this);
            }

            var doubleValue = context.REAL();
            if (doubleValue != null)
            {
                return doubleValue.Accept(this);
            }

            var stringValue = context.STRING();
            if (stringValue != null)
            {
                return stringValue.Accept(this);
            }

            var complexId = context.COMPLEX_ID();
            if (complexId != null)
            {
                return complexId.Accept(this);
            }

            var id = context.ID();
            var argumentList = context.argumentList(); 
            if (argumentList != null)
            {
                // function call
                var arguments = argumentList.Accept(this).Values;
                return new CompilationResult
                {
                    Value = new FunctionCallExpression(id.Symbol.Line, id.Symbol.Column, id.GetText(), arguments)
                };
            }

            // attribute identifier
            return id.Accept(this);
        }

        public CompilationResult VisitArgumentList(QueryParser.ArgumentListContext context)
        {
            var argumentCount = context.comparison().Length;
            var arguments = new List<ValueExpression>();
            for (var i = 0; i < argumentCount; ++i)
            {
                var argument = context.comparison(i).Accept(this).Value;
                arguments.Add(argument);
            }

            return new CompilationResult
            {
                Values = arguments
            };
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

        public IQuery Compile(TextReader inputQuery, IQueryErrorListener queryErrorListener)
        {
            // create all necessary components to parse a query
            var input = new AntlrInputStream(inputQuery);
            var lexer = new QueryLexer(input);
            lexer.AddErrorListener(new LexerErrorListener(queryErrorListener));

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new QueryParser(tokenStream);
            parser.AddErrorListener(new ParserErrorListener(queryErrorListener));

            // parse and compile the query
            queryErrorListener.BeforeCompilation();
            IQuery result;
            try
            {
                var query = parser.entry();
                var compiler = new QueryCompilerVisitor(_queryFactory, _runtime, this, queryErrorListener);
                result = compiler.Compile(query);
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

        public IQuery Compile(string query)
        {
            return Compile(new StringReader(query), _queryQueryErrorListener);
        }
    }
}
