//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from C:\projects\Viewer\src\Viewer.Query\QueryParser.g4 by ANTLR 4.7.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace Viewer.Query {

using Antlr4.Runtime.Misc;
using IErrorNode = Antlr4.Runtime.Tree.IErrorNode;
using ITerminalNode = Antlr4.Runtime.Tree.ITerminalNode;
using IToken = Antlr4.Runtime.IToken;
using ParserRuleContext = Antlr4.Runtime.ParserRuleContext;

/// <summary>
/// This class provides an empty implementation of <see cref="IQueryParserListener"/>,
/// which can be extended to create a listener which only needs to handle a subset
/// of the available methods.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.7.1")]
[System.CLSCompliant(false)]
public partial class QueryParserBaseListener : IQueryParserListener {
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.entry"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterEntry([NotNull] QueryParser.EntryContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.entry"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitEntry([NotNull] QueryParser.EntryContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.queryExpression"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterQueryExpression([NotNull] QueryParser.QueryExpressionContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.queryExpression"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitQueryExpression([NotNull] QueryParser.QueryExpressionContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.intersection"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterIntersection([NotNull] QueryParser.IntersectionContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.intersection"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitIntersection([NotNull] QueryParser.IntersectionContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.queryFactor"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterQueryFactor([NotNull] QueryParser.QueryFactorContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.queryFactor"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitQueryFactor([NotNull] QueryParser.QueryFactorContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.query"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterQuery([NotNull] QueryParser.QueryContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.query"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitQuery([NotNull] QueryParser.QueryContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.unorderedQuery"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterUnorderedQuery([NotNull] QueryParser.UnorderedQueryContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.unorderedQuery"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitUnorderedQuery([NotNull] QueryParser.UnorderedQueryContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.source"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterSource([NotNull] QueryParser.SourceContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.source"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitSource([NotNull] QueryParser.SourceContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.optionalWhere"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterOptionalWhere([NotNull] QueryParser.OptionalWhereContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.optionalWhere"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitOptionalWhere([NotNull] QueryParser.OptionalWhereContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.optionalOrderBy"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterOptionalOrderBy([NotNull] QueryParser.OptionalOrderByContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.optionalOrderBy"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitOptionalOrderBy([NotNull] QueryParser.OptionalOrderByContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.orderByList"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterOrderByList([NotNull] QueryParser.OrderByListContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.orderByList"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitOrderByList([NotNull] QueryParser.OrderByListContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.orderByKey"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterOrderByKey([NotNull] QueryParser.OrderByKeyContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.orderByKey"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitOrderByKey([NotNull] QueryParser.OrderByKeyContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.optionalDirection"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterOptionalDirection([NotNull] QueryParser.OptionalDirectionContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.optionalDirection"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitOptionalDirection([NotNull] QueryParser.OptionalDirectionContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.predicate"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterPredicate([NotNull] QueryParser.PredicateContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.predicate"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitPredicate([NotNull] QueryParser.PredicateContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.conjunction"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterConjunction([NotNull] QueryParser.ConjunctionContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.conjunction"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitConjunction([NotNull] QueryParser.ConjunctionContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.literal"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterLiteral([NotNull] QueryParser.LiteralContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.literal"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitLiteral([NotNull] QueryParser.LiteralContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.comparison"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterComparison([NotNull] QueryParser.ComparisonContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.comparison"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitComparison([NotNull] QueryParser.ComparisonContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.expression"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterExpression([NotNull] QueryParser.ExpressionContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.expression"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitExpression([NotNull] QueryParser.ExpressionContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.multiplication"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterMultiplication([NotNull] QueryParser.MultiplicationContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.multiplication"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitMultiplication([NotNull] QueryParser.MultiplicationContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.factor"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterFactor([NotNull] QueryParser.FactorContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.factor"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitFactor([NotNull] QueryParser.FactorContext context) { }
	/// <summary>
	/// Enter a parse tree produced by <see cref="QueryParser.argumentList"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void EnterArgumentList([NotNull] QueryParser.ArgumentListContext context) { }
	/// <summary>
	/// Exit a parse tree produced by <see cref="QueryParser.argumentList"/>.
	/// <para>The default implementation does nothing.</para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	public virtual void ExitArgumentList([NotNull] QueryParser.ArgumentListContext context) { }

	/// <inheritdoc/>
	/// <remarks>The default implementation does nothing.</remarks>
	public virtual void EnterEveryRule([NotNull] ParserRuleContext context) { }
	/// <inheritdoc/>
	/// <remarks>The default implementation does nothing.</remarks>
	public virtual void ExitEveryRule([NotNull] ParserRuleContext context) { }
	/// <inheritdoc/>
	/// <remarks>The default implementation does nothing.</remarks>
	public virtual void VisitTerminal([NotNull] ITerminalNode node) { }
	/// <inheritdoc/>
	/// <remarks>The default implementation does nothing.</remarks>
	public virtual void VisitErrorNode([NotNull] IErrorNode node) { }
}
} // namespace Viewer.Query