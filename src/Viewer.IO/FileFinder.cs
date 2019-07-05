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
using Viewer.Core.Collections;

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
        /// Same as <see cref="GetDirectories()"/> but it searches the directories in alphabetical
        /// order.
        /// </summary>
        /// <returns>List of directories matching the pattern</returns>
        IEnumerable<string> GetDirectories();

        /// <summary>
        /// Find all directories which match <see cref="Pattern"/>. Directories are searched in
        /// order given by <paramref name="directoryComparer"/>.
        /// </summary>
        /// <param name="directoryComparer">
        /// Directory comparer which is used to sort the searched directories.
        /// </param>
        /// <returns>Found directories</returns>
        IEnumerable<string> GetDirectories(IComparer<string> directoryComparer);

        /// <summary>
        /// Find all directories which match <see cref="Pattern"/>. Directories are searched in
        /// order given by <paramref name="directoryComparer"/>.
        /// </summary>
        /// <param name="directoryComparer">
        /// Directory comparer which is used to sort the searched directories.
        /// </param>
        /// <param name="progress">
        /// Reports all searched directories (not just directories matched by the pattern)
        /// </param>
        /// <returns>Found directories</returns>
        IEnumerable<string> GetDirectories(
            IComparer<string> directoryComparer, 
            IProgress<string> progress);

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
            /// Number of pattern parts <see cref="Path"/> has matched.
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
            return GetDirectories(StringComparer.CurrentCultureIgnoreCase);
        }
        
        public IEnumerable<string> GetDirectories(IComparer<string> directoryComparer)
        {
            return GetDirectories(directoryComparer, new Progress<string>());
        }

        public IEnumerable<string> GetDirectories(
            IComparer<string> directoryComparer, 
            IProgress<string> progress)
        {
            return FindAll(directoryComparer, progress);
        }

        /// <inheritdoc />
        /// <summary>
        /// Compare search algorithm state paths using provided path comparer
        /// </summary>
        private class StateComparer : IComparer<State>
        {
            private readonly IComparer<string> _pathComparer;

            public StateComparer(IComparer<string> pathComparer)
            {
                _pathComparer = pathComparer ?? 
                                throw new ArgumentNullException(nameof(pathComparer));
            }

            public int Compare(State x, State y)
            {
                // handle null values
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

        /// <summary>
        /// Find all folders which match <see cref="Pattern"/>.
        /// </summary>
        /// <param name="pathComparer">Search order</param>
        /// <param name="progress">Reports all searched directory paths</param>
        /// <returns>All folders which match <see cref="Pattern"/></returns>
        private IEnumerable<string> FindAll(
            IComparer<string> pathComparer, 
            IProgress<string> progress)
        {
            var comparer = new StateComparer(pathComparer);
            var parts = Pattern.GetParts().ToList();
            if (parts.Count == 0)
            {
                yield break;
            }

            // the root directory does not exist 
            var firstPath = parts[0];
            if (!_fileSystem.DirectoryExists(firstPath))
            {
                yield break;
            }

            // pattern contains only the root directory
            if (parts.Count == 1)
            {
                yield return firstPath;
                yield break;
            }
            
            var states = new BinaryHeap<State>(comparer);
            states.Enqueue(new State(firstPath, 1));
            
            // find all directories which match given pattern
            while (states.Count > 0)
            {
                var state = states.Dequeue();
                progress.Report(state.Path);

                if (state.MatchedPartCount >= parts.Count)
                {
                    // this path has matched the whole pattern => return it
                    yield return state.Path;
                }
                else
                {
                    var part = parts[state.MatchedPartCount];
                    if (part == PathPattern.RecursivePattern)
                    {
                        // This part is after the first ** pattern.
                        // Use a different algorithm: search all directories once and use pattern
                        // matching to determine whether to include given directory in the result.
                        if (Pattern.Match(state.Path))
                        {
                            yield return state.Path;
                        }

                        // search all subdirectories exactly once
                        foreach (var child in EnumerateDirectories(state.Path, null))
                        {
                            // MatchedPartCount can be any value >= firstGeneralPatternIndex
                            states.Enqueue(new State(child, state.MatchedPartCount));
                        }
                    }
                    else if (!PathPattern.ContainsSpecialCharacters(part))
                    {
                        // path part is a relative path without any special characters
                        var path = Path.Combine(state.Path, part);
                        if (!_fileSystem.DirectoryExists(path))
                        {
                            continue;
                        }

                        states.Add(new State(path, state.MatchedPartCount + 1));
                    }
                    else // part has to be a simple pattern which does not contain **
                    {
                        Trace.Assert(part != PathPattern.RecursivePattern);
                        foreach (var dir in EnumerateDirectories(state.Path, part))
                        {
                            states.Add(new State(dir, state.MatchedPartCount + 1));
                        }
                    }
                }
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
