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
    internal class QueryViewQuery : QueryFragment
    {
        private readonly IExecutableQuery _queryView;
        private readonly string _queryViewName;

        public override string Text => $"select {_queryViewName}";

        public override IComparer<IEntity> Comparer => _queryView.Comparer;

        public override IEnumerable<PathPattern> Patterns => _queryView.Patterns;

        public QueryViewQuery(IExecutableQuery queryView, string queryViewName)
        {
            _queryView = queryView;
            _queryViewName = queryViewName;
        }

        public override IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress, 
            CancellationToken cancellationToken,
            IComparer<string> searchOrder)
        {
            return ExecuteSubquery(_queryView, progress, cancellationToken, searchOrder);
        }

        public override bool Match(IEntity entity)
        {
            return _queryView.Match(entity);
        }
    }
}
