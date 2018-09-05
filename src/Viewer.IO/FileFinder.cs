using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
    public class FileFinder 
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
                Path = path;
                MatchedPartCount = matchedPartCount;
            }
        }
        
        private readonly IFileSystem _fileSystem;
        private readonly IReadOnlyList<string> _parts;
        private readonly Regex _regex;

        /// <summary>
        /// Files and folders with at least one attribute from this list will not be included in
        /// the result.
        /// </summary>
        public FileAttributes HiddenAttributes { get; } = FileAttributes.System;

        /// <summary>
        /// Pattern of this file finder.
        /// </summary>
        public string Pattern { get; }

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
            Pattern = directoryPattern ?? throw new ArgumentNullException(nameof(directoryPattern));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            var invalidCharacters = Path.GetInvalidPathChars();
            if (Pattern.IndexOfAny(invalidCharacters) >= 0)
                throw new ArgumentException(nameof(directoryPattern) + " contains invalid characters.");

            _parts = ParsePattern(Pattern);
            _regex = CompileRegex(Pattern);
        }

        /// <summary>
        /// Split path using directory separators and join sequence of "**" into 1 part.
        /// For example:
        /// "a/b" => "a", "b"
        /// "a/**/b" => "a", "**", "b"
        /// "a/**/**/b" => "a", "**", "b"
        /// </summary>
        /// <param name="pattern">Path pattern to split</param>
        /// <returns>Path parts</returns>
        private static IEnumerable<string> Split(string pattern)
        {
            string lastPart = null;
            var parts = PathUtils.Split(pattern);
            foreach (var part in parts)
            {
                if (part == "**")
                {
                    if (lastPart != "**")
                    {
                        yield return part;
                    }
                }
                else
                {
                    yield return part;
                }

                lastPart = part;
            }
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
            var patternParts = Split(directoryPattern);

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
            var parts = Split(pattern).ToArray();
            for (var i = 0; i < parts.Length; ++i)
            {
                var part = parts[i];
                if (part == "**")
                {
                    if (parts.Length == 1) // the whole pattern is "**"
                    {
                        sb.Append(@".*");
                    }
                    else if (i == 0) 
                    {
                        // the pattern starts with "**" but this is not the last part
                        sb.Append(@"(.+[/\\])?");
                    }
                    else if (i == parts.Length - 1) 
                    {
                        // the pattern ends with "**" but this is not the first part
                        sb.Append(@"([/\\].+)?");
                    }
                    else 
                    {
                        // this part is in the middle
                        sb.Append(@"([/\\].+)?[/\\]");
                    }
                }
                else
                {
                    if (i > 0 && parts[i - 1] != "**")
                    {
                        sb.Append(@"[/\\]");
                    }

                    var partPattern = part
                        .Replace("*", @"[^/\\]*")
                        .Replace("?", @"[^/\\]");
                    sb.Append(partPattern);
                }
            }
            sb.Append(@"[/\\]*$");
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// Find all directories which match given pattern. This function will skip folders which
        /// throw <see cref="UnauthorizedAccessException"/> or <see cref="SecurityException"/>.
        /// </summary>
        /// <returns>List of directories matching the pattern</returns>
        public IEnumerable<string> GetDirectories()
        {
            var result = new ConcurrentQueue<string>();
            using (var resultCount = new SemaphoreSlim(0))
            {
                Task.Run(() =>
                {
                    try
                    {
                        FindAll(path =>
                        {
                            result.Enqueue(path);
                            resultCount.Release();
                        });
                    }
                    finally
                    {
                        result.Enqueue(null);
                        resultCount.Release();
                    }
                });

                for (;;)
                {
                    resultCount.Wait();
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
            }
        }

        private void FindAll(Action<string> onNext)
        {
            if (_parts.Count == 0)
            {
                return;
            }

            var firstPath = _parts[0];
            if (!_fileSystem.DirectoryExists(firstPath))
            {
                return;
            }
            
            if (_parts.Count == 1)
            {
                onNext(firstPath);
                return;
            }

            var states = new ConcurrentQueue<State>();
            states.Enqueue(new State(firstPath, 1));

            while (!states.IsEmpty)
            {
                var newLevel = new ConcurrentQueue<State>();
                
                Parallel.ForEach(states, state =>
                {
                    if (state.MatchedPartCount >= _parts.Count)
                    {
                        onNext(state.Path);
                    }
                    else
                    {
                        var part = _parts[state.MatchedPartCount];
                        if (!IsPattern(part))
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
            return _regex.IsMatch(path);
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
