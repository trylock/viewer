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
    /// * is a sequence of characters but a directory separator 
    /// ? is a single character but a directory separator
    /// ** must be delimited with directory separators from both sides and it is an arbitrary sequence of characters (including the directory separator)
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

        private class PathPart
        {
            /// <summary>
            /// Relative path to a directory
            /// </summary>
            public string Path { get; }

            /// <summary>
            /// true iff Path contains * or ?
            /// </summary>
            public bool IsPattern { get; }

            public PathPart(string path)
            {
                Path = path;
                IsPattern = Path.Contains('*') || Path.Contains('?');
            }

            public override string ToString()
            {
                return Path;
            }
        }

        private readonly IFileSystem _fileSystem;
        private readonly IReadOnlyList<PathPart> _parts;

        public FileFinder(IFileSystem fileSystem, string directoryPattern)
        {
            if (directoryPattern == null)
            {
                throw new ArgumentNullException(nameof(directoryPattern));
            }

            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _parts = ParsePattern(directoryPattern);
        }

        private List<PathPart> ParsePattern(string directoryPattern)
        {
            var parts = new List<PathPart>();
            var patternParts = PathUtils.Split(directoryPattern);
            var pathBuilder = new StringBuilder();
            foreach (var part in patternParts)
            {
                if (!part.Contains('*') && !part.Contains('?'))
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
                        parts.Add(new PathPart(directoryPath));
                        pathBuilder = new StringBuilder();
                    }

                    // add directory name pattern
                    parts.Add(new PathPart(part));
                }
            }

            if (pathBuilder.Length > 0)
            {
                parts.Add(new PathPart(pathBuilder.ToString()));
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

            var firstPath = _parts[0].Path;
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
                        if (!part.IsPattern)
                        {
                            // path part is a relative path without any special characters
                            var path = Path.Combine(state.Path, part.Path);
                            if (!_fileSystem.DirectoryExists(path))
                            {
                                return;
                            }
                            
                            newLevel.Enqueue(new State(path, state.LastMatchedPartIndex + 1));
                        }
                        else if (part.Path == "**")
                        {
                            foreach (var dir in _fileSystem.EnumerateDirectories(state.Path))
                            {
                                newLevel.Enqueue(new State(dir, state.LastMatchedPartIndex + 1));
                                newLevel.Enqueue(new State(dir, state.LastMatchedPartIndex));
                            }
                        }
                        else
                        {
                            foreach (var dir in _fileSystem.EnumerateDirectories(state.Path, part.Path))
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

        /// <summary>
        /// Get files in directories matching the pattern
        /// </summary>
        /// <returns>List of files in directories which match the pattern</returns>
        public IEnumerable<string> GetFiles()
        {
            return GetDirectories().SelectMany(_fileSystem.EnumerateFiles);
        }
    }
}
