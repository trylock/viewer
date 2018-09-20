using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.IO
{
    /// <summary>
    /// Find files and directories based on a directory path pattern.
    /// The pattern can contain special characters <c>*</c>, <c>?</c> and <c>**</c>:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <c>*</c> matches any sequence of characters except a directory separator
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <c>?</c> matches any character except a directory separator
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <c>**</c> matches any sequence of characters (even a directory separator). It has
    ///         to be delimited by directory separator from both sides (i.e., <c>a/b**/c</c> is
    ///         invalid, but it can be replaced with <c>a/b*/**/c</c>). It matches even an empty
    ///         string (i.e., <c>a/**/b</c> matches <c>a/b</c>, <c>a/x/b</c>, <c>a/x/y/b</c> etc.)
    ///         </description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <example>
    /// <code>
    /// var finder = new FileFinder(fileSystem, "C:/photos/**/vacation/*2017");
    /// foreach (var directoryPath in finder.GetDirectories()) { ... }
    /// foreach (var filePath in finder.GetFiles()) { ... }
    /// </code>
    /// </example>
    public interface IFileFinder
    {
        /// <summary>
        /// Pattern of this file finder.
        /// </summary>
        PathPattern Pattern { get; }
        
        /// <summary>
        /// Same as <see cref="GetDirectories()"/> but it uses <see cref="CancellationToken.None"/>.
        /// </summary>
        /// <returns>List of directories matching the pattern</returns>
        IEnumerable<string> GetDirectories();

        /// <summary>
        /// Find all directories which match <see cref="Pattern"/>. This function will skip folders
        /// which throw <see cref="UnauthorizedAccessException"/> or <see cref="SecurityException"/>.
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancellation token which can be used to cancel this operation
        /// </param>
        /// <returns></returns>
        IEnumerable<string> GetDirectories(CancellationToken cancellationToken);

        /// <summary>
        /// Test whether <paramref name="path"/> matches <see cref="Pattern"/>.
        /// </summary>
        /// <param name="path">Path to test</param>
        /// <returns>true iff <paramref name="path"/> matches <see cref="Pattern"/></returns>
        bool Match(string path);
    }
    
    public class FileFinder : IFileFinder
    {
        private class State
        {
            /// <summary>
            /// Path to a directory
            /// </summary>
            public string Path { get; }

            /// <summary>
            /// Number of pattern pats <see cref="Path"/> has matched.
            /// </summary>
            public int MatchedPartCount { get; }

            public State(string path, int matchedPartCount)
            {
                Path = PathUtils.NormalizePath(path) + '/';
                MatchedPartCount = matchedPartCount;
            }
        }
        
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Files and folders with at least one attribute from this list will not be included in
        /// the result.
        /// </summary>
        public FileAttributes HiddenAttributes { get; } = FileAttributes.System;

        public PathPattern Pattern { get; }

        /// <summary>
        /// Create a file finder with <paramref name="directoryPattern"/>.
        /// </summary>
        /// <param name="fileSystem">Wrapper used to access file system</param>
        /// <param name="directoryPattern">
        /// Directory path pattern. See <see cref="FileFinder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileSystem"/> or <paramref name="directoryPattern"/> is null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="directoryPattern"/> contains invalid characters
        /// </exception>
        public FileFinder(IFileSystem fileSystem, string directoryPattern)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            Pattern = new PathPattern(directoryPattern);
        }

        public IEnumerable<string> GetDirectories()
        {
            return GetDirectories(CancellationToken.None);
        }
        
        public IEnumerable<string> GetDirectories(CancellationToken cancellationToken)
        {
            var result = new ConcurrentQueue<string>();
            using (var resultCount = new SemaphoreSlim(0))
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        FindAll(path =>
                        {
                            result.Enqueue(path);
                            resultCount.Release();
                        }, cancellationToken);
                    }
                    finally
                    {
                        result.Enqueue(null);
                        resultCount.Release();
                    }
                }, cancellationToken);

                for (;;)
                {
                    resultCount.Wait(cancellationToken);

                    if (!result.TryDequeue(out var path))
                    {
                        continue;
                    }

                    if (path == null)
                    {
                        break;
                    }

                    yield return path;
                }

                // propagate all exceptions to the caller
                task.Wait(cancellationToken);
            }
        }

        private void FindAll(Action<string> onNext, CancellationToken cancellationToken)
        {
            var parts = Pattern.GetParts().ToList();
            if (parts.Count == 0)
            {
                return;
            }

            var firstPath = parts[0];
            if (!_fileSystem.DirectoryExists(firstPath))
            {
                return;
            }

            if (parts.Count == 1)
            {
                onNext(firstPath);
                return;
            }

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken
            };
            var visited = new ConcurrentDictionary<string, bool>();
            var states = new ConcurrentQueue<State>();
            states.Enqueue(new State(firstPath, 1));

            while (!states.IsEmpty)
            {
                var newLevel = new ConcurrentQueue<State>();

                Parallel.ForEach(states, parallelOptions, state =>
                {
                    if (state.MatchedPartCount >= parts.Count)
                    {
                        if (visited.TryAdd(state.Path, true))
                        {
                            onNext(state.Path);
                        }
                    }
                    else
                    {
                        var part = parts[state.MatchedPartCount];
                        if (!PathPattern.ContainsSpecialCharacters(part))
                        {
                            // path part is a relative path without any special characters
                            var path = Path.Combine(state.Path, part);
                            if (!_fileSystem.DirectoryExists(path))
                            {
                                return;
                            }

                            newLevel.Enqueue(new State(path, state.MatchedPartCount + 1));
                        }
                        else if (part == "**")
                        {
                            // assume the pattern has been matched
                            newLevel.Enqueue(new State(state.Path, state.MatchedPartCount + 1));

                            // assume it has not been matched yet
                            foreach (var dir in EnumerateDirectories(state.Path, null))
                            {
                                newLevel.Enqueue(new State(dir, state.MatchedPartCount));
                            }
                        }
                        else
                        {
                            foreach (var dir in EnumerateDirectories(state.Path, part))
                            {
                                newLevel.Enqueue(new State(dir, state.MatchedPartCount + 1));
                            }
                        }
                    }
                });

                states = newLevel;
            }
        }

        public bool Match(string path)
        {
            return Pattern.Match(path);
        }

        private IEnumerable<string> EnumerateDirectories(string path, string pattern)
        {
            IEnumerable<string> dirs = null;
            try
            {
                if (pattern == null)
                {
                    dirs = _fileSystem.EnumerateDirectories(path);
                }
                else
                {
                    dirs = _fileSystem.EnumerateDirectories(path, pattern);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (FileNotFoundException)
            {
            }
            catch (IOException) // path is a file name
            {
            }

            return SelectDirectories(dirs);
        }

        private IEnumerable<string> SelectDirectories(IEnumerable<string> dirs)
        {
            if (dirs == null)
            {
                yield break;
            }

            foreach (var dir in dirs)
            {
                var attrs = _fileSystem.GetAttributes(dir);
                if ((attrs & HiddenAttributes) == 0)
                {
                    yield return dir;
                }
            }
        }

        /// <summary>
        /// Find all files in directories which match given pattern
        /// </summary>
        /// <returns>List of files in directories which match the pattern</returns>
        public IEnumerable<string> GetFiles()
        {
            foreach (var directory in GetDirectories())
            {
                IEnumerable<string> files = null;
                try
                {
                    files = _fileSystem.EnumerateFiles(directory);
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (SecurityException)
                {
                }

                if (files == null)
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }
            }
        }
    }
}
