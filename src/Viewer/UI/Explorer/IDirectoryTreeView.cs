using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core.UI;

namespace Viewer.UI.Explorer
{
    internal class DirectoryView
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

    internal class DirectoryEventArgs : EventArgs
    {
        /// <summary>
        /// Full path to a expanded directory
        /// </summary>
        public string FullPath { get; }
        
        public DirectoryEventArgs(string fullPath)
        {
            FullPath = fullPath;
        }
    }

    internal class RenameDirectoryEventArgs : DirectoryEventArgs
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

    internal class PasteEventArgs : DirectoryEventArgs
    {
        /// <summary>
        /// Data to paste to the directory
        /// </summary>
        public IDataObject Data { get; }

        /// <summary>
        /// Determines how to paste the data (e.g. copy, move)
        /// </summary>
        public DragDropEffects Effect { get; }

        public PasteEventArgs(string fullPath, IDataObject data, DragDropEffects effect) : base(fullPath)
        {
            Data = data;
            Effect = effect;
        }
    }

    internal interface IDirectoryTreeView : IWindowView
    {
        /// <summary>
        /// Event called when user opens a directory 
        /// </summary>
        event EventHandler<DirectoryEventArgs> OpenDirectory;

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
        event EventHandler<DirectoryEventArgs> CreateDirectory;

        /// <summary>
        /// Event called when user requests to open a directory in system file explorer
        /// </summary>
        event EventHandler<DirectoryEventArgs> OpenInExplorer;

        /// <summary>
        /// Event called when user requests to copy a directory
        /// </summary>
        event EventHandler<DirectoryEventArgs> CopyDirectory;

        /// <summary>
        /// Event called when user requests to paste specified files to a directory
        /// </summary>
        event EventHandler<PasteEventArgs> PasteToDirectory;

        /// <summary>
        /// Event called when user requests to paste files from clipborad to a directory
        /// </summary>
        event EventHandler<DirectoryEventArgs> PasteClipboardToDirectory;

        /// <summary>
        /// Load subdirectories of given directory.
        /// </summary>
        /// <param name="pathParts">List of directory names which composes path to a directory</param>
        /// <param name="subdirectories">Subdirectories in given directory</param>
        /// <param name="expand">true iff the view should expand the directory</param>
        void LoadDirectories(IEnumerable<string> pathParts, IEnumerable<DirectoryView> subdirectories, bool expand);
        
        /// <summary>
        /// Remove directory from the tree.
        /// </summary>
        /// <param name="pathParts">Path to the directory</param>
        void RemoveDirectory(IEnumerable<string> pathParts);

        /// <summary>
        /// Add directory
        /// </summary>
        /// <param name="parentPath">Path to a parent</param>
        /// <param name="newDirectory">New directory to add</param>
        void AddDirectory(IEnumerable<string> parentPath, DirectoryView newDirectory);

        /// <summary>
        /// Set new directory view (e.g. after a rename operation)
        /// </summary>
        /// <param name="path">Path to old directory</param>
        /// <param name="directory">New directory view</param>
        void SetDirectory(IEnumerable<string> path, DirectoryView directory);

        /// <summary>
        /// Make sure given directory is visible and make it the selected directory.
        /// </summary>
        /// <param name="path">Path to a directory</param>
        void SelectDirectory(IEnumerable<string> path);

        /// <summary>
        /// Reset all highlighted nodes so that they are not highlighted anymore.
        /// </summary>
        void ResetHighlight();

        /// <summary>
        /// Highlight directory at <paramref name="path"/> in the view. Highlighting a directory
        /// will also select its node. There can be more than 1 highlighted node at a time. See
        /// <see cref="ResetHighlight"/> to reset highlight. Node highlighting is draw even
        /// if the window does not have focus.
        /// </summary>
        /// <param name="path">Path to a directory</param>
        void HighlightDirectory(IEnumerable<string> path);

        /// <summary>
        /// Make sure given directory is visible and begin editing its name.
        /// </summary>
        /// <param name="path">Path to a directory</param>
        void BeginEditDirectory(IEnumerable<string> path);
    }
}
