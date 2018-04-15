namespace Viewer.UI.Explorer
{
    partial class DirectoryTreeView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.TreeView = new System.Windows.Forms.TreeView();
            this.FileContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ToggleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenInFileExplorerMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.CopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.DeleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RenameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.NewFolderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FileContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // TreeView
            // 
            this.TreeView.AllowDrop = true;
            this.TreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TreeView.ContextMenuStrip = this.FileContextMenu;
            this.TreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeView.FullRowSelect = true;
            this.TreeView.HotTracking = true;
            this.TreeView.Indent = 10;
            this.TreeView.ItemHeight = 24;
            this.TreeView.LabelEdit = true;
            this.TreeView.Location = new System.Drawing.Point(0, 0);
            this.TreeView.Name = "TreeView";
            this.TreeView.ShowLines = false;
            this.TreeView.Size = new System.Drawing.Size(218, 330);
            this.TreeView.TabIndex = 0;
            this.TreeView.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.TreeView_AfterLabelEdit);
            this.TreeView.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.TreeView_AfterExpand);
            this.TreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.TreeView_ItemDrag);
            this.TreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.TreeView_NodeMouseClick);
            this.TreeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.TreeView_DragDrop);
            this.TreeView.DragOver += new System.Windows.Forms.DragEventHandler(this.TreeView_DragOver);
            this.TreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TreeView_KeyDown);
            this.TreeView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TreeView_MouseMove);
            // 
            // FileContextMenu
            // 
            this.FileContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.FileContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToggleMenuItem,
            this.OpenInFileExplorerMenuItem,
            this.toolStripSeparator3,
            this.CopyMenuItem,
            this.PasteMenuItem,
            this.toolStripSeparator2,
            this.DeleteMenuItem,
            this.RenameMenuItem,
            this.toolStripSeparator1,
            this.NewFolderMenuItem});
            this.FileContextMenu.Name = "FileContextMenu";
            this.FileContextMenu.Size = new System.Drawing.Size(217, 190);
            // 
            // ToggleMenuItem
            // 
            this.ToggleMenuItem.Name = "ToggleMenuItem";
            this.ToggleMenuItem.Size = new System.Drawing.Size(216, 24);
            this.ToggleMenuItem.Text = "Toggle";
            this.ToggleMenuItem.Click += new System.EventHandler(this.ToggleMenuItem_Click);
            // 
            // OpenInFileExplorerMenuItem
            // 
            this.OpenInFileExplorerMenuItem.Name = "OpenInFileExplorerMenuItem";
            this.OpenInFileExplorerMenuItem.Size = new System.Drawing.Size(216, 24);
            this.OpenInFileExplorerMenuItem.Text = "Open in File Explorer";
            this.OpenInFileExplorerMenuItem.Click += new System.EventHandler(this.OpenInFileExplorerMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(213, 6);
            // 
            // CopyMenuItem
            // 
            this.CopyMenuItem.Name = "CopyMenuItem";
            this.CopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.CopyMenuItem.Size = new System.Drawing.Size(216, 24);
            this.CopyMenuItem.Text = "Copy";
            this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
            // 
            // PasteMenuItem
            // 
            this.PasteMenuItem.Name = "PasteMenuItem";
            this.PasteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
            this.PasteMenuItem.Size = new System.Drawing.Size(216, 24);
            this.PasteMenuItem.Text = "Paste";
            this.PasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(213, 6);
            // 
            // DeleteMenuItem
            // 
            this.DeleteMenuItem.Name = "DeleteMenuItem";
            this.DeleteMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.DeleteMenuItem.Size = new System.Drawing.Size(216, 24);
            this.DeleteMenuItem.Text = "Delete";
            this.DeleteMenuItem.Click += new System.EventHandler(this.DeleteMenuItem_Click);
            // 
            // RenameMenuItem
            // 
            this.RenameMenuItem.Name = "RenameMenuItem";
            this.RenameMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.RenameMenuItem.Size = new System.Drawing.Size(216, 24);
            this.RenameMenuItem.Text = "Rename";
            this.RenameMenuItem.Click += new System.EventHandler(this.RenameMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(213, 6);
            // 
            // NewFolderMenuItem
            // 
            this.NewFolderMenuItem.Name = "NewFolderMenuItem";
            this.NewFolderMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.NewFolderMenuItem.Size = new System.Drawing.Size(216, 24);
            this.NewFolderMenuItem.Text = "New Folder";
            this.NewFolderMenuItem.Click += new System.EventHandler(this.NewFolderMenuItem_Click);
            // 
            // DirectoryTreeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(218, 330);
            this.Controls.Add(this.TreeView);
            this.Name = "DirectoryTreeView";
            this.FileContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView TreeView;
        private System.Windows.Forms.ContextMenuStrip FileContextMenu;
        private System.Windows.Forms.ToolStripMenuItem RenameMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem DeleteMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem NewFolderMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CopyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OpenInFileExplorerMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem ToggleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem PasteMenuItem;
    }
}
