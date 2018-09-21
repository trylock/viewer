using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.Query.Expressions
{
    /// <inheritdoc />
    /// <summary>
    /// Attribute access expression in a query
    /// </summary>
    internal class AttributeAccessExpression : ValueExpression
    {
        private static readonly Attribute NullAttribute = 
            new Attribute("", new IntValue(null), AttributeSource.Custom);

        /// <summary>
        /// Name of the attribute
        /// </summary>
        public string Name { get; }

        public AttributeAccessExpression(int line, int column, string name) : base(line, column)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString()
        {
            return Name;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override Expression ToExpressionTree(
            ParameterExpression entityParameter,
            IRuntime runtime)
        {
            var name = Expression.Constant(Name);
            var attributeGetter = typeof(IEntity).GetMethod("GetAttribute");

            return Expression.Property(
                Expression.Coalesce(
                    Expression.Call(entityParameter, attributeGetter, name),
                    Expression.Constant(NullAttribute)
                ),
                "Value");
        }
    }
}
