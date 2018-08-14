using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.Data.Storage;
using Viewer.IO;
using Path = System.IO.Path;

namespace Viewer.Query
{
    public interface IExecutableQuery
    {
        /// <summary>
        /// Evaluate this query.
        /// Entities are loaded lazily as much as possible.
        /// </summary>
        /// <param name="progress">This class is used to report query execution progress. You can use <see cref="NullQueryProgress"/>.</param>
        /// <param name="cancellationToken">Cancellation token used to cancel the execution.</param>
        /// <returns>Matched entities.</returns>
        IEnumerable<IEntity> Evaluate(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Immutable structure which describes a query.
    /// Evaluated entities are returned in random order even if this query has a comparer.
    /// It up to the user to use provided comparer to sort the returned values.
    /// </summary>
    public interface IQuery : IExecutableQuery
    {
        /// <summary>
        /// Textual representation of the query
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Current comparer to sort the query result
        /// </summary>
        IComparer<IEntity> Comparer { get; }

        /// <summary>
        /// Set comparer to order the query result
        /// </summary>
        /// <param name="comparer"></param>
        /// <returns></returns>
        IQuery WithComparer(IComparer<IEntity> comparer);

        /// <summary>
        /// Create the same query with different textual representation
        /// </summary>
        /// <param name="text">Textual representation of this query</param>
        /// <returns>New query with new textual representation</returns>
        IQuery WithText(string text);

        /// <summary>
        /// Only include entities in the result if <paramref name="predicate"/> returns true
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IQuery Where(Func<IEntity, bool> predicate);

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

        public IEnumerable<IEntity> Evaluate(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            return Enumerable.Empty<IEntity>();
        }
    }

    /// <summary>
    /// Memory query always returns entities from the provided collection.
    /// </summary>
    public sealed class MemoryQuery : IExecutableQuery
    {
        private readonly IEnumerable<IEntity> _entities;

        /// <summary>
        /// Convert a collection in memory into a query
        /// </summary>
        /// <param name="entities">Entities to return on evaluation</param>
        public MemoryQuery(IEnumerable<IEntity> entities)
        {
            _entities = entities;
        }

        public IEnumerable<IEntity> Evaluate(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            progress.Report(new QueryProgressReport(ReportType.BeginExecution, null, 0));
            foreach (var entity in _entities)
            {
                progress.Report(new QueryProgressReport(ReportType.EndLoading, entity.Path, 0));
                yield return entity;
            }
            progress.Report(new QueryProgressReport(ReportType.EndExecution, null, 0));
        }
    }

    internal class SelectQuery : IExecutableQuery
    {
        private readonly IFileSystem _fileSystem;
        private readonly IEntityManager _entities;
        private readonly string _pattern;
        private readonly FileAttributes _hiddenFlags;

        public SelectQuery(IFileSystem fileSystem, IEntityManager entities, string pattern, FileAttributes hiddenFlags)
        {
            _fileSystem = fileSystem;
            _entities = entities;
            _pattern = pattern;
            _hiddenFlags = hiddenFlags;
        }

        public IEnumerable<IEntity> Evaluate(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            progress.Report(new QueryProgressReport(ReportType.BeginExecution, null, 0));

            foreach (var dir in EnumerateDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // add files
                foreach (var file in EnumerateFiles(dir))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // load jpeg files only
                    var extension = Path.GetExtension(file)?.ToLowerInvariant();
                    if (extension != ".jpg" && extension != ".jpeg")
                    {
                        continue;
                    }

                    // skip files with hidden attributes
                    if (IsHidden(file))
                        continue;
                    
                    // load file
                    progress.Report(new QueryProgressReport(ReportType.BeginLoading, file, 0));
                    var result = LoadEntity(file);
                    progress.Report(new QueryProgressReport(ReportType.EndLoading, file, result.BytesRead));
                    if (result.Entity == null)
                    {
                        continue;
                    }

                    yield return result.Entity;
                }

                // add directories
                foreach (var directory in EnumerateDirectories(dir))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // skip files with hidden attributes
                    if (IsHidden(directory))
                        continue;

                    yield return new DirectoryEntity(directory);
                }
            }

            progress.Report(new QueryProgressReport(ReportType.EndExecution, null, 0));
        }

        private bool IsHidden(string path)
        {
            return (_fileSystem.GetAttributes(path) & _hiddenFlags) != 0;
        }

        private IEnumerable<string> EnumerateFiles(string path)
        {
            try
            {
                return _fileSystem.EnumerateFiles(path);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SecurityException)
            {
            }
            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> EnumerateDirectories(string path)
        {
            try
            {
                return _fileSystem.EnumerateDirectories(path);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SecurityException)
            {
            }
            return Enumerable.Empty<string>();
        }

        private LoadResult LoadEntity(string path)
        {
            try
            {
                return _entities.GetEntity(path);
            }
            catch (InvalidDataFormatException)
            {
            }
            catch (PathTooLongException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (IOException)
            {
            }
            catch (Exception e) when (e.GetType() == typeof(UnauthorizedAccessException) || 
                                      e.GetType() == typeof(SecurityException))
            {
                // skip these 
            }

            return new LoadResult(null, 0);
        }
        
        private IEnumerable<string> EnumerateDirectories()
        {
            if (_pattern == null)
            {
                return Enumerable.Empty<string>();
            }

            var finder = new FileFinder(_fileSystem, _pattern);
            return finder.GetDirectories();
        }
    }

    internal class FilteredQuery : IExecutableQuery
    {
        private readonly IExecutableQuery _source;
        private readonly Func<IEntity, bool> _predicate;

        public FilteredQuery(IExecutableQuery source, Func<IEntity, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IEnumerable<IEntity> Evaluate(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            return _source.Evaluate(progress, cancellationToken).Where(_predicate);
        }
    }

    internal class ExceptQuery : IExecutableQuery
    {
        private readonly IExecutableQuery _first;
        private readonly IExecutableQuery _second;

        public ExceptQuery(IExecutableQuery first, IExecutableQuery second)
        {
            _first = first;
            _second = second;
        }

        public IEnumerable<IEntity> Evaluate(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            var firstEvaluation = _first.Evaluate(progress, cancellationToken);
            var secondEvaluation = _second.Evaluate(progress, cancellationToken);
            return firstEvaluation.Except(secondEvaluation, EntityPathEqualityComparer.Default);
        }
    }

    internal class IntersectQuery : IExecutableQuery
    {
        private readonly IExecutableQuery _first;
        private readonly IExecutableQuery _second;

        public IntersectQuery(IExecutableQuery first, IExecutableQuery second)
        {
            _first = first;
            _second = second;
        }

        public IEnumerable<IEntity> Evaluate(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            var firstEvaluation = _first.Evaluate(progress, cancellationToken);
            var secondEvaluation = _second.Evaluate(progress, cancellationToken);
            return firstEvaluation.Intersect(secondEvaluation, EntityPathEqualityComparer.Default);
        }
    }

    internal class UnionQuery : IExecutableQuery
    {
        private readonly IExecutableQuery _first;
        private readonly IExecutableQuery _second;

        public UnionQuery(IExecutableQuery first, IExecutableQuery second)
        {
            _first = first;
            _second = second;
        }

        public IEnumerable<IEntity> Evaluate(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            var firstEvaluation = _first.Evaluate(progress, cancellationToken);
            var secondEvaluation = _second.Evaluate(progress, cancellationToken);
            return firstEvaluation.Union(secondEvaluation, EntityPathEqualityComparer.Default);
        }
    }

    public class Query : IQuery
    {
        private readonly IExecutableQuery _source;

        public string Text { get;}
        public IComparer<IEntity> Comparer { get; }

        public Query(IExecutableQuery source, IComparer<IEntity> comparer, string text)
        {
            _source = source;
            Text = text;
            Comparer = comparer;
        }
        
        public override string ToString()
        {
            return Text;
        }
        
        public IEnumerable<IEntity> Evaluate(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            return _source.Evaluate(progress, cancellationToken);
        }

        public IQuery WithComparer(IComparer<IEntity> comparer)
        {
            return new Query(_source, comparer, Text);
        }

        public IQuery WithText(string text)
        {
            return new Query(_source, Comparer, text);
        }

        public IQuery Where(Func<IEntity, bool> predicate)
        {
            return new Query(new FilteredQuery(_source, predicate), Comparer, Text);
        }

        public IQuery Except(IExecutableQuery entities)
        {
            return new Query(new ExceptQuery(_source, entities), Comparer, Text);
        }

        public IQuery Union(IExecutableQuery entities)
        {
            return new Query(new UnionQuery(_source, entities), Comparer, Text);
        }

        public IQuery Intersect(IExecutableQuery entities)
        {
            return new Query(new IntersectQuery(_source, entities), Comparer, Text);
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
            return new Query(EmptyQuery.Default, EntityComparer.Default, "");
        }

        public IQuery CreateQuery(string pattern)
        {
            return new Query(
                new SelectQuery(_fileSystem, _entities, pattern, FileAttributes.System | FileAttributes.Temporary), 
                EntityComparer.Default, 
                "select \"" + pattern + "\"");
        }
    }
}
