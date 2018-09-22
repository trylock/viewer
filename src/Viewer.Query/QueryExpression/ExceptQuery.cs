using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.QueryExpression
{
    internal class ExceptQuery : BinaryQueryOperator
    {
        protected override string Operator => "except";

        public ExceptQuery(IExecutableQuery first, IExecutableQuery second) : base(first, second)
        {
        }
        
        public override IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress, 
            CancellationToken cancellationToken, 
            IComparer<string> searchOrder)
        {
            var firstEvaluation = ExecuteSubquery(First, progress, cancellationToken, searchOrder);
            foreach (var item in firstEvaluation)
            {
                if (!Second.Match(item))
                    yield return item;
            }
        }

        public override bool Match(IEntity entity)
        {
            return First.Match(entity) && !Second.Match(entity);
        }
    }
}
