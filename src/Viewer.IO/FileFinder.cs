﻿using System;
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
        string Pattern { get; }
        
        /// <summary>
        /// Find all directories which match <see cref="Pattern"/>. This function will skip folders
        /// which throw <see cref="UnauthorizedAccessException"/> or <see cref="SecurityException"/>.
        /// </summary>
        /// <returns>List of directories matching the pattern</returns>
        IEnumerable<string> GetDirectories();

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
                Path = PathUtils.NormalizePath(path);
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
        /// Get the longest prefix of path names from <paramref name="pattern"/> such that
        /// they don't contain any special pattern characters.
        /// </summary>
        /// <param name="pattern">Patter whose base path you want to get</param>
        /// <returns>
        /// Base path of the pattern or null if <paramref name="pattern"/> is empty, null or its
        /// first part contains special pattern characters.
        /// </returns>
        public static string GetBasePatternPath(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return null;

            var parts = PathUtils.Split(pattern);
            var prefixPath = "";
            foreach (var part in parts)
            {
                if (IsPattern(part))
                    break;
                prefixPath = Path.Combine(prefixPath, part) + Path.DirectorySeparatorChar;
            }

            if (prefixPath.Length <= 0)
            {
                return null;
            }

            return PathUtils.NormalizePath(prefixPath);
        }

        /// <summary>
        /// Get parent directory of <paramref name="pattern"/>. This function takes into account
        /// special pattern characters. Specifically, it can deal with patterns of type "a/b/**".
        /// </summary>
        /// <param name="pattern">Pattern whose parrent directory you want to get</param>
        /// <returns>
        /// Pattern which matches all parent directories of folders in <paramref name="pattern"/>.
        /// </returns>
        public static string GetParentDirectoryPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return null;

            var parts = Split(pattern).ToList();
            if (parts.Count <= 0)
            {
                return null;
            }

            if (parts.Count == 1 && parts[0] != "**" && parts[0] != "..")
            {
                return parts[0];
            }

            if (parts[parts.Count - 1] == "**" || parts[parts.Count - 1] == "..")
            {
                parts.Add("..");
            }
            else
            {
                parts.RemoveAt(parts.Count - 1);
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
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

            var visited = new ConcurrentDictionary<string, bool>();
            var states = new ConcurrentQueue<State>();
            states.Enqueue(new State(firstPath, 1));

            while (!states.IsEmpty)
            {
                var newLevel = new ConcurrentQueue<State>();

                Parallel.ForEach(states, state =>
                {
                    if (state.MatchedPartCount >= _parts.Count)
                    {
                        if (visited.TryAdd(state.Path, true))
                        {
                            onNext(state.Path);
                        }
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
