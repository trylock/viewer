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
    /// The pattern can contain special characters *, ? and **
    /// * matches any sequence of character except a directory separator
    /// ? matches any character except a directory separator
    /// ** matches any sequence of characters (even a directory separator)
    /// ** must be delimited with a directory separator from both sides or it has to be at the start or at the end
    /// ** matches even empty string (i.e. a/**/b matches a/b, a/x/b, a/x/y/b etc.)
    /// </summary>
    /// <example>
    ///     var finder = new FileFinder("C:/photos/**/vacation/*2017");
    ///     foreach (var directoryPath in finder.GetDirectories()) { ... }
    ///     foreach (var filePath in finder.GetFiles()) { ... }
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

        public FileFinder(IFileSystem fileSystem, string directoryPattern)
        {
            if (directoryPattern == null)
            {
                throw new ArgumentNullException(nameof(directoryPattern));
            }

            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _parts = ParsePattern(directoryPattern);
        }

        private List<string> ParsePattern(string directoryPattern)
        {
            var parts = new List<string>();
            var patternParts = PathUtils.Split(directoryPattern);
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
                    // add preceeding relative path if any
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
        /// Get directories matching given pattern
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
        /// Get files in directories matching the pattern
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
