using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.IO;

namespace Viewer.Query.Execution
{
    internal abstract class BinaryQueryOperator : IExecutableQuery
    {
        public IExecutableQuery First { get; }
        public IExecutableQuery Second { get; }

        public string Text => First.Text + Environment.NewLine +
                                       Operator + Environment.NewLine +
                                       Second.Text;

        public IComparer<IEntity> Comparer => First.Comparer;

        public IEnumerable<PathPattern> Patterns => First.Patterns.Concat(Second.Patterns);

        protected abstract string Operator { get; }

        protected BinaryQueryOperator(IExecutableQuery first, IExecutableQuery second)
        {
            First = first;
            Second = second;
        }

        public virtual IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress,
            CancellationToken cancellationToken)
        {
            return Execute(new ExecutionOptions
            {
                Progress = progress,
                CancellationToken = cancellationToken
            });
        }

        public abstract IEnumerable<IEntity> Execute(ExecutionOptions options);

        public abstract bool Match(IEntity entity);
    }
}
