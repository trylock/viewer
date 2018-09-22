﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.IO;
using Viewer.Query.Expressions;

namespace Viewer.Query.QueryExpression
{
    internal class WhereQuery : QueryFragment
    {
        private readonly IAttributeCache _attributes;
        private readonly IExecutableQuery _source;
        private readonly Func<IEntity, bool> _predicate;
        private readonly ValueExpression _expression;

        public override IComparer<IEntity> Comparer => _source.Comparer;

        public override IEnumerable<PathPattern> Patterns => _source.Patterns;

        public override string Text
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

                return sourceText + Environment.NewLine + "where " + _expression;
            }
        }
        
        public WhereQuery(
            IRuntime runtime, 
            IAttributeCache attributes,
            IExecutableQuery source, 
            ValueExpression expression)
        {
            _attributes = attributes;
            _source = source;
            _expression = expression;
            _predicate = _expression.CompilePredicate(runtime);
        }

        public override IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress, 
            CancellationToken cancellationToken,
            IComparer<string> searchOrder)
        {
            if (_source is QueryFragment fragment) // the subquery supports search order
            {
                var priorityFunction = new SearchPriorityComparer(_expression);
                priorityFunction.Index(_attributes);

                return fragment.Execute(progress, cancellationToken, priorityFunction).Where(_predicate);
            }

            return _source.Execute(progress, cancellationToken).Where(_predicate);
        }

        public override bool Match(IEntity entity)
        {
            return _source.Match(entity) && _predicate(entity);
        }
    }
}
