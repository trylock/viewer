using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.IO
{
    public static class PathUtils
    {
        /// <summary>
        /// List of characters that could be used as a path separators
        /// </summary>
        public static char[] PathSeparators =
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        /// <summary>
        /// Split filesystem path to parts (file and directory names)
        /// </summary>
        /// <param name="fullPath">Path to a directory/file</param>
        /// <returns>Parts of the path</returns>
        public static IEnumerable<string> Split(string fullPath)
        {
            return fullPath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Get last part of given path (i.e. file/folder name).
        /// Note: if the last character is a directory separator, it will be removed
        /// </summary>
        /// <param name="path">Path to a file or folder</param>
        /// <returns>Last part of the path</returns>
        public static string GetLastPart(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var lastPartLength = 0; 
            var separatorIndex = path.Length - 1;
            while (separatorIndex >= 0)
            {
                // check if there is a path separator at this position
                var isSeparator = false;
                foreach (var sep in PathSeparators)
                {
                    if (sep == path[separatorIndex])
                    {
                        isSeparator = true;
                        break;
                    }
                }

                if (isSeparator && separatorIndex + 1 != path.Length)
                {
                    break;
                }
                else if (!isSeparator)
                {
                    ++lastPartLength;
                }

                --separatorIndex;
            }
            
            return path.Substring(separatorIndex + 1, lastPartLength);
        }

        public static string GetDirectoryPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                return "";

            // if the last character is a path separator, ignore it
            var index = path.Length - 1;
            if (PathSeparators.Contains(path[index]))
            {
                --index;
            }

            // find the next path separator
            for (; index >= 0; --index)
            {
                if (PathSeparators.Contains(path[index]))
                {
                    break;
                }
            }

            if (index < 0)
            {
                return "";
            }

            return path.Substring(0, index);
        }

        /// <summary>
        /// Check whether given string could be a valid file/folder name
        /// </summary>
        /// <param name="name">Name of a file</param>
        /// <returns>true iff given value could be a valid file/folder name</returns>
        public static bool IsValidFileName(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        /// <summary>
        /// Get list of printable invalid file name characters in a string.
        /// </summary>
        /// <returns>String containing invalid file name characters separated by comma</returns>
        public static string GetInvalidFileCharacters()
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in invalid)
            {
                if (char.IsControl(c) && !char.IsWhiteSpace(c))
                    continue;

                if (c == '\n')
                    sb.Append("\\n");
                else if (c == '\t')
                    sb.Append("\\t");
                else if (c == '\r')
                    sb.Append("\\r");
                else
                    sb.Append(c);
                sb.Append(", ");
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 3, 3); // remove the last separator
            }

            return sb.ToString();
        }
    }
}
