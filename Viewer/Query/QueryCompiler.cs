using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Viewer.Data;
using Viewer.Data.Formats.Attributes;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.Query
{
    public interface IQueryCompiler
    {
        /// <summary>
        /// Compile given query to an internal structure which can then be evaluated.
        /// </summary>
        /// <param name="input">Stream with the query</param>
        /// <returns>Compiled LINQ query</returns>
        IQuery Compile(TextReader input);
    }

    internal class QueryAttributes
    {
        /// <summary>
        /// Current query object
        /// </summary>
        public IQuery Query { get; set; }
        
        /// <summary>
        /// Expression computing value of this subexpression.
        /// </summary>
        public Expression Value { get; set; }
    }

    internal class QueryCompilerVisitor : IQueryVisitor<QueryAttributes>
    {
        private readonly IQueryFactory _queryFactory;
        private readonly IRuntime _runtime;

        private readonly Attribute _nullAttribute = new Attribute("", new IntValue(null));
        private readonly ParameterExpression _entityParameter = Expression.Parameter(typeof(IEntity), "entity");

        public QueryCompilerVisitor(IQueryFactory queryFactory, IRuntime runtime)
        {
            _queryFactory = queryFactory;
            _runtime = runtime;
        }

        public QueryAttributes Visit(IParseTree tree)
        {
            throw new NotImplementedException();
        }

        public QueryAttributes VisitChildren(IRuleNode node)
        {
            return node.Accept(this);
        }

        public QueryAttributes VisitTerminal(ITerminalNode node)
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
                case QueryLexer.ID:
                    var name = Expression.Constant(node.Symbol.Text);
                    var attributeGetter = typeof(IEntity).GetMethod("GetAttribute");
                    
                    expr = Expression.Property(
                        Expression.Coalesce(
                            Expression.Call(_entityParameter, attributeGetter, name), 
                            Expression.Constant(_nullAttribute)
                        ),
                        "Value");
                    break;
            }

            return new QueryAttributes
            {
                Value = expr
            };
        }

        public QueryAttributes VisitErrorNode(IErrorNode node)
        {
            throw new NotImplementedException();
        }

        public QueryAttributes VisitQuery(QueryParser.QueryContext context)
        {
            var source = context.source();
            var optionalWhere = context.optionalWhere();

            // empty query is valid 
            if (source == null)
            {
                return new QueryAttributes
                {
                    Query = _queryFactory.CreateQuery()
                };
            }

            var sourceResult = source.Accept(this);
            var optionalWhereResult = optionalWhere.Accept(this);

            // compile the where condition
            var filter = Expression.Lambda<Func<IEntity, bool>>(
                optionalWhereResult.Value,
                _entityParameter
            );
            var entityPredicate = filter.Compile();

            // compose the query
            var query = sourceResult.Query.Where(entityPredicate);
            return new QueryAttributes
            {
                Query = query
            };
        }

        public QueryAttributes VisitSource(QueryParser.SourceContext context)
        {
            var subquery = context.query();
            if (subquery != null)
            {
                return subquery.Accept(this);
            }

            var emptyQuery = _queryFactory.CreateQuery();
            var pattern = context.PATH_PATTERN().GetText();
            return new QueryAttributes
            {
                Query = emptyQuery.Path(pattern.Substring(1, pattern.Length - 2))
            };
        }

        public QueryAttributes VisitOptionalWhere(QueryParser.OptionalWhereContext context)
        {
            var condition = context.comparison();
            if (condition == null)
            {
                return new QueryAttributes
                {
                    Value = Expression.Constant(true)
                };
            }

            var result = condition.Accept(this);
            return new QueryAttributes
            {
                Value = Expression.Not(Expression.Property(result.Value, "IsNull"))
            };
        }

        public QueryAttributes VisitComparison(QueryParser.ComparisonContext context)
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
            return CompileBinaryOperator(op, left, right);
        }

        public QueryAttributes VisitExpression(QueryParser.ExpressionContext context)
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
            return CompileBinaryOperator(op, left, right);
        }

        public QueryAttributes VisitMultiplication(QueryParser.MultiplicationContext context)
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
            return CompileBinaryOperator(op, left, right);
        }

        public QueryAttributes VisitFactor(QueryParser.FactorContext context)
        {
            var comparison = context.comparison();
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

            var id = context.ID();
            return id.Accept(this);
        }

        private QueryAttributes CompileBinaryOperator(string op, QueryAttributes lhs, QueryAttributes rhs)
        {
            var expr = RuntimeCall(op, lhs.Value, rhs.Value);

            return new QueryAttributes
            {
                Value = expr
            };
        }

        /// <summary>
        /// Create a runtime function call expression
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private Expression RuntimeCall(string functionName, params Expression[] arguments)
        {
            var runtimeCall = _runtime.GetType().GetMethod("FindAndCall");
            return Expression.Call(
                Expression.Constant(_runtime),
                runtimeCall,
                Expression.Constant(functionName),
                Expression.NewArrayInit(typeof(BaseValue), arguments));
        }
    }

    [Export(typeof(IQueryCompiler))]
    public class QueryCompiler : IQueryCompiler
    {
        private readonly IQueryFactory _queryFactory;
        private readonly IRuntime _runtime;

        [ImportingConstructor]
        public QueryCompiler(IQueryFactory queryFactory, IRuntime runtime)
        {
            _queryFactory = queryFactory;
            _runtime = runtime;
        }

        public IQuery Compile(TextReader inputQuery)
        {
            // create all necessary components to parse a query
            var input = new AntlrInputStream(inputQuery);
            var lexer = new QueryLexer(input);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new QueryParser(tokenStream);

            // parse and compile the query
            var query = parser.query();
            var compiler = new QueryCompilerVisitor(_queryFactory, _runtime);
            var result = query.Accept(compiler);

            return result.Query;
        }
    }
}
