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
    internal abstract class BinaryQueryOperator : QueryFragment
    {
        protected readonly IExecutableQuery First;
        protected readonly IExecutableQuery Second;

        public override string Text => First.Text + Environment.NewLine +
                              Operator + Environment.NewLine +
                              Second.Text;

        public override IComparer<IEntity> Comparer => First.Comparer;

        public override IEnumerable<PathPattern> Patterns => First.Patterns.Concat(Second.Patterns);

        protected abstract string Operator { get; }

        protected BinaryQueryOperator(IExecutableQuery first, IExecutableQuery second)
        {
            First = first;
            Second = second;
        }
    }
}
