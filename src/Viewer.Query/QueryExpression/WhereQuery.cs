using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.IO;
using Viewer.Query.Expressions;
using Viewer.Query.Search;

namespace Viewer.Query.QueryExpression
{
    internal class WhereQuery : QueryFragment
    {
        private readonly IRuntime _runtime;
        private readonly IAttributeCache _attributes;
        private readonly IExecutableQuery _source;
        private readonly ValueExpression _expression;

        #region Precomputed values

        /// <summary>
        /// _expression compiled to a predicate
        /// </summary>
        private readonly Func<IEntity, bool> _predicate;

        #endregion

        public override IComparer<IEntity> Comparer => _source.Comparer;

        public override IEnumerable<PathPattern> Patterns => _source.Patterns;

        public override string Text
        {
            get
            {
                var sourceText = _source.Text;
                if (sourceText == null)
                {
                    return null;
                }

                if (_source.GetType() != typeof(SelectQuery) &&
                    _source.GetType() != typeof(QueryViewQuery))
                {
                    sourceText = "select (" + Environment.NewLine +
                                 sourceText + Environment.NewLine +
                                 ")";
                }

                return sourceText + Environment.NewLine + "where " + _expression;
            }
        }
        
        public WhereQuery(
            IRuntime runtime, 
            IAttributeCache attributes,
            IExecutableQuery source, 
            ValueExpression expression)
        {
            _runtime = runtime;
            _attributes = attributes;
            _source = source;
            _expression = expression;
            _predicate = _expression.CompilePredicate(runtime);
        }

        public override IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress, 
            CancellationToken cancellationToken,
            IComparer<string> searchOrder)
        {
            if (_source is QueryFragment fragment) // the subquery supports search order
            {
                var statistics = Statistics.Fetch(_attributes, new AccessedAttributesVisitor(_expression));

                var comparer = new SearchPriorityComparer(_expression, new PriorityFunction(statistics));

                return fragment.Execute(progress, cancellationToken, comparer).Where(_predicate);
            }

            return _source.Execute(progress, cancellationToken).Where(_predicate);
        }

        public override bool Match(IEntity entity)
        {
            return _source.Match(entity) && _predicate(entity);
        }
    }
}
