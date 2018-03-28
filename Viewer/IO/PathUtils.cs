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

        /// <summary>
        /// Check whether given string could be a valid file/folder name
        /// </summary>
        /// <param name="name">Name of a file</param>
        /// <returns>true iff given value could be a valid file/folder name</returns>
        public static bool IsValidFileName(string name)
        {
            return !String.IsNullOrEmpty(name) &&
                   name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }
}
