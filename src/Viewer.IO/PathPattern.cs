using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Viewer.IO
{
    /// <summary>
    /// This class represents a file system path pattern. It is immutable. Every modifying
    /// operation returns a new pattern.
    /// </summary>
    public class PathPattern
    {
        /// <summary>
        /// Textual representation of this pattern
        /// </summary>
        public string Text { get; }

        private readonly Lazy<Regex> _regex;

        /// <summary>
        /// Create a new path pattern from its textual representation.
        /// </summary>
        /// <param name="pattern">Path pattern</param>
        public PathPattern(string pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            var invalidCharacters = Path.GetInvalidPathChars();
            if (pattern.IndexOfAny(invalidCharacters) >= 0)
                throw new ArgumentException(nameof(pattern) + " contains invalid characters.");

            Text = pattern;
            _regex = new Lazy<Regex>(() => CompileRegex(Text));
        }

        /// <summary>
        /// Get textual representation of this pattern
        /// </summary>
        /// <returns>Textual representation of this path pattern</returns>
        public override string ToString()
        {
            return Text;
        }

        /// <summary>
        /// Split path using directory separators and join sequence of "**" parts into 1 part.
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
        /// Split the path using path parts which contain a pattern.
        /// For example: a/b/c?/d/e/f/**/g/* will be split into 6 parts:
        /// 'a/b', 'c?', 'd/e/f', '**', 'g' and '*'
        /// </summary>
        /// <returns>Pattern parts</returns>
        public IEnumerable<string> GetParts()
        {
            var parts = new List<string>();
            var patternParts = Split(Text);

            // join parts without a pattern
            var pathBuilder = new StringBuilder();
            foreach (var part in patternParts)
            {
                if (!ContainsSpecialCharacters(part))
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
        /// Check whether <paramref name="path"/> matches this pattern.
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>true iff <paramref name="path"/> matches this pattern</returns>
        public bool Match(string path)
        {
            return _regex.Value.IsMatch(path);
        }
        
        /// <summary>
        /// Get parent directories path pattern. This function takes into account special
        /// pattern characters. Specifically, it can deal with patterns of type "a/b/**".
        /// </summary>
        /// <returns>
        /// Pattern which matches all parent directories of folders in <see name="Text"/>.
        /// </returns>
        public PathPattern GetParent()
        {
            if (string.IsNullOrWhiteSpace(Text))
                return null;

            var parts = Split(Text).ToList();
            if (parts.Count <= 0)
            {
                return null;
            }

            if (parts.Count == 1 && parts[0] != "**" && parts[0] != "..")
            {
                return new PathPattern(parts[0]);
            }

            if (parts[parts.Count - 1] == "**" || parts[parts.Count - 1] == "..")
            {
                parts.Add("..");
            }
            else
            {
                parts.RemoveAt(parts.Count - 1);
            }

            return new PathPattern(string.Join(Path.DirectorySeparatorChar.ToString(), parts));
        }

        /// <summary>
        /// Get the longest prefix of file names from <see name="Text"/> such that they don't
        /// contain any special pattern characters.
        /// </summary>
        /// <returns>
        /// Base path of the pattern or null if <see  name="Text"/> is empty, null or its first
        /// part contains special pattern characters.
        /// </returns>
        public string GetBasePath()
        {
            if (string.IsNullOrWhiteSpace(Text))
                return null;

            var parts = PathUtils.Split(Text);
            var prefixPath = "";
            foreach (var part in parts)
            {
                if (ContainsSpecialCharacters(part))
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
        /// Check whether given path contains a special pattern character
        /// </summary>
        /// <param name="path">Tested path</param>
        /// <returns>true iff <paramref name="path"/> contains a special pattern character</returns>
        public static bool ContainsSpecialCharacters(string path)
        {
            return path.Contains('*') || path.Contains('?');
        }
    }
}
