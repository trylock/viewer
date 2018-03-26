using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.FileExplorer
{
    public class DirectoryView
    {
        /// <summary>
        /// Name shown to the user
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Actual file name of the directory
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// True iff there are some subdirectories in this directory
        /// </summary>
        public bool HasChildren { get; set; }
    }

    public class DirectoryEventArgs : EventArgs
    {
        /// <summary>
        /// Full path to a expanded directory
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// true iff the operation was successful
        /// </summary>
        public bool IsSuccessful { get; private set; } = true;

        public DirectoryEventArgs(string fullPath)
        {
            FullPath = fullPath;
        }

        /// <summary>
        /// Cancel the event
        /// </summary>
        public void Cancel()
        {
            IsSuccessful = false;
        }
    }

    public class CreateDirectoryEventArgs : DirectoryEventArgs
    {
        /// <summary>
        /// Name of the new directory
        /// </summary>
        public string NewName { get; set; }

        public CreateDirectoryEventArgs(string fullPath) : base(fullPath)
        {
        }
    }

    public class RenameDirectoryEventArgs : DirectoryEventArgs
    {
        /// <summary>
        /// New name of the directory (just the name, not the full path)
        /// </summary>
        public string NewName { get; }

        public RenameDirectoryEventArgs(string fullPath, string newName) : base(fullPath)
        {
            NewName = newName;
        }
    }

    public interface IDirectoryTreeView
    {
        /// <summary>
        /// Event called when a directory is expanded
        /// </summary>
        event EventHandler<DirectoryEventArgs> ExpandDirectory;

        /// <summary>
        /// Event called when a directory should be renamed
        /// </summary>
        event EventHandler<RenameDirectoryEventArgs> RenameDirectory;

        /// <summary>
        /// Event called when user requests to delete the directory
        /// </summary>
        event EventHandler<DirectoryEventArgs> DeleteDirectory;

        /// <summary>
        /// Event called when user requests to create a new directory
        /// </summary>
        event EventHandler<CreateDirectoryEventArgs> CreateDirectory;

        /// <summary>
        /// Event called when user requests to open a directory in system file explorer
        /// </summary>
        event EventHandler<DirectoryEventArgs> OpenInExplorer;

        /// <summary>
        /// Event called when user requests to copy a directory
        /// </summary>
        event EventHandler<DirectoryEventArgs> CopyDirectory;

        /// <summary>
        /// Event called when user requests to cut a directory
        /// </summary>
        event EventHandler<DirectoryEventArgs> CutDirectory;

        /// <summary>
        /// Event called when user requests to paste files from clipborad to a directory
        /// </summary>
        event EventHandler<DirectoryEventArgs> PasteToDirectory;

        /// <summary>
        /// Load subdirectories of given directory.
        /// </summary>
        /// <param name="pathParts">List of directory names which composes path to a directory</param>
        /// <param name="subdirectories">Subdirectories in given directory</param>
        void LoadDirectories(IEnumerable<string> pathParts, IEnumerable<DirectoryView> subdirectories);

        /// <summary>
        /// Show the unauthorized access error message to the user.
        /// </summary>
        /// <param name="path">Path to a file/directory</param>
        void UnauthorizedAccess(string path);

        /// <summary>
        /// Show the directory not found error message to the user.
        /// </summary>
        /// <param name="path">Path to a directory</param>
        void DirectoryNotFound(string path);

        /// <summary>
        /// Show the invalid file name error message to the user
        /// </summary>
        /// <param name="fileName">Invalid file name</param>
        /// <param name="invalidCharacters">Invalid characters in file name</param>
        void InvalidFileName(string fileName, string invalidCharacters);

        /// <summary>
        /// Show confirm dialog of directory deletion. 
        /// </summary>
        /// <param name="fileName">Full directory path</param>
        /// <returns>true iff user confirmed that he/she wants to delete the directory</returns>
        bool ConfirmDelete(string fileName);
    }
}
