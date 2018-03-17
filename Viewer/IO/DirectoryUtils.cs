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
    }
}
