using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Properties;

namespace Viewer.UI
{
    public class DirectoryController
    {
        public struct DriveView
        {
            /// <summary>
            /// Name in the file system without directory separator (e.g. "C:")
            /// </summary>
            public string Key;

            /// <summary>
            /// User readable name
            /// </summary>
            public string Name;
        }

        /// <summary>
        /// Directory with at least one of these flags will be hidden.
        /// </summary>
        public FileAttributes HideFlags { get; set; } = FileAttributes.Hidden;

        /// <summary>
        /// Directory separator characters
        /// </summary>
        public static char[] DirectorySeparators = new char[]
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        /// <summary>
        /// Get list of ready drive names.
        /// </summary>
        /// <returns>Drive names</returns>
        public IEnumerable<DriveView> GetDrives()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                    continue;

                var name = drive.Name.Substring(0, drive.Name.IndexOfAny(DirectorySeparators));

                if (drive.VolumeLabel != null && drive.VolumeLabel.Length > 0)
                {
                    yield return new DriveView
                    {
                        Key = name,
                        Name = drive.VolumeLabel + " (" + name + ")",
                    };
                }
                else
                {
                    yield return new DriveView
                    {
                        Key = name,
                        Name = name,
                    };
                }
            }
        }

        /// <summary>
        /// Get list of subdirectory names in a given directory
        /// </summary>
        /// <param name="parentDirPath">Parent directory path</param>
        /// <returns>List of subdirectories in given directory</returns>
        /// <exception cref="DirectoryNotFoundException">
        ///     <paramref name="parentDirPath"/> was not found
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     Access to the <paramref name="parentDirPath"/> was denied
        /// </exception>
        public IEnumerable<string> GetDirectories(string parentDirPath)
        {
            if (parentDirPath == null)
                throw new ArgumentNullException(nameof(parentDirPath));
           
            var di = new DirectoryInfo(parentDirPath);
            foreach (var directory in di.EnumerateDirectories())
            {
                if ((directory.Attributes & HideFlags) != 0)
                    continue;

                yield return directory.Name;
            }
        }

        /// <summary>
        /// Delete given directory and all files and folders in it.
        /// </summary>
        /// <param name="pathToDirectory">Path to a directory</param>
        /// <exception cref="DirectoryNotFoundException">
        ///     <paramref name="pathToDirectory"/> was not found
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     Access to the <paramref name="pathToDirectory"/> was denied
        /// </exception>
        public void Delete(string pathToDirectory)
        {
            Directory.Delete(pathToDirectory, true);
        }

        /// <summary>
        /// Rename <paramref name="fullPath"/> directory to <paramref name="newName"/>.
        /// </summary>
        /// <param name="fullPath">Full path to a directory</param>
        /// <param name="newName">New name (just the directory, without any directory separators)</param>
        public void Rename(string fullPath, string newName)
        {
            var invChars = Path.GetInvalidFileNameChars();
            if (newName.IndexOfAny(invChars) >= 0)
                throw new ArgumentException(
                    "File name '{newName}' contains invalid characters. " +
                    "Invalid characters are: " + string.Join(", ", invChars));
            
            var basePath = fullPath.Substring(0, fullPath.LastIndexOfAny(DirectorySeparators));
            if (basePath.Length > 0 &&
                basePath[basePath.Length - 1] != Path.DirectorySeparatorChar &&
                basePath[basePath.Length - 1] != Path.AltDirectorySeparatorChar)
            {
                basePath += Path.DirectorySeparatorChar;
            }

            Directory.Move(fullPath, Path.Combine(basePath, newName));
        }
        
        /// <summary>
        /// Create new directory: <paramref name="fullPath"/> + Path.DirectorySeparatorChar + <paramref name="dirName"/>.
        /// </summary>
        /// <param name="fullPath">Path to a directory</param>
        /// <param name="dirName">New directory name</param>
        public void CreateDirectory(string fullPath, string dirName)
        {
            Directory.CreateDirectory(Path.Combine(fullPath, dirName));
        }

        /// <summary>
        /// Open given file in system file explorer
        /// </summary>
        /// <param name="path">Paht to a file</param>
        public void OpenInExplorer(string path)
        {
            Process.Start(
                Resources.ExplorerProcessName, 
                string.Format(Resources.ExplorerOpenFolderArguments, path));
        }
    }
}
