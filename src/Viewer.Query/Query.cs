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
using Path = System.IO.Path;

namespace Viewer.Query
{
    public interface IExecutableQuery
    {
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
        IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken);

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

    internal class SelectQuery : IExecutableQuery
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public IEnumerable<string> Patterns
        {
            get
            {
                yield return _fileFinder.Pattern;
            }
        }

        private readonly IFileSystem _fileSystem;
        private readonly IEntityManager _entities;
        private readonly FileFinder _fileFinder;
        private readonly FileAttributes _hiddenFlags;

        public SelectQuery(IFileSystem fileSystem, IEntityManager entities, string pattern, FileAttributes hiddenFlags)
        {
            _fileFinder = new FileFinder(fileSystem, pattern ?? "");
            _fileSystem = fileSystem;
            _entities = entities;
            _hiddenFlags = hiddenFlags;
        }

        public IEnumerable<IEntity> Execute(
            IProgress<QueryProgressReport> progress, 
            CancellationToken cancellationToken)
        {
            progress.Report(new QueryProgressReport(ReportType.BeginExecution, null));
            
            foreach (var file in EnumeratePaths(cancellationToken))
            {
                progress.Report(new QueryProgressReport(ReportType.BeginLoading, file.Path));
                IEntity entity;
                if (file.IsFile)
                {
                    entity = LoadEntity(file.Path);
                }
                else
                {
                    entity = new DirectoryEntity(file.Path);
                }

                progress.Report(new QueryProgressReport(ReportType.EndLoading, file.Path));

                if (entity != null)
                {
                    yield return entity;
                }
            }
            
            progress.Report(new QueryProgressReport(ReportType.EndExecution, null));
        }

        private IEnumerable<(string Path, bool IsFile)> EnumeratePaths(CancellationToken token)
        {
            foreach (var dir in EnumerateDirectories())
            {
                token.ThrowIfCancellationRequested();

                foreach (var file in EnumerateFiles(dir))
                {
                    token.ThrowIfCancellationRequested();

                    // load jpeg files only
                    var extension = Path.GetExtension(file)?.ToLowerInvariant();
                    if (extension != ".jpg" && extension != ".jpeg")
                    {
                        continue;
                    }

                    // skip files with hidden attributes
                    if (IsHidden(file))
                        continue;

                    yield return (file, true);
                }

                foreach (var directory in EnumerateDirectories(dir))
                {
                    token.ThrowIfCancellationRequested();

                    // skip files with hidden attributes
                    if (IsHidden(directory))
                        continue;

                    yield return (directory, false);
                }
            }
        }

        public bool Match(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (IsHidden(entity.Path))
                return false;

            // the entity is in this query iff its parent diretory matches the path pattern
            var parentPath = PathUtils.GetDirectoryPath(entity.Path);
            return _fileFinder.Match(parentPath);
        }

        private bool IsHidden(string path)
        {
            try
            {
                return (_fileSystem.GetAttributes(path) & _hiddenFlags) != 0;
            }
            catch (FileNotFoundException e)
            {
                // this is fine, just don't load the file
                Logger.Trace(e, "SELECT \"{0}\"", _fileFinder.Pattern); 
            }
            catch (DirectoryNotFoundException e)
            {
                // this is fine, just don't load the file
                Logger.Trace(e, "SELECT \"{0}\"", _fileFinder.Pattern); 
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Debug(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (SecurityException e)
            {
                Logger.Debug(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (IOException e)
            {
                // If the file is being used by another process, skip it.
                Logger.Warn(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }

            return true;
        }

        private IEnumerable<string> EnumerateFiles(string path)
        {
            try
            {
                return _fileSystem.EnumerateFiles(path);
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Trace(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (ArgumentException e)
            {
                // this should not happen as we have checked the path pattern during query compilation
                Logger.Error(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Trace(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (SecurityException e)
            {
                Logger.Trace(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> EnumerateDirectories(string path)
        {
            try
            {
                return _fileSystem.EnumerateDirectories(path);
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Trace(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (ArgumentException e)
            {
                // this should not happen as we have checked the path pattern during query compilation
                Logger.Error(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Debug(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (SecurityException e)
            {
                Logger.Debug(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            return Enumerable.Empty<string>();
        }

        private IEntity LoadEntity(string path)
        {
            try
            {
                return _entities.GetEntity(path);
            }
            catch (InvalidDataFormatException e)
            {
                Logger.Debug(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (PathTooLongException e)
            {
                Logger.Debug(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (ArgumentException e)
            {
                Logger.Error(e, "SELECT \"{0}\"", _fileFinder.Pattern);
            }
            catch (IOException e)
            {
                Logger.Debug(e);
            }
            catch (Exception e) when (e.GetType() == typeof(UnauthorizedAccessException) || 
                                      e.GetType() == typeof(SecurityException))
            {
                // skip these 
            }

            return null;
        }
        
        private IEnumerable<string> EnumerateDirectories()
        {
            return _fileFinder.GetDirectories();
        }
    }

    internal class FilteredQuery : IExecutableQuery
    {
        private readonly IExecutableQuery _source;
        private readonly Func<IEntity, bool> _predicate;

        public IEnumerable<string> Patterns => _source.Patterns;

        public FilteredQuery(IExecutableQuery source, Func<IEntity, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
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

    internal abstract class BinaryOperatorQuery : IExecutableQuery
    {
        protected readonly IExecutableQuery First;
        protected readonly IExecutableQuery Second;

        protected BinaryOperatorQuery(IExecutableQuery first, IExecutableQuery second)
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

    internal class ExceptQuery : BinaryOperatorQuery
    {
        public ExceptQuery(IExecutableQuery first, IExecutableQuery second) : base(first, second)
        {
        }

        public override IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            var firstEvaluation = First.Execute(progress, cancellationToken);
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

    internal class IntersectQuery : BinaryOperatorQuery
    {
        public IntersectQuery(IExecutableQuery first, IExecutableQuery second) : base(first, second)
        {
        }

        public override IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            var visited = new HashSet<IEntity>(EntityPathEqualityComparer.Default);
            var firstEvaluation = First.Execute(progress, cancellationToken);
            foreach (var item in firstEvaluation)
            {
                visited.Add(item);
                if (Second.Match(item))
                    yield return item;
            }

            var secondEvaluation = Second.Execute(progress, cancellationToken);
            foreach (var item in secondEvaluation)
            {
                if (!visited.Contains(item) && First.Match(item))
                    yield return item;
            }
        }

        public override bool Match(IEntity entity)
        {
            return First.Match(entity) && Second.Match(entity);
        }
    }

    internal class UnionQuery : BinaryOperatorQuery
    {
        public UnionQuery(IExecutableQuery first, IExecutableQuery second) : base(first, second)
        {
        }

        public override IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            var firstEvaluation = First.Execute(progress, cancellationToken);
            var secondEvaluation = Second.Execute(progress, cancellationToken);
            return firstEvaluation.Union(secondEvaluation, EntityPathEqualityComparer.Default);
        }
        
        public override bool Match(IEntity entity)
        {
            return First.Match(entity) || Second.Match(entity);
        }
    }

    internal class Query : IQuery
    {
        private readonly IExecutableQuery _source;

        public string Text { get;}
        public IComparer<IEntity> Comparer { get; }
        
        public IEnumerable<string> Patterns => _source.Patterns;

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

        public IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            return _source.Execute(progress, cancellationToken);
        }

        public bool Match(IEntity entity)
        {
            return _source.Match(entity);
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
