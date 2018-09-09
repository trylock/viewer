using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.QueryExpression
{
    internal class QueryViewQuery : IExecutableQuery
    {
        private readonly IExecutableQuery _queryView;
        private readonly string _queryViewName;

        public string Text => $"select {_queryViewName}";

        public IComparer<IEntity> Comparer => _queryView.Comparer;

        public IEnumerable<string> Patterns => _queryView.Patterns;

        public QueryViewQuery(IExecutableQuery queryView, string queryViewName)
        {
            _queryView = queryView;
            _queryViewName = queryViewName;
        }

        public IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            return _queryView.Execute(progress, cancellationToken);
        }

        public bool Match(IEntity entity)
        {
            return _queryView.Match(entity);
        }
    }

}
