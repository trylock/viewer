using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Viewer.Query.Search;

namespace Viewer.Query.Execution
{
    internal class SimpleQuery : IExecutableQuery
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // dependencies
        private readonly IRuntime _runtime;
        private readonly IFileSystem _fileSystem;
        private readonly IEntityManager _entityManager;
        private readonly IPriorityComparerFactory _priorityComparerFactory;

        // properties
        private readonly IFileFinder _fileFinder;
        private readonly Func<IEntity, bool> _compiledPredicate;

        private static readonly FileAttributes HiddenFlags = FileAttributes.System | 
                                                             FileAttributes.Temporary;
        
        public string Text { get; }
        public ValueExpression Predicate { get; }
        public IComparer<IEntity> Comparer { get; }
        public IEnumerable<PathPattern> Patterns => Enumerable.Repeat(_fileFinder.Pattern, 1);

        public SimpleQuery(
            IEntityManager entityManager,
            IFileSystem fileSystem,
            IRuntime runtime,
            IPriorityComparerFactory priorityComparerFactory,
            string pattern,
            ValueExpression predicate,
            IComparer<IEntity> comparer,
            string comparerText)
        {
            _runtime = runtime;
            _fileSystem = fileSystem;
            _entityManager = entityManager;
            _priorityComparerFactory = priorityComparerFactory;

            _fileFinder = _fileSystem.CreateFileFinder(pattern);
            Predicate = predicate;
            _compiledPredicate = Predicate.CompilePredicate(_runtime);
            Comparer = comparer;

            var text = $"select \"{_fileFinder.Pattern.Text}\"";
            if (Predicate != ValueExpression.True)
            {
                text += $" where {Predicate}";
            }

            if (!string.IsNullOrWhiteSpace(comparerText))
            {
                text += $" order by {comparerText}";
            }

            Text = text;
        }

        public IEnumerable<IEntity> Execute(IProgress<QueryProgressReport> progress, CancellationToken cancellationToken)
        {
            return Execute(new ExecutionOptions
            {
                Progress = progress,
                CancellationToken = cancellationToken
            });
        }

        public IEnumerable<IEntity> Execute(ExecutionOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return ExecuteImpl(options);
        }

        private IEnumerable<IEntity> ExecuteImpl(ExecutionOptions options)
        {
            // determine an optimal search order
            var searchOrder = _priorityComparerFactory.Create(Predicate);
            var files = EnumeratePaths(options.Progress, options.CancellationToken, searchOrder);

            // execute the query
            using (var reader = _entityManager.CreateReader())
            {
                foreach (var file in files)
                {
                    options.Progress.Report(new QueryProgressReport(ReportType.BeginLoading, file.Path));

                    IEntity entity = null;
                    if (file.IsFile)
                    {
                        entity = LoadEntity(reader, file.Path);
                    }
                    else // file is a directory
                    {
                        entity = new DirectoryEntity(file.Path);
                    }

                    options.Progress.Report(new QueryProgressReport(ReportType.EndLoading, file.Path));

                    if (entity != null && _compiledPredicate(entity))
                    {
                        yield return entity;
                    }
                }
            }

            options.Progress.Report(new QueryProgressReport(ReportType.EndExecution, null));
        }
        
        public bool Match(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (IsHidden(entity.Path))
                return false;

            // if the entity is in the result set, its directory has to match the path pattern
            var parentPath = PathUtils.GetDirectoryPath(entity.Path);
            if (!_fileFinder.Match(parentPath))
            {
                return false;
            }

            return _compiledPredicate(entity);
        }

        /// <summary>
        /// Create a new simple query with a different comparer
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public SimpleQuery WithComparer(IComparer<IEntity> comparer, string text)
        {
            return new SimpleQuery(
                _entityManager, _fileSystem, _runtime, _priorityComparerFactory, 
                _fileFinder.Pattern.Text, Predicate, comparer, text);
        }

        /// <summary>
        /// Create a new simple query with a predicate: <c>oldPredicate and newPredicate</c>
        /// </summary>
        /// <param name="predicate">New predicate</param>
        /// <returns>New simple query</returns>
        public SimpleQuery AppendPredicate(ValueExpression predicate)
        {
            var newPredicate = new AndExpression(
                predicate.Line, 
                predicate.Column, 
                Predicate, 
                predicate);
            return new SimpleQuery(
                _entityManager, _fileSystem, _runtime, _priorityComparerFactory,
                _fileFinder.Pattern.Text, newPredicate, Comparer, null);
        }

        private bool IsValidPath(string path)
        {
            var extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !IsHidden(path);
        }

        private bool IsHidden(string path)
        {
            try
            {
                return (_fileSystem.GetAttributes(path) & HiddenFlags) != 0;
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
        
        private IEnumerable<(string Path, bool IsFile)> EnumeratePaths(
            IProgress<QueryProgressReport> progress,
            CancellationToken token,
            IComparer<string> searchOrder)
        {
            foreach (var dir in EnumerateDirectories(searchOrder))
            {
                token.ThrowIfCancellationRequested();

                // report that we have found a new folder
                progress.Report(new QueryProgressReport(ReportType.Folder, dir));

                foreach (var file in EnumerateFiles(dir))
                {
                    token.ThrowIfCancellationRequested();
                    
                    if (!IsValidPath(file))
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

        private IEntity LoadEntity(IReadableAttributeStorage storage, string path)
        {
            try
            {
                return storage.Load(path);
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

        private IEnumerable<string> EnumerateDirectories(IComparer<string> searchOrder)
        {
            return _fileFinder.GetDirectories(searchOrder);
        }
    }
}
