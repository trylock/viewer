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
        private DirectoryController _controller = new DirectoryController();

        private TextBox _renameTextBox;

        public DirectoryTreeControl()
        {
            InitializeComponent();

            // initialize the tree view with ready logical drives
            TreeView.Sorted = true;
            TreeView.BeginUpdate();
            foreach (var drive in _controller.GetDrives())
            {
                var node = TreeView.Nodes.Add(drive, drive);
                node.Expand();
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
            var path = node.FullPath;

            TreeView.BeginUpdate();

            // remove old subdirectories
            node.Nodes.Clear();

            // add new subdirectories
            try
            {
                foreach (var directory in _controller.GetDirectories(path))
                {
                    node.Nodes.Add(directory, directory);
                }
            }
            catch (UnauthorizedAccessException)
            {
                UnauthorizedAccess(path);
            }
            catch (DirectoryNotFoundException)
            {
                node.Remove();
                node = null;
                DirectoryNotFound(path);
            }

            // make sure the node is expanded
            if (node != null)
                node.Expand();
            TreeView.EndUpdate();
        }

        private void EditName(TreeNode node)
        {
            _renameTextBox.Text = node.Text;
            _renameTextBox.Location = node.Bounds.Location;
            _renameTextBox.Show();
            _renameTextBox.BringToFront();
            _renameTextBox.Focus();
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

            return;

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
            EditName(TreeView.SelectedNode);
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
        
        private void NewFolderMenuItem_Click(object sender, EventArgs e)
        {
            var parentNode = TreeView.SelectedNode;
            _controller.CreateDirectory(parentNode.FullPath, "New Folder");

            // add a new node for the directory
            var node = parentNode.Nodes.Add("New Folder", "New Folder");
            node.EnsureVisible();
            TreeView.SelectedNode = node;
            EditName(node);
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
                    
                    // don't rename the directory if we haven't changed the name
                    if (_renameTextBox.Text == node.Name)
                    {
                        _renameTextBox.Hide();
                        return;
                    }

                    // rename the directory
                    try
                    {
                        _controller.Rename(node.FullPath, _renameTextBox.Text);

                        // update the UI
                        node.Text = _renameTextBox.Text;
                        node.Name = _renameTextBox.Text;
                        _renameTextBox.Hide();
                        TreeView.Sort();
                        node.EnsureVisible();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        UnauthorizedAccess(node.FullPath);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        DirectoryNotFound(node.FullPath);
                    }

                    break;
            }
        }
    }
}
