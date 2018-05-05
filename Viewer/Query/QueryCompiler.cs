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
    
    internal class QueryCompilerVisitor : IQueryVisitor<Expression>
    {
        private readonly IQueryFactory _queryFactory;
        private readonly IRuntime _runtime;

        private readonly ParameterExpression _entityParameter = Expression.Parameter(typeof(IEntity), "entity");
        private readonly Attribute _nullAttribute = new Attribute("", new IntValue(null));
        private readonly List<ValueOrder> _order = new List<ValueOrder>();

        public IQuery Query { get; private set; }

        public QueryCompilerVisitor(IQueryFactory queryFactory, IRuntime runtime)
        {
            _queryFactory = queryFactory;
            _runtime = runtime;
            Query = _queryFactory.CreateQuery();
        }

        public Expression Visit(IParseTree tree)
        {
            throw new NotImplementedException();
        }

        public Expression VisitChildren(IRuleNode node)
        {
            throw new NotImplementedException();
        }

        public Expression VisitTerminal(ITerminalNode node)
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

            return expr;
        }

        public Expression VisitErrorNode(IErrorNode node)
        {
            throw new NotImplementedException();
        }

        public Expression VisitQuery(QueryParser.QueryContext context)
        {
            var source = context.source();
            var optionalWhere = context.optionalWhere();
            var optionalOrderBy = context.optionalOrderBy();

            // empty query is valid 
            if (source == null)
            {
                return null;
            }

            // visit children
            source.Accept(this);
            var optionalWhereResult = optionalWhere.Accept(this);
            optionalOrderBy.Accept(this);

            // compile the where condition
            var filter = Expression.Lambda<Func<IEntity, bool>>(
                optionalWhereResult,
                _entityParameter
            );
            var entityPredicate = filter.Compile();

            // compose the query
            Query = Query.Where(entityPredicate);
            Query = Query.SetComparer(new EntityComparer(_order));
            return null;
        }

        public Expression VisitSource(QueryParser.SourceContext context)
        {
            var subquery = context.query();
            if (subquery != null)
            {
                return subquery.Accept(this);
            }
            
            var pattern = context.STRING().GetText();
            Query = Query.Path(pattern.Substring(1, pattern.Length - 2));
            return null;
        }

        public Expression VisitOptionalWhere(QueryParser.OptionalWhereContext context)
        {
            var condition = context.comparison();
            if (condition == null)
            {
                return Expression.Constant(true);
            }

            var result = condition.Accept(this);
            return Expression.Not(Expression.Property(result, "IsNull"));
        }

        public Expression VisitOptionalOrderBy(QueryParser.OptionalOrderByContext context)
        {
            var orderByList = context.orderByList();
            if (orderByList == null)
            {
                return null;
            }
            return orderByList.Accept(this);
        }

        public Expression VisitOrderByList(QueryParser.OrderByListContext context)
        {
            foreach (var child in context.orderByKey())
            {
                child.Accept(this);
            }

            return null;
        }

        public Expression VisitOrderByKey(QueryParser.OrderByKeyContext context)
        {
            var valueExpr = context.comparison().Accept(this);
            var valueGetterExpr = Expression.Lambda<Func<IEntity, BaseValue>>(
                valueExpr,
                _entityParameter
            );
            var valueGetter = valueGetterExpr.Compile();
            var direction = context.optionalDirection().GetText() == "DESC" ? -1 : 1;

            _order.Add(new ValueOrder
            {
                Getter = valueGetter,
                Direction = direction
            });

            return null;
        }

        public Expression VisitOptionalDirection(QueryParser.OptionalDirectionContext context)
        {
            return null;
        }

        public Expression VisitComparison(QueryParser.ComparisonContext context)
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

        public Expression VisitExpression(QueryParser.ExpressionContext context)
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

        public Expression VisitMultiplication(QueryParser.MultiplicationContext context)
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

        public Expression VisitFactor(QueryParser.FactorContext context)
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

            var stringValue = context.STRING();
            if (stringValue != null)
            {
                return stringValue.Accept(this);
            }

            var id = context.ID();
            var argumentList = context.argumentList();
            if (argumentList != null)
            {
                // function call
                var arguments = argumentList.Accept(this);
                return RuntimeCall(id.GetText(), arguments);
            }

            // attribute identifier
            return id.Accept(this);
        }

        public Expression VisitArgumentList(QueryParser.ArgumentListContext context)
        {
            var argumentCount = context.comparison().Length;
            var arguments = new Expression[argumentCount];
            for (var i = 0; i < argumentCount; ++i)
            {
                arguments[i] = context.comparison(i).Accept(this);
            }
            return Expression.NewArrayInit(typeof(BaseValue), arguments);
        }

        private Expression CompileBinaryOperator(string op, Expression lhs, Expression rhs)
        {
            var expr = RuntimeCall(op, lhs, rhs);
            return expr;
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
            query.Accept(compiler);
            return compiler.Query;
        }
    }
}
