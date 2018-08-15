using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Viewer.IO
{
    /// <summary>
    /// Find files and directories based on a directory path pattern.
    /// The pattern can contain special characters <c>*</c>, <c>?</c> and <c>**</c>:
    /// <list type="bullet">
    ///     <item>
    ///         <description><c>*</c> matches any sequence of characters except a directory separator</description>
    ///     </item>
    ///     <item>
    ///         <description><c>?</c> matches any character except a directory separator</description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <c>**</c> matches any sequence of characters (even a directory separator).
    ///             It has to be delimited by directory separator from both sides (i.e., <c>a/b**/c</c> is invalid, but it can be replaced with <c>a/b*/**/c</c>).
    ///             It matches even an empty string (i.e., <c>a/**/b</c> matches <c>a/b</c>, <c>a/x/b</c>, <c>a/x/y/b</c> etc.)
    ///         </description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <example>
    ///     <code>
    ///         var finder = new FileFinder(fileSystem, "C:/photos/**/vacation/*2017");
    ///         foreach (var directoryPath in finder.GetDirectories()) { ... }
    ///         foreach (var filePath in finder.GetFiles()) { ... }
    ///     </code>
    /// </example>
    public class FileFinder 
    {
        private class State
        {
            public string Path { get; }
            public int LastMatchedPartIndex { get; }

            public State(string path, int lastMatchedPart)
            {
                Path = path;
                LastMatchedPartIndex = lastMatchedPart;
            }
        }
        
        private readonly IFileSystem _fileSystem;
        private readonly IReadOnlyList<string> _parts;
        private readonly Regex _regex;

        /// <summary>
        /// Create a file finder with <paramref name="directoryPattern"/>.
        /// </summary>
        /// <param name="fileSystem">Wrapper used to access file system</param>
        /// <param name="directoryPattern">Directory path pattern. See <see cref="FileFinder"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileSystem"/> or <paramref name="directoryPattern"/> is null</exception>
        public FileFinder(IFileSystem fileSystem, string directoryPattern)
        {
            if (directoryPattern == null)
            {
                throw new ArgumentNullException(nameof(directoryPattern));
            }

            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _parts = ParsePattern(directoryPattern);
            _regex = CompileRegex(directoryPattern);
        }

        /// <summary>
        /// Split the path using path parts which contain a pattern.
        /// For example: a/b/c?/d/e/f/**/g/* will be split into 6 parts:
        /// 'a/b', 'c?', 'd/e/f', '**', 'g' and '*'
        /// </summary>
        /// <param name="directoryPattern">Path pattern to split</param>
        /// <returns>Path parts</returns>
        private static List<string> ParsePattern(string directoryPattern)
        {
            var parts = new List<string>();
            var patternParts = PathUtils.Split(directoryPattern);

            // join parts without a pattern
            var pathBuilder = new StringBuilder();
            foreach (var part in patternParts)
            {
                if (!IsPattern(part))
                {
                    pathBuilder.Append(part);
                    pathBuilder.Append(Path.DirectorySeparatorChar);
                }
                else
                {
                    // add preceeding relative path 
                    if (pathBuilder.Length > 0)
                    {
                        var directoryPath = pathBuilder.ToString();
                        parts.Add(directoryPath);
                        pathBuilder = new StringBuilder();
                    }

                    // add directory name pattern
                    parts.Add(part);
                }
            }

            if (pathBuilder.Length > 0)
            {
                parts.Add(pathBuilder.ToString());
            }

            return parts;
        }

        /// <summary>
        /// Compile <paramref name="pattern"/> to a regex.
        /// </summary>
        /// <param name="pattern">Directory path pattern</param>
        /// <returns>Compiled regex</returns>
        private static Regex CompileRegex(string pattern)
        {
            var sb = new StringBuilder();
            sb.Append("^");
            var parts = PathUtils.Split(pattern).ToArray();
            var index = 0;
            foreach (var part in parts)
            {
                if (part == "**")
                {
                    sb.Append(@"(.*[/\\])?");
                }
                else
                {
                    var partPattern = part
                        .Replace("*", @"[^/\\]*")
                        .Replace("?", @"[^/\\]");
                    sb.Append(partPattern);

                    if (index < parts.Length - 1)
                    {
                        sb.Append(@"[/\\]");
                    }
                }

                ++index;
            }
            sb.Append(@"[/\\]*$");
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// Find all directories which match given pattern.
        /// This function will skip folders which throw <see cref="UnauthorizedAccessException"/> or <see cref="SecurityException"/>.
        /// </summary>
        /// <returns>List of directories matching the pattern</returns>
        public IEnumerable<string> GetDirectories()
        {
            if (_parts.Count == 0)
            {
                yield break;
            }

            var firstPath = _parts[0];
            if (!_fileSystem.DirectoryExists(firstPath))
            {
                yield break;
            }
            
            if (_parts.Count == 1)
            {
                yield return firstPath;
                yield break;
            }

            var states = new ConcurrentQueue<State>();
            states.Enqueue(new State(firstPath, 0));

            while (!states.IsEmpty)
            {
                var newLevel = new ConcurrentQueue<State>();
                var result = new ConcurrentQueue<string>();
                
                Parallel.ForEach(states, state =>
                {
                    if (state.LastMatchedPartIndex + 1 >= _parts.Count)
                    {
                        result.Enqueue(state.Path);
                    }
                    else
                    {
                        var part = _parts[state.LastMatchedPartIndex + 1];
                        if (!IsPattern(part))
                        {
                            // path part is a relative path without any special characters
                            var path = Path.Combine(state.Path, part);
                            if (!_fileSystem.DirectoryExists(path))
                            {
                                return;
                            }
                            
                            newLevel.Enqueue(new State(path, state.LastMatchedPartIndex + 1));
                        }
                        else if (part == "**")
                        {
                            // assume the pattern has been matched
                            newLevel.Enqueue(new State(state.Path, state.LastMatchedPartIndex + 1));
                            
                            // assume it has not been matched yet
                            foreach (var dir in EnumerateDirectories(state.Path))
                            {
                                newLevel.Enqueue(new State(dir, state.LastMatchedPartIndex));
                            }
                        }
                        else
                        {
                            foreach (var dir in EnumerateDirectories(state.Path, part))
                            {
                                newLevel.Enqueue(new State(dir, state.LastMatchedPartIndex + 1));
                            }
                        }
                    }
                });

                foreach (var item in result)
                {
                    yield return item;
                }

                states = newLevel;
            }
        }

        public bool Match(string path)
        {
            return _regex.IsMatch(path);
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

        private IEnumerable<string> EnumerateDirectories(string path, string pattern)
        {
            try
            {
                return _fileSystem.EnumerateDirectories(path, pattern);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SecurityException)
            {
            }

            return Enumerable.Empty<string>();
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

        /// <summary>
        /// Check whether given path contains a special pattern character
        /// </summary>
        /// <param name="path">Tested path</param>
        /// <returns>true iff <paramref name="path"/> contains a special pattern character</returns>
        private static bool IsPattern(string path)
        {
            return path.Contains('*') || path.Contains('?');
        }
    }
}
