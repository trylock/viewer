using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Properties;

namespace Viewer.UI
{
    public partial class DirectoryTreeControl : UserControl
    {
        /// <summary>
        /// Directory with at least one of these flags will be hidden.
        /// </summary>
        public FileAttributes HideFlags = FileAttributes.Hidden;

        public DirectoryTreeControl()
        {
            InitializeComponent();

            // initialize the tree view with ready logical drives
            TreeView.BeginUpdate();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                    continue;

                TreeView.Nodes.Add(drive.Name, drive.Name);
            }
            TreeView.EndUpdate();
        }

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.IsExpanded)
            {
                e.Node.Collapse();
            }
            else
            {
                UpdateSubdirectories(e.Node);
            }
        }

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

                    node.Nodes.Add(directory.FullName, directory.Name);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    string.Format(Resources.UnauthorizedAccess_Message, di.Name), 
                    Resources.UnauthorizedAccess_Label, 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Warning);
            }

            // make sure the node is expanded
            node.Expand();
            TreeView.EndUpdate();
        }
    }
}
