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
using Viewer.Properties;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Explorer
{
    public partial class DirectoryTreeControl : WindowView, IDirectoryTreeView
    {
        public DirectoryTreeControl(string name)
        {
            InitializeComponent();

            Text = name;
            
            var list = new ImageList();
            list.Images.Add(Resources.Directory);

            TreeView.ImageList = list;
            TreeView.ImageIndex = 0;
            TreeView.Sorted = true;
        }
        
        #region View interface
        
        public event EventHandler<DirectoryEventArgs> ExpandDirectory;
        public event EventHandler<RenameDirectoryEventArgs> RenameDirectory;
        public event EventHandler<DirectoryEventArgs> DeleteDirectory;
        public event EventHandler<DirectoryEventArgs> CreateDirectory;
        public event EventHandler<DirectoryEventArgs> OpenInExplorer;
        public event EventHandler<DirectoryEventArgs> CopyDirectory;
        public event EventHandler<PasteEventArgs> PasteToDirectory;
        public event EventHandler<DirectoryEventArgs> PasteClipboardToDirectory;
        
        public void LoadDirectories(IEnumerable<string> pathParts, IEnumerable<DirectoryView> subdirectories)
        {
            var result = FindNode(pathParts);
            if (!result.IsFound)
            {
                return;
            }

            TreeView.BeginUpdate();
            result.Children.Clear();
            foreach (var dir in subdirectories)
            {
                var node = result.Children.Add(dir.FileName, dir.UserName);
                if (dir.HasChildren)
                {
                    // add dummy child node so that the user is able to expand this node
                    node.Nodes.Add("", "");
                }
            }
            TreeView.EndUpdate();
        }

        public void RemoveDirectory(IEnumerable<string> pathParts)
        {
            var result = FindNode(pathParts);
            if (result.Node == null)
            {
                return;
            }

            result.Node.Remove();
        }

        public void AddDirectory(IEnumerable<string> parentPath, DirectoryView newDir)
        {
            var result = FindNode(parentPath);
            if (!result.IsFound)
            {
                return;
            }

            result.Children.Add(newDir.FileName, newDir.UserName);
        }

        public void SetDirectory(IEnumerable<string> path, DirectoryView newDir)
        {
            var result = FindNode(path);
            if (result.Node == null)
            {
                return;
            }

            result.Node.Text = newDir.UserName;
            result.Node.Name = newDir.FileName;
        }

        public void SelectDirectory(IEnumerable<string> path)
        {
            var result = FindNode(path);
            if (result.Node == null)
            {
                return;
            }

            result.Node.EnsureVisible();
            TreeView.SelectedNode = result.Node;
        }

        public void BeginEditDirectory(IEnumerable<string> path)
        {
            var result = FindNode(path);
            if (result.Node == null)
            {
                return;
            }

            result.Node.EnsureVisible();
            result.Node.BeginEdit();
            TreeView.SelectedNode = result.Node;
        }

        #endregion
        
        private string GetPath(TreeNode node)
        {
            var parts = new List<string>();
            while (node != null)
            {
                parts.Add(node.Name);
                node = node.Parent;
            }

            parts.Reverse();
            var fullPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts);
            if (parts.Count == 1)
            {
                return fullPath + Path.DirectorySeparatorChar;
            }
            return fullPath;
        }

        private struct TreeSearchResult
        {
            /// <summary>
            /// Parent node or 
            /// null if IsFound == false or 
            /// null if we have found a list of top level nodes
            /// </summary>
            public TreeNode Node;

            /// <summary>
            /// Children of the found node.
            /// null if we haven't found any node in the tree
            /// </summary>
            public TreeNodeCollection Children;

            public bool IsFound => Children != null;
        }
        
        private TreeSearchResult FindNode(IEnumerable<string> pathParts)
        {
            TreeNode node = null;
            var collection = TreeView.Nodes;
            foreach (var part in pathParts)
            {
                var found = false;
                foreach (TreeNode child in collection)
                {
                    if (child.Name == part)
                    {
                        collection = child.Nodes;
                        node = child;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return new TreeSearchResult();
                }
            }

            return new TreeSearchResult
            {
                Node = node,
                Children = collection
            };
        }

        private string GetSelectedNodePath()
        {
            if (TreeView.SelectedNode == null)
                return null;
            return GetPath(TreeView.SelectedNode);
        }
        
        #region TreeView Events

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeView.SelectedNode = e.Node;
            }
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            var info = TreeView.HitTest(e.Location);
            if (info.Node != null)
            {
                Cursor.Current = Cursors.Hand;
            }
            else
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void TreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            ExpandDirectory?.Invoke(sender, new DirectoryEventArgs(GetPath(e.Node)));
        }

        private void TreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            var node = e.Node;
            var path = GetPath(node);
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
            
            RenameDirectory?.Invoke(sender, new RenameDirectoryEventArgs(path, e.Label));
        }
        
        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                case Keys.Enter:
                    e.SuppressKeyPress = true;
                    ToggleMenuItem_Click(sender, e);
                    break;
            }
        }

        private void TreeView_DragDrop(object sender, DragEventArgs e)
        {
            var node = TreeView.GetNodeAt(TreeView.PointToClient(new Point(e.X, e.Y)));
            if (node != null)
            {
                var path = GetPath(node);
                PasteToDirectory?.Invoke(sender, new PasteEventArgs(path, e.Data, e.Effect));
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            TreeView.Focus();
            TreeView.SelectedNode = TreeView.GetNodeAt(TreeView.PointToClient(new Point(e.X, e.Y)));
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                if ((e.AllowedEffect & DragDropEffects.Move) != 0)
                {
                    e.Effect = DragDropEffects.Move;
                }
                else if ((e.AllowedEffect & DragDropEffects.Copy) != 0)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        
        private void TreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            var node = (TreeNode) e.Item;
            var path = GetPath(node);
            DoDragDrop(new DataObject(DataFormats.FileDrop, new[]{ path }), DragDropEffects.Copy);
        }

        #endregion


        #region Context Menu Events

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            TreeView.SelectedNode.EnsureVisible();
            TreeView.SelectedNode.BeginEdit();
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            var node = TreeView.SelectedNode;
            var path = GetPath(node);
            DeleteDirectory?.Invoke(sender, new DirectoryEventArgs(path));
        }
        
        private void NewFolderMenuItem_Click(object sender, EventArgs e)
        {
            var parentNode = TreeView.SelectedNode;
            var parentPath = GetPath(parentNode);
            CreateDirectory?.Invoke(sender, new DirectoryEventArgs(parentPath));
        }

        private void ToggleMenuItem_Click(object sender, EventArgs e)
        {
            var node = TreeView.SelectedNode;
            if (node == null)
                return;
            node.Toggle();
        }

        private void OpenInFileExplorerMenuItem_Click(object sender, EventArgs e)
        {
            var path = GetSelectedNodePath();
            if (path != null)
            {
                OpenInExplorer?.Invoke(sender, new DirectoryEventArgs(path));
            }
        }

        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
            var path = GetSelectedNodePath();
            if (path != null)
            {
                CopyDirectory?.Invoke(sender, new DirectoryEventArgs(path));
            }
        }
        
        private void PasteMenuItem_Click(object sender, EventArgs e)
        {
            var path = GetSelectedNodePath();
            if (path != null)
            {
                PasteClipboardToDirectory?.Invoke(sender, new DirectoryEventArgs(path));
                TreeView.SelectedNode.Expand();
            }
        }

        #endregion
    }
}
