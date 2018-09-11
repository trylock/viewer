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
    internal class OrderedQuery : IExecutableQuery
    {
        private readonly IExecutableQuery _source;
        private readonly string _comparerText;

        public string Text
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

        public IComparer<IEntity> Comparer { get; }

        public IEnumerable<PathPattern> Patterns => _source.Patterns;

        public OrderedQuery(IExecutableQuery source, IComparer<IEntity> comparer, string comparerText)
        {
            _source = source;
            Comparer = comparer;
            _comparerText = comparerText;
        }

        public IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress,
            CancellationToken cancellationToken)
        {
            return _source.Execute(progress, cancellationToken);
        }

        public bool Match(IEntity entity)
        {
            return _source.Match(entity);
        }
    }
}
