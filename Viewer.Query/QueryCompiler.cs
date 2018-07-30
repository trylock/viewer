using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Viewer.Data;
using Viewer.Data.Formats.Attributes;
using Attribute = Viewer.Data.Attribute;

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
        /// <param name="errorListener">Error reporter</param>
        /// <returns>Compiled query</returns>
        IQuery Compile(TextReader input, IErrorListener errorListener);
    }

    internal class CompilationResult
    {
        /// <summary>
        /// Expression which computes a value
        /// </summary>
        public Expression Value { get; set; }

        /// <summary>
        /// Subquery
        /// </summary>
        public IQuery Query { get; set; }

        /// <summary>
        /// Value getter for construction of a comparer
        /// </summary>
        public ValueOrder Order { get; set; }

        /// <summary>
        /// Entity comparer
        /// </summary>
        public IComparer<IEntity> Comparer { get; set; }
    }
    
    internal class QueryCompilerVisitor : IQueryVisitor<CompilationResult>
    {
        private readonly IQueryCompiler _queryCompiler;
        private readonly IErrorListener _errorListener;
        private readonly IQueryFactory _queryFactory;
        private readonly IRuntime _runtime;

        private readonly ParameterExpression _entityParameter = Expression.Parameter(typeof(IEntity), "entity");
        private readonly Attribute _nullAttribute = new Attribute("", new IntValue(null));
        
        public QueryCompilerVisitor(IQueryFactory queryFactory, IRuntime runtime, IQueryCompiler compiler, IErrorListener errorListener)
        {
            _queryFactory = queryFactory;
            _runtime = runtime;
            _queryCompiler = compiler;
            _errorListener = errorListener;
        }

        public IQuery Compile(IParseTree tree)
        {
            var query = Visit(tree).Query;
            return query.WithText(tree.GetText());
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
            Expression expr = null;
            switch (node.Symbol.Type)
            {
                case QueryLexer.INT:
                    var intValue = int.Parse(node.Symbol.Text);
                    expr = Expression.Constant(new IntValue(intValue));
                    break;
                case QueryLexer.REAL:
                    var doubleValue = double.Parse(node.Symbol.Text);
                    expr = Expression.Constant(new RealValue(doubleValue));
                    break;
                case QueryLexer.STRING:
                    var stringValue = node.Symbol.Text.Substring(1, node.Symbol.Text.Length - 2);
                    expr = Expression.Constant(new StringValue(stringValue));
                    break;
                case QueryLexer.COMPLEX_ID:
                    expr = CompileAttributeAccess(node.Symbol.Text.Substring(1, node.Symbol.Text.Length - 2));
                    break;
                case QueryLexer.ID:
                    expr = CompileAttributeAccess(node.Symbol.Text);
                    break;
            }

            return new CompilationResult{ Value = expr };
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

            // parser ORDER BY
            var orderBy = context.optionalOrderBy();
            if (orderBy != null)
            {
                var comparer = context.optionalOrderBy().Accept(this).Comparer;
                query = query.WithComparer(comparer);
            }

            return new CompilationResult{ Query = query };
        }

        public CompilationResult VisitUnorderedQuery(QueryParser.UnorderedQueryContext context)
        {
            var source = context.source();
            var optionalWhere = context.optionalWhere();

            // visit children
            var query = source.Accept(this).Query;
            var optionalWhereResult = optionalWhere.Accept(this).Value;

            // compile the where condition
            var filter = Expression.Lambda<Func<IEntity, bool>>(
                optionalWhereResult,
                _entityParameter
            );
            var entityPredicate = filter.Compile();

            return new CompilationResult{ Query = query.Where(entityPredicate) };
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
                var view = _queryCompiler.Views[viewId.GetText()];
                query = _queryCompiler.Compile(new StringReader(view), _errorListener);
                if (query == null)
                {
                    // compilation of the subquery failed
                    throw new ParseCanceledException();
                }
                return new CompilationResult{ Query = query };
            }
            
            // set path pattern
            var pattern = context.STRING();
            var pathPattern = pattern.GetText().Substring(1, pattern.GetText().Length - 2);
            query = _queryFactory.CreateQuery(pathPattern);
            return new CompilationResult{ Query = query };
        }

        public CompilationResult VisitOptionalWhere(QueryParser.OptionalWhereContext context)
        {
            var condition = context.predicate();
            if (condition == null)
            {
                return new CompilationResult{ Value = Expression.Constant(true) };
            }

            var predicate = condition.Accept(this).Value;
            return new CompilationResult
            {
                Value = Expression.Not(Expression.Property(predicate, "IsNull"))
            };
        }

        public CompilationResult VisitOptionalOrderBy(QueryParser.OptionalOrderByContext context)
        {
            var orderByList = context.orderByList();
            if (orderByList != null)
            {
                return orderByList.Accept(this);
            }
            
            return new CompilationResult
            {
                Comparer = new EntityComparer()
            };
        }

        public CompilationResult VisitOrderByList(QueryParser.OrderByListContext context)
        {
            var orderByList = new List<ValueOrder>();
            foreach (var child in context.orderByKey())
            {
                var item = child.Accept(this).Order;
                orderByList.Add(item);
            }

            return new CompilationResult
            {
                Comparer = new EntityComparer(orderByList)
            };
        }

        public CompilationResult VisitOrderByKey(QueryParser.OrderByKeyContext context)
        {
            var valueExpr = context.comparison().Accept(this).Value;
            var valueGetterExpr = Expression.Lambda<Func<IEntity, BaseValue>>(
                valueExpr,
                _entityParameter
            );
            var valueGetter = valueGetterExpr.Compile();
            var direction = context.optionalDirection().GetText().ToLowerInvariant() == "desc" ? -1 : 1;

            return new CompilationResult
            {
                Order = new ValueOrder
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
            var lhs = left.Accept(this).Value;
            return new CompilationResult
            {
                Value = RuntimeCall("or", lhs, rhs)
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
            var lhs = left.Accept(this).Value;
            return new CompilationResult
            {
                Value = RuntimeCall("and", lhs, rhs)
            };
        }

        public CompilationResult VisitLiteral(QueryParser.LiteralContext context)
        {
            var value = context.comparison().Accept(this).Value;
            if (context.NOT() != null)
            {
                return new CompilationResult
                {
                    Value = RuntimeCall("not", new[]{ value })
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
            var op = context.REL_OP().Symbol.Text;
            return new CompilationResult
            {
                Value = CompileBinaryOperator(op, left.Value, right.Value)
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
            var op = context.ADD_SUB().Symbol.Text;
            return new CompilationResult
            {
                Value = CompileBinaryOperator(op, left.Value, right.Value)
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
            var op = context.MULT_DIV().Symbol.Text;
            return new CompilationResult
            {
                Value = CompileBinaryOperator(op, left.Value, right.Value)
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
                var arguments = argumentList.Accept(this).Value;
                return new CompilationResult
                {
                    Value = RuntimeCall(id.GetText(), arguments)
                };
            }

            // attribute identifier
            return id.Accept(this);
        }

        public CompilationResult VisitArgumentList(QueryParser.ArgumentListContext context)
        {
            var argumentCount = context.comparison().Length;
            var arguments = new Expression[argumentCount];
            for (var i = 0; i < argumentCount; ++i)
            {
                arguments[i] = context.comparison(i).Accept(this).Value;
            }

            return new CompilationResult
            {
                Value = Expression.NewArrayInit(typeof(BaseValue), arguments)
            };
        }

        private Expression CompileBinaryOperator(string op, Expression lhs, Expression rhs)
        {
            var expr = RuntimeCall(op, lhs, rhs);
            return expr;
        }

        /// <summary>
        /// Compile attribute value getter.
        /// Generated code will also handle missing attributes.
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        private Expression CompileAttributeAccess(string attributeName)
        {
            var name = Expression.Constant(attributeName);
            var attributeGetter = typeof(IEntity).GetMethod("GetAttribute");

            return Expression.Property(
                Expression.Coalesce(
                    Expression.Call(_entityParameter, attributeGetter, name),
                    Expression.Constant(_nullAttribute)
                ),
                "Value");
        }

        /// <summary>
        /// Create a runtime function call expression
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private Expression RuntimeCall(string functionName, params Expression[] arguments)
        {
            return RuntimeCall(functionName, Expression.NewArrayInit(typeof(BaseValue), arguments));
        }

        private Expression RuntimeCall(string functionName, Expression argumentArray)
        {
            if (argumentArray.Type != typeof(BaseValue[]))
                throw new ArgumentOutOfRangeException(nameof(argumentArray));

            var runtimeCall = _runtime.GetType().GetMethod("FindAndCall");
            return Expression.Call(
                Expression.Constant(_runtime),
                runtimeCall,
                Expression.Constant(functionName),
                argumentArray);
        }
    }
    
    internal class ParserErrorListener : IAntlrErrorListener<IToken>
    {
        private readonly IErrorListener _errorListener;

        public ParserErrorListener(IErrorListener listener)
        {
            _errorListener = listener;
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
            _errorListener.ReportError(line, charPositionInLine, msg);
            throw new ParseCanceledException(e);
        }
    }

    internal class LexerErrorListener : IAntlrErrorListener<int>
    {
        private readonly IErrorListener _errorListener;

        public LexerErrorListener(IErrorListener listener)
        {
            _errorListener = listener;
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
            _errorListener.ReportError(line, charPositionInLine, msg);
        }
    }

    [Export(typeof(IQueryCompiler))]
    public class QueryCompiler : IQueryCompiler
    {
        private readonly IQueryFactory _queryFactory;
        private readonly IRuntime _runtime;

        public IQueryViewRepository Views { get; }

        [ImportingConstructor]
        public QueryCompiler(IQueryFactory queryFactory, IRuntime runtime, IQueryViewRepository queryViewRepository)
        {
            _queryFactory = queryFactory;
            _runtime = runtime;
            Views = queryViewRepository;
        }

        public IQuery Compile(TextReader inputQuery, IErrorListener errorListener)
        {
            // create all necessary components to parse a query
            var input = new AntlrInputStream(inputQuery);
            var lexer = new QueryLexer(input);
            lexer.AddErrorListener(new LexerErrorListener(errorListener));

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new QueryParser(tokenStream);
            parser.AddErrorListener(new ParserErrorListener(errorListener));

            // parse and compile the query
            errorListener.BeforeCompilation();
            IQuery result;
            try
            {
                var query = parser.queryExpression();
                var compiler = new QueryCompilerVisitor(_queryFactory, _runtime, this, errorListener);
                result = compiler.Compile(query);
            }
            catch (ParseCanceledException)
            {
                // an error was reported already
                return null;
            }
            finally
            {
                errorListener.AfterCompilation();
            }

            return result;
        }
    }
}
