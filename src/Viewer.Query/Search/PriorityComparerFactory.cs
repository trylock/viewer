using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.Query.Expressions;

namespace Viewer.Query.Search
{
    internal interface IPriorityComparerFactory
    {
        /// <summary>
        /// Create a new search priority comparer from <paramref name="expression"/>. 
        /// </summary>
        /// <remarks>
        /// It is recommended **not** to cache the returned value as it may depend on some
        /// data that changes even though <paramref name="expression"/> hasn't changed.
        /// </remarks>
        /// <param name="expression">Expression from the where part of a query</param>
        /// <returns>Search priority comparer of directory paths</returns>
        IComparer<string> Create(ValueExpression expression);
    }

    [Export(typeof(IPriorityComparerFactory))]
    internal class PriorityComparerFactory : IPriorityComparerFactory
    {
        private readonly IAttributeCache _attributeCache;
        private readonly HashSet<string> _metadataAttributeNames;

        [ImportingConstructor]
        public PriorityComparerFactory(
            IAttributeCache attributeCache, 
            [ImportMany] IEnumerable<IAttributeReaderFactory> attrReaderFactories)
        {
            _attributeCache = attributeCache;
            _metadataAttributeNames = new HashSet<string>(
                attrReaderFactories.SelectMany(item => item.MetadataAttributeNames));
        }

        public IComparer<string> Create(ValueExpression expression)
        {
            var statistics = Statistics.Fetch(_attributeCache, new AccessedAttributesVisitor(expression));

            return new SearchPriorityComparer(
                expression, 
                new PriorityFunction(statistics, _metadataAttributeNames));
        }
    }
}
