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
        /// Function called when there is a progress done in copying a file
        /// </summary>
        /// <param name="name">File name</param>
        /// <remarks>true iff we should continue</remarks>
        public delegate bool CopyCallback(string name);

        /// <summary>
        /// Copy <paramref name="sourceDirectory"/> and all its contents recursively 
        /// to <paramref name="targetDirectory"/>.
        /// </summary>
        /// <param name="sourceDirectory">Source directory</param>
        /// <param name="targetDirectory">Target directory</param>
        /// <param name="overwrite">
        ///     true iff we should overwrite files in the <paramref name="targetDirectory"/>
        /// </param>
        /// <param name="progressCallback">
        ///     Function called before we start copying a file
        /// </param>
        /// <exception cref="DirectoryNotFoundException">
        ///     <paramref name="sourceDirectory"/> was not found
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="sourceDirectory"/> == <paramref name="targetDirectory"/>
        /// </exception>
        public static void Copy(
            string sourceDirectory, 
            string targetDirectory, 
            bool overwrite = false,
            CopyCallback progressCallback = null)
        {
            CopyAux(sourceDirectory, targetDirectory, overwrite, progressCallback);
        }

        private static bool CopyAux(
            string sourceDirectory,
            string targetDirectory,
            bool overwrite,
            CopyCallback progressCallback)
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
                if (progressCallback != null)
                {
                    var shouldContinue = progressCallback.Invoke(file.FullName);
                    if (!shouldContinue)
                        return false;
                }

                file.CopyTo(targetFile, overwrite);
            }


            // recursively copy subdirectories
            var folders = source.GetDirectories();
            foreach (var folder in folders)
            {
                var targetFolder = Path.Combine(targetDirectory, folder.Name);
                var shouldContinue = CopyAux(
                    folder.FullName,
                    targetFolder,
                    overwrite,
                    progressCallback);

                if (!shouldContinue)
                    return false;
            }

            return true;
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
            if (!PathUtils.IsValidFileName(newName))
                throw new ArgumentException(nameof(newName) + " is not a valid file name.");

            var basePath = fullPath.Substring(0, fullPath.LastIndexOfAny(PathUtils.PathSeparators));
            if (basePath.Length > 0 &&
                basePath[basePath.Length - 1] != Path.DirectorySeparatorChar &&
                basePath[basePath.Length - 1] != Path.AltDirectorySeparatorChar)
            {
                basePath += Path.DirectorySeparatorChar;
            }

            Directory.Move(fullPath, Path.Combine(basePath, newName));
        }

        /// <summary>
        /// Count files in directory
        /// </summary>
        /// <param name="fullPath">Path to a directory</param>
        /// <param name="isRecursive">Should we recursively count files in subdirectories</param>
        /// <returns>Number of files in the directory</returns>
        public static long CountFiles(string fullPath, bool isRecursive)
        {
            long count = Directory.EnumerateFiles(fullPath).Count();
            if (!isRecursive)
            {
                return count;
            }

            foreach (var dir in Directory.EnumerateDirectories(fullPath))
            {
                count += CountFiles(dir, true);
            }

            return count;
        }
    }
}
