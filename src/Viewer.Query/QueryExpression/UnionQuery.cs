using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.QueryExpression
{
    internal class UnionQuery : BinaryQueryOperator
    {
        protected override string Operator => "union";

        public UnionQuery(IExecutableQuery first, IExecutableQuery second) : base(first, second)
        {
        }

        public override IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            var firstEvaluation = First.Execute(progress, cancellationToken);
            var secondEvaluation = Second.Execute(progress, cancellationToken);
            return firstEvaluation.Union(secondEvaluation, EntityPathEqualityComparer.Default);
        }

        public override bool Match(IEntity entity)
        {
            return First.Match(entity) || Second.Match(entity);
        }
    }
}
