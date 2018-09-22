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
    internal abstract class QueryFragment : IExecutableQuery
    {
        public abstract string Text { get; }
        public abstract IComparer<IEntity> Comparer { get; }
        public abstract IEnumerable<PathPattern> Patterns { get; }

        public abstract bool Match(IEntity entity);

        public virtual IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress,
            CancellationToken cancellationToken)
        {
            return Execute(progress, cancellationToken, StringComparer.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Execute the query using <paramref name="searchOrder"/> as order for searching directories.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="searchOrder"></param>
        /// <returns></returns>
        public abstract IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress,
            CancellationToken cancellationToken,
            IComparer<string> searchOrder);

        protected IEnumerable<IEntity> ExecuteSubquery(
            IExecutableQuery subquery,
            IProgress<QueryProgressReport> progress,
            CancellationToken cancellationToken,
            IComparer<string> searchOrder)
        {
            if (subquery is QueryFragment fragment)
            {
                return fragment.Execute(progress, cancellationToken, searchOrder);
            }

            return subquery.Execute(progress, cancellationToken);
        }
    }
}
