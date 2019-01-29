using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        /// Pattern which matches parent directory
        /// </summary>
        public const string ParentDirectoryPattern = "..";

        /// <summary>
        /// Pattern which matches current directory
        /// </summary>
        public const string CurrentDirectoryPattern = ".";

        /// <summary>
        /// Pattern which matches 0..n arbitrary subdirectories
        /// </summary>
        public const string RecursivePattern = "**";

        /// <summary>
        /// Textual representation of this pattern
        /// </summary>
        public string Text { get; }

        private readonly Lazy<Regex> _regex;

        /// <summary>
        /// Create a new path pattern from its textual representation.
        /// </summary>
        /// <remarks>
        /// The pattern regex is compiled on demand so that this constructor is
        /// relatively inexpensive.
        /// </remarks>
        /// <param name="pattern">Path pattern</param>
        public PathPattern(string pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            var invalidCharacters = Path.GetInvalidPathChars();
            if (pattern.IndexOfAny(invalidCharacters) >= 0)
                throw new ArgumentException(nameof(pattern) + " contains invalid characters.");
            
            Text = NormalizePath(pattern);
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
        /// Replace directory name separators with / (forward slash), interpret .. and .
        /// </summary>
        /// <param name="path">Path to normalize</param>
        /// <returns>Normalized path</returns>
        private static string NormalizePath(string path)
        {
            var parts = new Stack<string>();
            foreach (var part in Split(path))
            {
                if (part == ParentDirectoryPattern)
                {
                    if (parts.Count > 1) // do not remove the last part
                    {
                        parts.Pop();
                    }
                }
                else if (part == CurrentDirectoryPattern)
                {
                    // skip
                }
                else
                {
                    parts.Push(part);
                }
            }

            return string.Join("/", parts.Reverse());
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
                if (part == RecursivePattern)
                {
                    if (lastPart != RecursivePattern)
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
        /// Given a path <paramref name="pattern"/>, find a regular expressin which acepts the
        /// same language.
        /// </summary>
        /// <remarks>
        /// When it comes to directory separators, the returned regexp will allow an arbitrary
        /// number of them at the end of a path (including no separator at all). Individual folders
        /// have to be separated with exactly one directory separator.
        /// </remarks>
        /// <param name="pattern">Path pattern</param>
        /// <returns>Equivalent regular expression</returns>
        private static string BuildRegex(string pattern)
        {
            var sb = new StringBuilder();
            sb.Append("^");
            var parts = Split(pattern).ToList();
            for (var i = 0; i < parts.Count; ++i)
            {
                var part = parts[i];
                if (part == RecursivePattern)
                {
                    if (parts.Count == 1) // the whole pattern is "**"
                    {
                        sb.Append(@"(([^/\\]+[/\\])*([^/\\]+))?");
                    }
                    else if (i == 0)
                    {
                        // the pattern starts with "**" but this is not the last part
                        sb.Append(@"([^/\\]+[/\\])*");
                    }
                    else if (i == parts.Count - 1)
                    {
                        // the pattern ends with "**" but this is not the first part
                        sb.Append(@"([/\\][^/\\]+)*");
                    }
                    else
                    {
                        // this part is in the middle
                        sb.Append(@"([/\\][^/\\]+)*[/\\]");
                    }
                }
                else
                {
                    if (i > 0 && parts[i - 1] != "**")
                    {
                        sb.Append(@"[/\\]");
                    }

                    if (part == "*") // make sure "*" won't match an empty string
                    {
                        sb.Append(@"[^/\\]+");
                    }
                    else
                    {
                        var partPattern = part
                            .Replace("*", @"[^/\\]*")
                            .Replace("?", @"[^/\\]");
                        sb.Append(partPattern);
                    }
                }
            }
            sb.Append(@"[/\\]*$");
            return sb.ToString();
        }

        /// <summary>
        /// Compile <paramref name="pattern"/> to a regex.
        /// </summary>
        /// <param name="pattern">Directory path pattern</param>
        /// <returns>Compiled regex</returns>
        private static Regex CompileRegex(string pattern)
        {
            return new Regex(BuildRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
        /// Get parent directories path pattern. The last part of the pattern is removed.
        /// This applies even to the ** pattern.
        /// <example>
        /// <code>
        /// GetPattern("a/b") -> "a"
        /// GetPattern("a/b?c") -> "a"
        /// GetPattern("a/b*c") -> "a"
        /// GetPattern("a/**") -> "a"
        /// GetPattern("a") -> "a"
        /// </code>
        /// </example>
        /// </summary>
        /// <returns>
        /// Pattern without the last part or null if <see cref="Text"/> is empty or null.
        /// </returns>
        public PathPattern GetParent()
        {
            if (string.IsNullOrWhiteSpace(Text))
                return null;

            // remove the last pattern part 
            // note, it won't be . or .. since pattern path is normalized
            var sb = new StringBuilder();
            using (var enumerator = Split(Text).GetEnumerator())
            {
                var hasNext = enumerator.MoveNext();
                var isFirst = true;

                while (hasNext)
                {
                    var value = enumerator.Current;
                    hasNext = enumerator.MoveNext();

                    // if this is not the last part or this is the first part
                    // i.e., the first part won't be removed no matter what
                    if (hasNext || isFirst)
                    {
                        sb.Append(value);
                        sb.Append('/');
                    }
                    isFirst = false;
                }
            }
            
            Trace.Assert(sb.Length > 0, "sb.Length > 0");
            
            // remove the last /
            sb.Remove(sb.Length - 1, 1);
            
            return new PathPattern(sb.ToString());
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
        /// <returns>
        /// true iff <paramref name="path"/> contains a special pattern character
        /// </returns>
        public static bool ContainsSpecialCharacters(string path)
        {
            return path.Contains('*') || path.Contains('?');
        }
    }
}
