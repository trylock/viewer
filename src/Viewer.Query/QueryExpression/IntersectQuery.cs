using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.QueryExpression
{
    internal class IntersectQuery : BinaryQueryOperator
    {
        protected override string Operator => "intersect";

        public IntersectQuery(IExecutableQuery first, IExecutableQuery second) : base(first, second)
        {
        }

        public override IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress, 
            CancellationToken cancellationToken,
            IComparer<string> searchOrder)
        {
            var visited = new HashSet<IEntity>(EntityPathEqualityComparer.Default);
            var firstEvaluation = ExecuteSubquery(First, progress, cancellationToken, searchOrder);
            foreach (var item in firstEvaluation)
            {
                visited.Add(item);
                if (Second.Match(item))
                    yield return item;
            }

            var secondEvaluation = ExecuteSubquery(Second, progress, cancellationToken, searchOrder);
            foreach (var item in secondEvaluation)
            {
                if (!visited.Contains(item) && First.Match(item))
                    yield return item;
            }
        }

        public override bool Match(IEntity entity)
        {
            return First.Match(entity) && Second.Match(entity);
        }
    }

}
