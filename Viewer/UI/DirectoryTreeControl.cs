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

        public DirectoryTreeControl()
        {
            InitializeComponent();

            // initialize the tree view with ready logical drives
            TreeView.Sorted = true;
            TreeView.BeginUpdate();
            foreach (var drive in _controller.GetDrives())
            {
                var node = TreeView.Nodes.Add(drive, drive);
                foreach (var folder in _controller.GetDirectories(node.FullPath))
                {
                    node.Nodes.Add(folder, folder);
                }
            }
            TreeView.EndUpdate();
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
                    var subnode = node.Nodes.Add(directory, directory);
                    foreach (var subdirectory in _controller.GetDirectories(subnode.FullPath))
                    {
                        subnode.Nodes.Add(subdirectory, subdirectory);
                    }
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
            if (e.Button == MouseButtons.Right)
            {
                TreeView.SelectedNode = e.Node;
            }
        }

        private void TreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            UpdateSubdirectories(e.Node);
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
            TreeView.SelectedNode.BeginEdit();
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
            node.BeginEdit();
        }

        #endregion

        private void TreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            var node = e.Node;
            if (e.Label == null)
            {
                return;
            }

            // don't rename the directory if we haven't changed the name
            if (e.Label == node.Name)
            {
                e.CancelEdit = true;
                return;
            }

            // rename the directory
            try
            {
                _controller.Rename(node.FullPath, e.Label);

                // update the UI
                node.Name = e.Label;
                node.Text = e.Label;
                node.EndEdit(false);
            }
            catch (UnauthorizedAccessException)
            {
                UnauthorizedAccess(node.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                DirectoryNotFound(node.FullPath);
            }
        }
    }
}
