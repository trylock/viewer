using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.Query.Expressions;
using Viewer.Query.QueryExpression;

namespace Viewer.Query
{
    public interface IExecutableQuery
    {
        /// <summary>
        /// Textual representation of the query. It can be null if this query does not have
        /// a textual representation.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Comparer which defines order of the result of this query
        /// </summary>
        IComparer<IEntity> Comparer { get; }

        /// <summary>
        /// Path patterns to directories searched by this query
        /// </summary>
        IEnumerable<PathPattern> Patterns { get; }

        /// <summary>
        /// Evaluate this query. Entities are loaded lazily as much as possible and they are not
        /// sorted.
        /// </summary>
        /// <param name="progress">
        /// This class is used to report query execution progress. You can use
        /// <see cref="NullQueryProgress"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token used to cancel the execution.
        /// </param>
        /// <returns>Matched entities.</returns>
        IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress, 
            CancellationToken cancellationToken);

        /// <summary>
        /// Check whether <paramref name="entity"/> matches the query (i.e., it sould be in the query result)
        /// </summary>
        /// <param name="entity">Checked entity</param>
        /// <returns>true iff <paramref name="entity"/> matches the query</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null</exception>
        bool Match(IEntity entity);
    }

    /// <summary>
    /// Query which is always empty
    /// </summary>
    public sealed class EmptyQuery : IExecutableQuery
    {
        public static EmptyQuery Default { get; } = new EmptyQuery();

        public string Text => null;
        public IComparer<IEntity> Comparer => EntityComparer.Default;
        public IEnumerable<PathPattern> Patterns => Enumerable.Empty<PathPattern>();

        public IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            return Enumerable.Empty<IEntity>();
        }

        public bool Match(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            return false;
        }
    }

    /// <summary>
    /// Memory query always returns entities from the provided collection.
    /// </summary>
    public sealed class MemoryQuery : IExecutableQuery
    {
        private readonly IEnumerable<IEntity> _entities;

        public string Text => null;
        public IComparer<IEntity> Comparer => EntityComparer.Default;
        public IEnumerable<PathPattern> Patterns => Enumerable.Empty<PathPattern>();

        /// <summary>
        /// Convert a collection in memory into a query
        /// </summary>
        /// <param name="entities">Entities to return on evaluation</param>
        public MemoryQuery(IEnumerable<IEntity> entities)
        {
            _entities = entities;
        }

        public IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            progress.Report(new QueryProgressReport(ReportType.BeginExecution, null));
            foreach (var entity in _entities)
            {
                progress.Report(new QueryProgressReport(ReportType.EndLoading, entity.Path));
                yield return entity;
            }
            progress.Report(new QueryProgressReport(ReportType.EndExecution, null));
        }

        public bool Match(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            return _entities.Contains(entity);
        }
    }

    /// <summary>
    /// Immutable structure which builds a query. This is used by the <see cref="QueryCompiler"/>
    /// so that it is easier to isolate and test the compilation result.
    /// </summary>
    internal interface IQuery : IExecutableQuery
    {
        /// <summary>
        /// Build a where query
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        IQuery Where(ValueExpression expression);

        /// <summary>
        /// Set a new comparer
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="comparerText"></param>
        /// <returns></returns>
        IQuery WithComparer(IComparer<IEntity> comparer, string comparerText);

        /// <summary>
        /// Copy this query but set a new textual representation.
        /// </summary>
        /// <param name="text">New textual representation of this query</param>
        /// <returns>Copy of this query with a new textual representation</returns>
        IQuery WithText(string text);

        /// <summary>
        /// Compue set minus on this query and <paramref name="entities"/>
        /// </summary>
        /// <param name="entities">Entities to subtract from this query</param>
        /// <returns>New query</returns>
        IQuery Except(IExecutableQuery entities);

        /// <summary>
        /// Compue set union on this query and <paramref name="entities"/>
        /// </summary>
        /// <param name="entities">Entities to add to this query</param>
        /// <returns>New query</returns>
        IQuery Union(IExecutableQuery entities);

        /// <summary>
        /// Compue set intersection on this query and <paramref name="entities"/>
        /// </summary>
        /// <param name="entities"></param>
        /// <returns>New query</returns>
        IQuery Intersect(IExecutableQuery entities);

        /// <summary>
        /// Make this query a view query (i.e., query in the form: SELECT queryViewName)
        /// </summary>
        /// <param name="queryViewName">Query view name of this query</param>
        /// <returns></returns>
        IQuery View(string queryViewName);
    }

    internal class Query : IQuery
    {
        private readonly IRuntime _runtime;
        private readonly IAttributeCache _attributes;
        private readonly IExecutableQuery _source;
        private readonly string _text;

        public string Text => _text ?? _source.Text;

        public IComparer<IEntity> Comparer => _source.Comparer;
        
        public IEnumerable<PathPattern> Patterns => _source.Patterns;

        public Query(IRuntime runtime, IAttributeCache attributes, IExecutableQuery source) 
            : this(runtime, attributes, source, null)
        {
        }

        public Query(IRuntime runtime, IAttributeCache attributes, IExecutableQuery source, string text)
        {
            _runtime = runtime;
            _attributes = attributes;
            _source = source;
            _text = text;
        }
        
        public override string ToString()
        {
            return Text;
        }

        public IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            return _source.Execute(progress, cancellationToken);
        }

        public bool Match(IEntity entity)
        {
            return _source.Match(entity);
        }

        public IQuery WithComparer(IComparer<IEntity> comparer, string comparerText)
        {
            return new Query(_runtime, _attributes, new OrderedQuery(_source, comparer, comparerText), _text);
        }

        public IQuery Where(ValueExpression expression)
        {
            return new Query(_runtime, _attributes, new WhereQuery(_runtime, _attributes, _source, expression), _text);
        }

        public IQuery Except(IExecutableQuery entities)
        {
            return new Query(_runtime, _attributes, new ExceptQuery(_source, entities), _text);
        }

        public IQuery Union(IExecutableQuery entities)
        {
            return new Query(_runtime, _attributes, new UnionQuery(_source, entities), _text);
        }

        public IQuery Intersect(IExecutableQuery entities)
        {
            return new Query(_runtime, _attributes, new IntersectQuery(_source, entities), _text);
        }

        public IQuery View(string queryViewName)
        {
            return new Query(_runtime, _attributes, new QueryViewQuery(_source, queryViewName), _text);
        }

        public IQuery WithText(string text)
        {
            return new Query(_runtime, _attributes, _source, text);
        }
    }
    
    public interface IQueryFactory
    {
        /// <summary>
        /// Create an empty query
        /// </summary>
        /// <returns>Empty query</returns>
        IExecutableQuery CreateQuery();

        /// <summary>
        /// Create a query with given path pattern
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        IExecutableQuery CreateQuery(string pattern);

        /// <summary>
        /// Create a union of 2 queries.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        IExecutableQuery Union(IExecutableQuery first, IExecutableQuery second);
    }

    [Export(typeof(IQueryFactory))]
    public class QueryFactory : IQueryFactory
    {
        private readonly IRuntime _runtime;
        private readonly IAttributeCache _attributes;
        private readonly IEntityManager _entities;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public QueryFactory(
            IRuntime runtime,
            IAttributeCache attributes,
            IEntityManager entities, 
            IFileSystem fileSystem)
        {
            _runtime = runtime;
            _attributes = attributes;
            _entities = entities;
            _fileSystem = fileSystem;
        }

        public IExecutableQuery CreateQuery()
        {
            return new Query(_runtime, _attributes, EmptyQuery.Default, null);
        }

        public IExecutableQuery CreateQuery(string pattern)
        {
            return new Query(_runtime, _attributes, new SelectQuery(
                _fileSystem, 
                _entities, 
                pattern, 
                FileAttributes.System | FileAttributes.Temporary
            ), null);
        }

        public IExecutableQuery Union(IExecutableQuery first, IExecutableQuery second)
        {
            return new UnionQuery(first, second);
        }
    }
}
