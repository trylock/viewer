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
        /// Find all directories which match <see cref="Pattern"/>. Directories searched in
        /// order by given comparer.
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancellation token which can be used to cancel the search.
        /// </param>
        /// <param name="directoryComparer">
        /// Directory comparer which is used to sort the searched directories.
        /// </param>
        /// <returns>Found directories</returns>
        IEnumerable<string> GetDirectories(
            CancellationToken cancellationToken, 
            IComparer<string> directoryComparer);

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
            return GetDirectories(cancellationToken, Comparer<string>.Default);
        }

        public IEnumerable<string> GetDirectories(
            CancellationToken cancellationToken, 
            IComparer<string> directoryComparer)
        {
            return FindAll(cancellationToken, directoryComparer);
        }

        private class StateComparer : IComparer<State>
        {
            private readonly IComparer<string> _pathComparer;

            public StateComparer(IComparer<string> pathComparer)
            {
                _pathComparer = pathComparer ?? throw new ArgumentNullException(nameof(pathComparer));
            }

            public int Compare(State x, State y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }
                else if (x == null)
                {
                    return -1;
                }
                else if (y == null)
                {
                    return 1;
                }
                return _pathComparer.Compare(x.Path, y.Path);
            }
        }

        private IEnumerable<string> FindAll(
            CancellationToken cancellationToken, 
            IComparer<string> pathComparer)
        {
            var comparer = new StateComparer(pathComparer);
            var parts = Pattern.GetParts().ToList();
            if (parts.Count == 0)
            {
                yield break;
            }

            var firstPath = parts[0];
            if (!_fileSystem.DirectoryExists(firstPath))
            {
                yield break;
            }

            if (parts.Count == 1)
            {
                yield return firstPath;
                yield break;
            }
            
            var visited = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var states = new Queue<State>();
            states.Enqueue(new State(firstPath, 1));

            while (states.Count > 0)
            {
                var newLevel = new List<State>();

                foreach (var state in states)
                {
                    if (state.MatchedPartCount >= parts.Count)
                    {
                        if (!visited.Contains(state.Path))
                        {
                            yield return state.Path;
                            visited.Add(state.Path);
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
                                continue;
                            }

                            newLevel.Add(new State(path, state.MatchedPartCount + 1));
                        }
                        else if (part == "**")
                        {
                            // assume the pattern has been matched
                            newLevel.Add(new State(state.Path, state.MatchedPartCount + 1));

                            // assume it has not been matched yet
                            foreach (var dir in EnumerateDirectories(state.Path, null))
                            {
                                newLevel.Add(new State(dir, state.MatchedPartCount));
                            }
                        }
                        else
                        {
                            foreach (var dir in EnumerateDirectories(state.Path, part))
                            {
                                newLevel.Add(new State(dir, state.MatchedPartCount + 1));
                            }
                        }
                    }
                }

                newLevel.Sort(comparer);

                states = new Queue<State>(newLevel);
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
