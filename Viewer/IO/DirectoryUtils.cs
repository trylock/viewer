using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.IO
{
    public static class DirectoryUtils
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
        public static IEnumerable<string> SplitPath(string fullPath)
        {
            return fullPath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Copy <paramref name="sourceDirectory"/> and all its contents recursively 
        /// to <paramref name="targetDirectory"/>.
        /// </summary>
        /// <param name="sourceDirectory">Source directory</param>
        /// <param name="targetDirectory">Target directory</param>
        /// <param name="overwrite">
        ///     true iff we should overwrite files in the <paramref name="targetDirectory"/>
        /// </param>
        /// <exception cref="DirectoryNotFoundException">
        ///     <paramref name="sourceDirectory"/> was not found
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="sourceDirectory"/> == <paramref name="targetDirectory"/>
        /// </exception>
        public static void Copy(string sourceDirectory, string targetDirectory, bool overwrite = false)
        {
            var source = new DirectoryInfo(sourceDirectory);
            var target = new DirectoryInfo(targetDirectory);
            if (!source.Exists)
                throw new DirectoryNotFoundException(sourceDirectory);
            if (source.FullName == target.FullName)
                throw new ArgumentException(
                    "Source and target directories must not be the same");

            // create target directory if it does not exist
            Directory.CreateDirectory(targetDirectory);

            // copy all files in source directory to target directory
            var files = source.GetFiles();
            foreach (var file in files)
            {
                var targetFile = Path.Combine(targetDirectory, file.Name);
                file.CopyTo(targetFile, overwrite);
            }

            // recursively copy subdirectories
            var folders = source.GetDirectories();
            foreach (var folder in folders)
            {
                var targetFolder = Path.Combine(targetDirectory, folder.Name);
                Copy(folder.FullName, targetFolder, overwrite);
            }
        }
        
        /// <summary>
        /// Check whether given string could be a valid file/folder name
        /// </summary>
        /// <param name="name">Name of a file</param>
        /// <returns>true iff given value could be a valid file/folder name</returns>
        public static bool IsValidName(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        /// <summary>
        /// Rename <paramref name="fullPath"/> directory to <paramref name="newName"/>.
        /// </summary>
        /// <param name="fullPath">Full path to a directory</param>
        /// <param name="newName">New name (just the directory, without any directory separators)</param>
        /// <exception cref="ArgumentNullException">
        ///     Some argument is null
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="newName"/> is not a valid folder name
        /// </exception>
        public static void Rename(string fullPath, string newName)
        {
            if (newName == null)
                throw new ArgumentNullException(nameof(newName));
            if (fullPath == null)
                throw new ArgumentNullException(nameof(fullPath));
            if (!IsValidName(newName))
                throw new ArgumentException(nameof(newName) + " is not a valid file name.");

            var basePath = fullPath.Substring(0, fullPath.LastIndexOfAny(PathSeparators));
            if (basePath.Length > 0 &&
                basePath[basePath.Length - 1] != Path.DirectorySeparatorChar &&
                basePath[basePath.Length - 1] != Path.AltDirectorySeparatorChar)
            {
                basePath += Path.DirectorySeparatorChar;
            }

            Directory.Move(fullPath, Path.Combine(basePath, newName));
        }
    }
}
