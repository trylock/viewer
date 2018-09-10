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
        IEnumerable<string> Patterns { get; }

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
    /// Immutable structure which describes a query.
    /// Evaluated entities are returned in random order even if this query has a comparer.
    /// It up to the user to use provided comparer to sort the returned values.
    /// </summary>
    public interface IQuery : IExecutableQuery
    {
        /// <summary>
        /// Set comparer to order the query result
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="comparerText">Textual representation of the comparer</param>
        /// <returns></returns>
        IQuery WithComparer(IComparer<IEntity> comparer, string comparerText);
        
        /// <summary>
        /// Only include entities in the result if <paramref name="predicate"/> returns true
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="predicateText">Textual representation of <paramref name="predicate"/></param>
        /// <returns></returns>
        IQuery Where(Func<IEntity, bool> predicate, string predicateText);

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

        /// <summary>
        /// Copy this query but set a new textual representation.
        /// </summary>
        /// <param name="text">New textual representation of this query</param>
        /// <returns>Copy of this query with a new textual representation</returns>
        IQuery WithText(string text);
    }
    
    public interface IQueryFactory
    {
        /// <summary>
        /// Create an empty query
        /// </summary>
        /// <returns>Empty query</returns>
        IQuery CreateQuery();

        /// <summary>
        /// Create a query with given path pattern
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        IQuery CreateQuery(string pattern);
    }

    /// <summary>
    /// Query which is always empty
    /// </summary>
    public sealed class EmptyQuery : IExecutableQuery
    {
        public static EmptyQuery Default { get; } = new EmptyQuery();

        public string Text => null;
        public IComparer<IEntity> Comparer => EntityComparer.Default;
        public IEnumerable<string> Patterns => Enumerable.Empty<string>();

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
        public IEnumerable<string> Patterns => Enumerable.Empty<string>();

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

    internal class Query : IQuery
    {
        private readonly IExecutableQuery _source;
        private readonly string _text;

        public string Text => _text ?? _source.Text;

        public IComparer<IEntity> Comparer => _source.Comparer;
        
        public IEnumerable<string> Patterns => _source.Patterns;

        public Query(IExecutableQuery source) : this(source, null)
        {
        }

        public Query(IExecutableQuery source, string text)
        {
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
            return new Query(new OrderedQuery(_source, comparer, comparerText), _text);
        }

        public IQuery Where(Func<IEntity, bool> predicate, string predicateText)
        {
            return new Query(new WhereQuery(_source, predicate, predicateText), _text);
        }

        public IQuery Except(IExecutableQuery entities)
        {
            return new Query(new ExceptQuery(_source, entities), _text);
        }

        public IQuery Union(IExecutableQuery entities)
        {
            return new Query(new UnionQuery(_source, entities), _text);
        }

        public IQuery Intersect(IExecutableQuery entities)
        {
            return new Query(new IntersectQuery(_source, entities), _text);
        }

        public IQuery View(string queryViewName)
        {
            return new Query(new QueryViewQuery(_source, queryViewName), _text);
        }

        public IQuery WithText(string text)
        {
            return new Query(_source, text);
        }
    }
    
    [Export(typeof(IQueryFactory))]
    public class QueryFactory : IQueryFactory
    {
        private readonly IEntityManager _entities;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public QueryFactory(
            IEntityManager entities, 
            IFileSystem fileSystem)
        {
            _entities = entities;
            _fileSystem = fileSystem;
        }

        public IQuery CreateQuery()
        {
            return new Query(EmptyQuery.Default, null);
        }

        public IQuery CreateQuery(string pattern)
        {
            return new Query(new SelectQuery(
                _fileSystem, 
                _entities, 
                pattern, 
                FileAttributes.System | FileAttributes.Temporary
            ), null);
        }
    }
}
