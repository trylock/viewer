using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.Data.SQLite;
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
        private readonly IAttributeStatisticsFactory _attributeStatisticsFactory;
        private readonly HashSet<string> _metadataAttributeNames;

        [ImportingConstructor]
        public PriorityComparerFactory(
            IAttributeStatisticsFactory attributeStatisticsFactory, 
            [ImportMany] IEnumerable<IAttributeReaderFactory> attrReaderFactories)
        {
            _attributeStatisticsFactory = attributeStatisticsFactory;
            _metadataAttributeNames = new HashSet<string>(
                attrReaderFactories.SelectMany(item => item.MetadataAttributeNames));
        }

        public IComparer<string> Create(ValueExpression expression)
        {
            var attributes = new AccessedAttributesVisitor(expression)
                .Where(name => !_metadataAttributeNames.Contains(name));
            
            var statistics = _attributeStatisticsFactory.Create(attributes);
            var priority = new PriorityFunction(statistics, _metadataAttributeNames);
            return new SearchPriorityComparer(expression, priority);
        }
    }
}
