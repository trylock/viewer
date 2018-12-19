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
        private Func<IEntity, bool> _compiledPredicate;
        private Func<IEntity, BaseValue> _compiledGroupFunc;

        private static readonly FileAttributes HiddenFlags = FileAttributes.System | 
                                                             FileAttributes.Temporary;

        /// <summary>
        /// Compile predicate on demand
        /// </summary>
        private Func<IEntity, bool> CompiledPredicate
        {
            get
            {
                if (_compiledPredicate == null)
                {
                    _compiledPredicate = Predicate.CompilePredicate(_runtime);
                }

                return _compiledPredicate;
            }
        }

        /// <summary>
        /// Compile group function on demand
        /// </summary>
        private Func<IEntity, BaseValue> CompiledGroupFunction
        {
            get
            {
                if (_compiledGroupFunc == null)
                {
                    _compiledGroupFunc = GroupFunction.CompileFunction(_runtime);
                }

                return _compiledGroupFunc;
            }
        }

        /// <summary>
        /// Textual representation of the query
        /// </summary>
        public string Text => ToString();

        /// <summary>
        /// Query predicate
        /// </summary>
        public ValueExpression Predicate { get; private set; } = ValueExpression.True;

        /// <summary>
        /// Group by expression
        /// </summary>
        public ValueExpression GroupFunction { get; private set; } = ValueExpression.Null;

        /// <summary>
        /// Query result comparer
        /// </summary>
        public IComparer<IEntity> Comparer { get; private set; } = EntityComparer.Default;

        /// <summary>
        /// Query comparer text
        /// </summary>
        public string ComparerText { get; private set; } = "";

        /// <summary>
        /// List of searched folder path patterns
        /// </summary>
        public IEnumerable<PathPattern> Patterns => Enumerable.Repeat(_fileFinder.Pattern, 1);

        public SimpleQuery(
            IEntityManager entityManager,
            IFileSystem fileSystem,
            IRuntime runtime,
            IPriorityComparerFactory priorityComparerFactory,
            string pattern)
        {
            _runtime = runtime;
            _fileSystem = fileSystem;
            _entityManager = entityManager;
            _priorityComparerFactory = priorityComparerFactory;
            _fileFinder = _fileSystem.CreateFileFinder(pattern);
        }

        /// <summary>
        /// Copy constructor. This does not copy computed values such as compiled predicate
        /// so that the predicate can be changed and recompiled later.
        /// </summary>
        /// <param name="other">Other simple query</param>
        private SimpleQuery(SimpleQuery other)
        {
            _runtime = other._runtime;
            _fileSystem = other._fileSystem;
            _entityManager = other._entityManager;
            _priorityComparerFactory = other._priorityComparerFactory;
            _fileFinder = other._fileFinder;
            Predicate = other.Predicate;
            Comparer = other.Comparer;
            ComparerText = other.ComparerText;
            GroupFunction = other.GroupFunction;
        }

        /// <summary>
        /// Get string representation of this simple query.
        /// </summary>
        /// <returns>
        /// String representation of the query. It is always a valid query.
        /// </returns>
        public override string ToString()
        {
            var text = $"select \"{_fileFinder.Pattern.Text}\"";
            if (Predicate != ValueExpression.True)
            {
                text += $" where {Predicate}";
            }

            if (!string.IsNullOrWhiteSpace(ComparerText))
            {
                text += $" order by {ComparerText}";
            }

            if (GroupFunction != ValueExpression.Null)
            {
                text += $" group by {GroupFunction}";
            }

            return text;
        }

        public BaseValue GetGroup(IEntity entity)
        {
            return CompiledGroupFunction(entity);
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

                    if (entity != null && CompiledPredicate(entity))
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
            
            return CompiledPredicate(entity);
        }

        #region Immutable factory functions

        /// <summary>
        /// Create a new simple query with a different comparer
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public SimpleQuery WithComparer(IComparer<IEntity> comparer, string text)
        {
            return new SimpleQuery(this)
            {
                Comparer = comparer,
                ComparerText = text
            };
        }

        /// <summary>
        /// Create a new simple query with a predicate: <c>oldPredicate and newPredicate</c>
        /// </summary>
        /// <param name="predicate">New predicate</param>
        /// <returns>New simple query</returns>
        public SimpleQuery AppendPredicate(ValueExpression predicate)
        {
            if (Predicate == ValueExpression.True)
            {
                return new SimpleQuery(this)
                {
                    Predicate = predicate
                };
            }

            var newPredicate = new AndExpression(
                predicate.Line,
                predicate.Column,
                Predicate,
                predicate);
            return new SimpleQuery(this)
            {
                Predicate = newPredicate
            };
        }

        /// <summary>
        /// Create a new simple query with a different group by expression
        /// </summary>
        /// <param name="expression">New group by expression</param>
        /// <returns>New simple query</returns>
        public SimpleQuery WithGroupFunction(ValueExpression expression)
        {
            return new SimpleQuery(this)
            {
                GroupFunction = expression
            };
        }

        #endregion

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
