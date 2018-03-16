using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.IO;
using Viewer.Properties;

namespace Viewer.UI
{
    public partial class DirectoryTreeControl : UserControl
    {
        /// <summary>
        /// Directory with at least one of these flags will be hidden.
        /// </summary>
        public FileAttributes HideFlags { get; set; } = FileAttributes.Hidden;

        private TextBox _renameTextBox;

        public DirectoryTreeControl()
        {
            InitializeComponent();

            // initialize the tree view with ready logical drives
            TreeView.Sorted = true;
            TreeView.BeginUpdate();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                    continue;

                TreeView.Nodes.Add(drive.Name.Remove(drive.Name.IndexOf(Path.DirectorySeparatorChar)), drive.Name);
            }
            TreeView.EndUpdate();
            
            // create text box for renaming files
            _renameTextBox = new TextBox();
            _renameTextBox.KeyDown += RenameTextBox_KeyDown;
            _renameTextBox.Hide();
            Controls.Add(_renameTextBox);
        }
        
        /// <summary>
        /// Update subdirectories of given node.
        /// </summary>
        /// <param name="node">Node of the directory TreeView</param>
        private void UpdateSubdirectories(TreeNode node)
        {
            var di = new DirectoryInfo(node.FullPath);

            TreeView.BeginUpdate();

            // remove old subdirectories
            node.Nodes.Clear();

            // add new subdirectories
            try
            {
                foreach (var directory in di.EnumerateDirectories())
                {
                    if ((directory.Attributes & HideFlags) != 0)
                        continue;

                    node.Nodes.Add(directory.Name, directory.Name);
                }
            }
            catch (UnauthorizedAccessException)
            {
                UnauthorizedAccess(di.FullName);
            }
            catch (DirectoryNotFoundException)
            {
                node.Remove();
                node = null;
                DirectoryNotFound(di.FullName);
            }

            // make sure the node is expanded
            if (node != null)
                node.Expand();
            TreeView.EndUpdate();
        }
        
        private void UnauthorizedAccess(string path)
        {
            MessageBox.Show(
                string.Format(Resources.UnauthorizedAccess_Message, path),
                Resources.UnauthorizedAccess_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void DirectoryNotFound(string path)
        {
            MessageBox.Show(
                string.Format(Resources.DirectoryNotFound_Message, path),
                Resources.DirectoryNotFound_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        #region TreeView Events

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Open context menu for right click
            if (e.Button == MouseButtons.Right)
            {
                TreeView.SelectedNode = e.Node;
                return;
            }

            // toggle node
            if (e.Node.IsExpanded)
            {
                e.Node.Collapse();
            }
            else
            {
                UpdateSubdirectories(e.Node);
            }
        }
        
        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            var info = TreeView.HitTest(e.Location);
            if (info.Node == null)
            {
                Cursor.Current = Cursors.Default;
            }
            else
            {
                Cursor.Current = Cursors.Hand;
            }
        }

        private void TreeView_MouseLeave(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.Default;
        }

        #endregion
        

        #region Context Menu Events

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            var node = TreeView.SelectedNode;
            _renameTextBox.Text = node.Text;
            _renameTextBox.Location = node.Bounds.Location;
            _renameTextBox.Show();
            _renameTextBox.BringToFront();
            _renameTextBox.Focus();
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            var node = TreeView.SelectedNode;
            var result = MessageBox.Show(
                string.Format(Resources.ConfirmDelete_Message, node.FullPath),
                Resources.ConfirmDelete_Label,
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question, 
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Directory.Delete(node.FullPath, true);
                    node.Remove();
                }
                catch (DirectoryNotFoundException)
                {
                    node.Remove();
                    DirectoryNotFound(node.FullPath);
                }
                catch (UnauthorizedAccessException)
                {
                    UnauthorizedAccess(node.FullPath);
                }
            }
        }

        #endregion

        private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    e.SuppressKeyPress = true;
                    _renameTextBox.Hide();
                    break;
                case Keys.Enter:
                    e.SuppressKeyPress = true;
                    var node = TreeView.SelectedNode;
                    if (node == null)
                        return;

                    // set new directory name in the UI
                    var oldName = node.Name;
                    var oldPath = node.FullPath;
                    node.Text = _renameTextBox.Text;
                    node.Name = _renameTextBox.Name;
                    _renameTextBox.Hide();

                    // don't rename the directory if we haven't changed the name
                    if (oldPath == node.FullPath)
                    {
                        return;
                    }

                    // rename the directory
                    try
                    {
                        Directory.Move(oldPath, node.FullPath);
                        TreeView.Sort();
                        node.EnsureVisible();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        UnauthorizedAccess(oldPath);
                        node.Text = oldName;
                        node.Name = oldName;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        DirectoryNotFound(oldPath);
                        node.Text = oldName;
                        node.Name = oldName;
                    }

                    break;
            }
        }
    }
}
