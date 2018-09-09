using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.QueryExpression
{
    internal class WhereQuery : IExecutableQuery
    {
        private readonly IExecutableQuery _source;
        private readonly Func<IEntity, bool> _predicate;
        private readonly string _predicateText;

        public IComparer<IEntity> Comparer => _source.Comparer;

        public IEnumerable<string> Patterns => _source.Patterns;

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
                    _source.GetType() != typeof(QueryViewQuery))
                {
                    sourceText = "select (" + Environment.NewLine +
                                 sourceText + Environment.NewLine +
                                 ")";
                }

                return sourceText + Environment.NewLine + "where " + _predicateText;
            }
        }


        public WhereQuery(IExecutableQuery source, Func<IEntity, bool> predicate, string predicateText)
        {
            _source = source;
            _predicate = predicate;
            _predicateText = predicateText;
        }

        public IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            return _source.Execute(progress, cancellationToken).Where(_predicate);
        }

        public bool Match(IEntity entity)
        {
            return _source.Match(entity) && _predicate(entity);
        }
    }
}
