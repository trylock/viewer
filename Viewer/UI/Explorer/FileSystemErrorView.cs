using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Properties;

namespace Viewer.UI.Explorer
{
    public interface IFileSystemErrorView
    {
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
        /// Show the file not found error message
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        void FileNotFound(string filePath);

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

        /// <summary>
        /// Show error message: failed to move from <paramref name="sourcePath"/> to <paramref name="destinationPath"/>
        /// </summary>
        /// <param name="sourcePath">Source path</param>
        /// <param name="destinationPath">Destination path</param>
        void FailedToMove(string sourcePath, string destinationPath);
    }

    public class FileSystemErrorView : IFileSystemErrorView
    {
        public void UnauthorizedAccess(string path)
        {
            MessageBox.Show(
                string.Format(Resources.UnauthorizedAccess_Message, path),
                Resources.UnauthorizedAccess_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public void DirectoryNotFound(string path)
        {
            MessageBox.Show(
                string.Format(Resources.DirectoryNotFound_Message, path),
                Resources.DirectoryNotFound_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public void InvalidFileName(string fileName, string invalidCharacters)
        {
            MessageBox.Show(
                string.Format(Resources.InvalidFileName_Message, fileName, invalidCharacters),
                Resources.InvalidFileName_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public bool ConfirmDelete(string fullPath)
        {
            var result = MessageBox.Show(
                string.Format(Resources.ConfirmDelete_Message, fullPath),
                Resources.ConfirmDelete_Label,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            return result == DialogResult.Yes;
        }

        public void FailedToMove(string sourcePath, string destinationPath)
        {
            MessageBox.Show(
                string.Format(Resources.FailedToMove_Message, sourcePath, destinationPath),
                Resources.FailedToMove_Label,
                MessageBoxButtons.OK);
        }

        public void FileNotFound(string filePath)
        {
            MessageBox.Show(
                string.Format(Resources.FileNotFound_Message, filePath),
                Resources.FileNotFound_Label,
                MessageBoxButtons.OK);
        }
    }
}
