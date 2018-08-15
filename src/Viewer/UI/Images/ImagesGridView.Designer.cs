namespace Viewer.UI.Images
{
    partial class ImagesGridView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImagesGridView));
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.ItemContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.OpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.CopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.DeleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RenameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PollTimer = new System.Windows.Forms.Timer(this.components);
            this.ShowCodeToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.BackToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ForwardToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.GridView = new Viewer.UI.Images.GridView();
            this.ItemContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // NameTextBox
            // 
            this.NameTextBox.Location = new System.Drawing.Point(53, 32);
            this.NameTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(87, 20);
            this.NameTextBox.TabIndex = 0;
            this.NameTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.NameTextBox.Visible = false;
            this.NameTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NameTextBox_KeyDown);
            this.NameTextBox.Leave += new System.EventHandler(this.NameTextBox_Leave);
            // 
            // ItemContextMenu
            // 
            this.ItemContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.ItemContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenMenuItem,
            this.toolStripSeparator1,
            this.CopyMenuItem,
            this.toolStripSeparator2,
            this.DeleteMenuItem,
            this.RenameMenuItem});
            this.ItemContextMenu.Name = "ItemContextMenu";
            this.ItemContextMenu.Size = new System.Drawing.Size(145, 104);
            // 
            // OpenMenuItem
            // 
            this.OpenMenuItem.Name = "OpenMenuItem";
            this.OpenMenuItem.Size = new System.Drawing.Size(144, 22);
            this.OpenMenuItem.Text = "Open";
            this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(141, 6);
            // 
            // CopyMenuItem
            // 
            this.CopyMenuItem.Name = "CopyMenuItem";
            this.CopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.CopyMenuItem.Size = new System.Drawing.Size(144, 22);
            this.CopyMenuItem.Text = "Copy";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(141, 6);
            // 
            // DeleteMenuItem
            // 
            this.DeleteMenuItem.Name = "DeleteMenuItem";
            this.DeleteMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.DeleteMenuItem.Size = new System.Drawing.Size(144, 22);
            this.DeleteMenuItem.Text = "Delete";
            // 
            // RenameMenuItem
            // 
            this.RenameMenuItem.Name = "RenameMenuItem";
            this.RenameMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.RenameMenuItem.Size = new System.Drawing.Size(144, 22);
            this.RenameMenuItem.Text = "Rename";
            this.RenameMenuItem.Click += new System.EventHandler(this.RenameMenuItem_Click);
            // 
            // PollTimer
            // 
            this.PollTimer.Tick += new System.EventHandler(this.PollTimer_Tick);
            // 
            // BackToolTip
            // 
            this.BackToolTip.ToolTipTitle = "Back to";
            // 
            // ForwardToolTip
            // 
            this.ForwardToolTip.ToolTipTitle = "Forward to";
            // 
            // GridView
            // 
            this.GridView.AutoScroll = true;
            this.GridView.ContextMenuStrip = this.ItemContextMenu;
            this.GridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridView.IsLoading = false;
            this.GridView.ItemPadding = new System.Drawing.Size(5, 5);
            this.GridView.Items = null;
            this.GridView.ItemSize = new System.Drawing.Size(0, 0);
            this.GridView.Location = new System.Drawing.Point(0, 0);
            this.GridView.Margin = new System.Windows.Forms.Padding(2);
            this.GridView.Name = "GridView";
            this.GridView.NameHeight = 30;
            this.GridView.NameSpace = 5;
            this.GridView.SelectionBounds = new System.Drawing.Rectangle(0, 0, 0, 0);
            this.GridView.Size = new System.Drawing.Size(626, 353);
            this.GridView.TabIndex = 1;
            this.GridView.DoubleClick += new System.EventHandler(this.GridView_DoubleClick);
            this.GridView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.GridView_MouseDown);
            this.GridView.MouseLeave += new System.EventHandler(this.GridView_MouseLeave);
            this.GridView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GridView_MouseMove);
            this.GridView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.GridView_MouseUp);
            // 
            // ImagesGridView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(626, 353);
            this.Controls.Add(this.GridView);
            this.Controls.Add(this.NameTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "ImagesGridView";
            this.Text = "Images";
            this.ItemContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox NameTextBox;
        private System.Windows.Forms.ContextMenuStrip ItemContextMenu;
        private System.Windows.Forms.ToolStripMenuItem RenameMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OpenMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CopyMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem DeleteMenuItem;
        private System.Windows.Forms.Timer PollTimer;
        private System.Windows.Forms.ToolTip ShowCodeToolTip;
        private System.Windows.Forms.ToolTip ForwardToolTip;
        private System.Windows.Forms.ToolTip BackToolTip;
        private GridView GridView;
    }
}
