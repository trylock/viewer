using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.IO;
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
        void InvalidFileName(string fileName);

        /// <summary>
        /// Show confirm dialog of file deletion. 
        /// </summary>
        /// <param name="fileName">Full directory path</param>
        /// <returns>true iff user confirmed that he/she wants to delete the file</returns>
        bool ConfirmDelete(string fileName);

        /// <summary>
        /// Show confirm dialog of deletion of a list of files
        /// </summary>
        /// <param name="fileName">List of files to delete</param>
        /// <returns>true iff user confirmed that he/she wants to delete all the files</returns>
        bool ConfirmDelete(IEnumerable<string> fileName);

        /// <summary>
        /// Show error message: failed to move from <paramref name="sourcePath"/> to <paramref name="destinationPath"/>
        /// </summary>
        /// <param name="sourcePath">Source path</param>
        /// <param name="destinationPath">Destination path</param>
        void FailedToMove(string sourcePath, string destinationPath);

        /// <summary>
        /// Specified path was too long
        /// </summary>
        /// <param name="path">Path which caused the error</param>
        void PathTooLong(string path);

        /// <summary>
        /// Specified file is in use.
        /// </summary>
        /// <param name="path">Path to a file</param>
        void FileInUse(string path);

        /// <summary>
        /// Notifies user that the previous operation on <paramref name="path"/> has failed and
        /// prompts the user to retry the operation.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <returns>true iff user has confirmed to perform the previous operation again</returns>
        bool RetryFileInUse(string path);
    }

    [Export(typeof(IFileSystemErrorView))]
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

        public void InvalidFileName(string fileName)
        {
            MessageBox.Show(
                string.Format(Resources.InvalidFileName_Message, fileName, PathUtils.GetInvalidFileCharacters()),
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

        public bool ConfirmDelete(IEnumerable<string> files)
        {
            var count = 0;
            string first = null;
            foreach (var file in files)
            {
                if (first == null)
                {
                    first = file;
                }

                ++count;
            }

            if (count == 1)
            {
                return ConfirmDelete(first);
            }

            var result = MessageBox.Show(
                string.Format(Resources.ConfirmDeleteAll_Message, count),
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
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public void PathTooLong(string path)
        {
            MessageBox.Show(
                string.Format(Resources.PathTooLong_Message, path), 
                Resources.PathTooLong_Label, 
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public void FileNotFound(string filePath)
        {
            MessageBox.Show(
                string.Format(Resources.FileNotFound_Message, filePath),
                Resources.FileNotFound_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public void FileInUse(string filePath)
        {
            MessageBox.Show(
                string.Format(Resources.FileInUse_Message, filePath),
                Resources.FileInUse_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public bool RetryFileInUse(string filePath)
        {
            return MessageBox.Show(
                string.Format(Resources.RetryFileInUse_Message, filePath),
                Resources.FileInUse_Label,
                MessageBoxButtons.RetryCancel,
                MessageBoxIcon.Warning) == DialogResult.Retry;
        }
    }
}
