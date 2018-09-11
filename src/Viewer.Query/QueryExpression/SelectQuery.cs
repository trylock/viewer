using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.IO;

namespace Viewer.Query.QueryExpression
{
    internal class SelectQuery : IExecutableQuery
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Text => $"select \"{_fileFinder.Pattern}\"";

        public IComparer<IEntity> Comparer => EntityComparer.Default;

        public IEnumerable<PathPattern> Patterns
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

            foreach (var file in EnumeratePaths(progress, cancellationToken))
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

        private IEnumerable<(string Path, bool IsFile)> EnumeratePaths(
            IProgress<QueryProgressReport> progress,
            CancellationToken token)
        {
            foreach (var dir in EnumerateDirectories())
            {
                token.ThrowIfCancellationRequested();

                // report that we have found a new folder
                progress.Report(new QueryProgressReport(ReportType.Folder, dir));

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
}
