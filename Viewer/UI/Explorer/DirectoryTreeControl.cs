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
        public DirectoryTreeControl()
        {
            InitializeComponent();
            
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
        public event EventHandler<CreateDirectoryEventArgs> CreateDirectory;
        public event EventHandler<DirectoryEventArgs> OpenInExplorer;
        public event EventHandler<DirectoryEventArgs> CopyDirectory;
        public event EventHandler<PasteEventArgs> PasteToDirectory;
        public event EventHandler<DirectoryEventArgs> PasteClipboardToDirectory;
        
        public void LoadDirectories(IEnumerable<string> pathParts, IEnumerable<DirectoryView> subdirectories)
        {
            var nodes = GetChildrenNodeCollection(pathParts);

            TreeView.BeginUpdate();
            nodes.Clear();
            foreach (var dir in subdirectories)
            {
                var node = nodes.Add(dir.FileName, dir.UserName);
                if (dir.HasChildren)
                {
                    // add dummy child node so that the user is able to expand this node
                    node.Nodes.Add("", "");
                }
            }
            TreeView.EndUpdate();
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

        private TreeNodeCollection GetChildrenNodeCollection(IEnumerable<string> pathParts)
        {
            var collection = TreeView.Nodes;
            foreach (var part in pathParts)
            {
                var found = false;
                foreach (TreeNode child in collection)
                {
                    if (child.Name == part)
                    {
                        collection = child.Nodes;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return null;
            }

            return collection;
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

            var args = new RenameDirectoryEventArgs(path, e.Label);
            RenameDirectory?.Invoke(sender, args);
            if (args.IsSuccessful)
            {
                node.Name = args.NewName;
                node.Text = args.NewName;
            }
            node.EndEdit(!args.IsSuccessful);
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
            var args = new DirectoryEventArgs(path);
            DeleteDirectory?.Invoke(sender, args);
            if (args.IsSuccessful)
            {
                node.Remove();
            }
        }
        
        private void NewFolderMenuItem_Click(object sender, EventArgs e)
        {
            var parentNode = TreeView.SelectedNode;
            var parentPath = GetPath(parentNode);
            var args = new CreateDirectoryEventArgs(parentPath);
            CreateDirectory?.Invoke(sender, args);
            if (args.IsSuccessful)
            {
                parentNode.Expand();

                // add a new node for the directory
                var node = parentNode.Nodes.Add(args.NewName, args.NewName);
                node.EnsureVisible();
                TreeView.SelectedNode = node;
                node.BeginEdit();
            }
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
