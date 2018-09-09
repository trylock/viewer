using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.QueryExpression
{
    internal abstract class BinaryQueryOperator : IExecutableQuery
    {
        protected readonly IExecutableQuery First;
        protected readonly IExecutableQuery Second;

        public string Text => First.Text + Environment.NewLine +
                              Operator + Environment.NewLine +
                              Second.Text;

        public IComparer<IEntity> Comparer => First.Comparer;

        protected abstract string Operator { get; }

        protected BinaryQueryOperator(IExecutableQuery first, IExecutableQuery second)
        {
            First = first;
            Second = second;
        }

        public IEnumerable<string> Patterns => First.Patterns.Concat(Second.Patterns);

        public abstract IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress,
            CancellationToken cancellationToken);

        public abstract bool Match(IEntity entity);
    }
}
