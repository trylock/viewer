using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.IO;

namespace Viewer.Query.QueryExpression
{
    internal class OrderedQuery : QueryFragment
    {
        private readonly IExecutableQuery _source;
        private readonly string _comparerText;

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
                    _source.GetType() != typeof(QueryViewQuery) &&
                    _source.GetType() != typeof(WhereQuery))
                {
                    sourceText = "select (" + Environment.NewLine +
                                 sourceText + Environment.NewLine +
                                 ")";
                }

                return sourceText + Environment.NewLine +
                       "order by " + _comparerText;
            }
        }

        public override IComparer<IEntity> Comparer { get; }

        public override IEnumerable<PathPattern> Patterns => _source.Patterns;

        public OrderedQuery(IExecutableQuery source, IComparer<IEntity> comparer, string comparerText)
        {
            _source = source;
            Comparer = comparer;
            _comparerText = comparerText;
        }

        public override IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress,
            CancellationToken cancellationToken,
            IComparer<string> searchOrder)
        {
            return ExecuteSubquery(_source, progress, cancellationToken, searchOrder);
        }

        public override bool Match(IEntity entity)
        {
            return _source.Match(entity);
        }
    }
}
