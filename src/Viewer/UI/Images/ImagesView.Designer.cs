namespace Viewer.UI.Images
{
    partial class ImagesView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImagesView));
            Viewer.UI.Images.Layout.VerticalGridLayout verticalGridLayout1 = new Viewer.UI.Images.Layout.VerticalGridLayout();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.ItemContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.OpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.RefreshMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ShowQueryMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.UpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PreviousMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.PasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.DeleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RenameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PollTimer = new System.Windows.Forms.Timer(this.components);
            this.ShowCodeToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.StatusLabel = new System.Windows.Forms.Label();
            this.PickDirectoryContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this._thumbnailGridView = new Viewer.UI.Images.ThumbnailGridView();
            this.HistoryView = new Viewer.UI.Images.HistoryView();
            this.BackToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ForwardToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.MoveTimer = new System.Windows.Forms.Timer(this.components);
            this.ItemContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // NameTextBox
            // 
            resources.ApplyResources(this.NameTextBox, "NameTextBox");
            this.NameTextBox.Name = "NameTextBox";
            this.ShowCodeToolTip.SetToolTip(this.NameTextBox, resources.GetString("NameTextBox.ToolTip"));
            this.BackToolTip.SetToolTip(this.NameTextBox, resources.GetString("NameTextBox.ToolTip1"));
            this.ForwardToolTip.SetToolTip(this.NameTextBox, resources.GetString("NameTextBox.ToolTip2"));
            this.NameTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NameTextBox_KeyDown);
            this.NameTextBox.Leave += new System.EventHandler(this.NameTextBox_Leave);
            // 
            // ItemContextMenu
            // 
            resources.ApplyResources(this.ItemContextMenu, "ItemContextMenu");
            this.ItemContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.ItemContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenMenuItem,
            this.toolStripSeparator5,
            this.toolStripSeparator3,
            this.RefreshMenuItem,
            this.ShowQueryMenuItem,
            this.toolStripSeparator4,
            this.UpMenuItem,
            this.PreviousMenuItem,
            this.NextMenuItem,
            this.toolStripSeparator1,
            this.PasteMenuItem,
            this.CopyMenuItem,
            this.CutMenuItem,
            this.toolStripSeparator2,
            this.DeleteMenuItem,
            this.RenameMenuItem});
            this.ItemContextMenu.Name = "ItemContextMenu";
            this.ShowCodeToolTip.SetToolTip(this.ItemContextMenu, resources.GetString("ItemContextMenu.ToolTip"));
            this.BackToolTip.SetToolTip(this.ItemContextMenu, resources.GetString("ItemContextMenu.ToolTip1"));
            this.ForwardToolTip.SetToolTip(this.ItemContextMenu, resources.GetString("ItemContextMenu.ToolTip2"));
            // 
            // OpenMenuItem
            // 
            resources.ApplyResources(this.OpenMenuItem, "OpenMenuItem");
            this.OpenMenuItem.Name = "OpenMenuItem";
            this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            // 
            // RefreshMenuItem
            // 
            resources.ApplyResources(this.RefreshMenuItem, "RefreshMenuItem");
            this.RefreshMenuItem.Name = "RefreshMenuItem";
            // 
            // ShowQueryMenuItem
            // 
            resources.ApplyResources(this.ShowQueryMenuItem, "ShowQueryMenuItem");
            this.ShowQueryMenuItem.Name = "ShowQueryMenuItem";
            this.ShowQueryMenuItem.Click += new System.EventHandler(this.ShowQueryMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            // 
            // UpMenuItem
            // 
            resources.ApplyResources(this.UpMenuItem, "UpMenuItem");
            this.UpMenuItem.Name = "UpMenuItem";
            this.UpMenuItem.Click += new System.EventHandler(this.UpMenuItem_Click);
            // 
            // PreviousMenuItem
            // 
            resources.ApplyResources(this.PreviousMenuItem, "PreviousMenuItem");
            this.PreviousMenuItem.Name = "PreviousMenuItem";
            this.PreviousMenuItem.Click += new System.EventHandler(this.PreviousMenuItem_Click);
            // 
            // NextMenuItem
            // 
            resources.ApplyResources(this.NextMenuItem, "NextMenuItem");
            this.NextMenuItem.Name = "NextMenuItem";
            this.NextMenuItem.Click += new System.EventHandler(this.NextMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            // 
            // PasteMenuItem
            // 
            resources.ApplyResources(this.PasteMenuItem, "PasteMenuItem");
            this.PasteMenuItem.Name = "PasteMenuItem";
            this.PasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
            // 
            // CopyMenuItem
            // 
            resources.ApplyResources(this.CopyMenuItem, "CopyMenuItem");
            this.CopyMenuItem.Name = "CopyMenuItem";
            // 
            // CutMenuItem
            // 
            resources.ApplyResources(this.CutMenuItem, "CutMenuItem");
            this.CutMenuItem.Name = "CutMenuItem";
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            // 
            // DeleteMenuItem
            // 
            resources.ApplyResources(this.DeleteMenuItem, "DeleteMenuItem");
            this.DeleteMenuItem.Name = "DeleteMenuItem";
            // 
            // RenameMenuItem
            // 
            resources.ApplyResources(this.RenameMenuItem, "RenameMenuItem");
            this.RenameMenuItem.Name = "RenameMenuItem";
            this.RenameMenuItem.Click += new System.EventHandler(this.RenameMenuItem_Click);
            // 
            // PollTimer
            // 
            this.PollTimer.Tick += new System.EventHandler(this.PollTimer_Tick);
            // 
            // StatusLabel
            // 
            resources.ApplyResources(this.StatusLabel, "StatusLabel");
            this.StatusLabel.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.StatusLabel.Name = "StatusLabel";
            this.BackToolTip.SetToolTip(this.StatusLabel, resources.GetString("StatusLabel.ToolTip"));
            this.ForwardToolTip.SetToolTip(this.StatusLabel, resources.GetString("StatusLabel.ToolTip1"));
            this.ShowCodeToolTip.SetToolTip(this.StatusLabel, resources.GetString("StatusLabel.ToolTip2"));
            // 
            // PickDirectoryContextMenu
            // 
            resources.ApplyResources(this.PickDirectoryContextMenu, "PickDirectoryContextMenu");
            this.PickDirectoryContextMenu.Name = "PickDirectoryContextMenu";
            this.ShowCodeToolTip.SetToolTip(this.PickDirectoryContextMenu, resources.GetString("PickDirectoryContextMenu.ToolTip"));
            this.BackToolTip.SetToolTip(this.PickDirectoryContextMenu, resources.GetString("PickDirectoryContextMenu.ToolTip1"));
            this.ForwardToolTip.SetToolTip(this.PickDirectoryContextMenu, resources.GetString("PickDirectoryContextMenu.ToolTip2"));
            // 
            // _thumbnailGridView
            // 
            resources.ApplyResources(this._thumbnailGridView, "_thumbnailGridView");
            this._thumbnailGridView.AllowDrop = true;
            this._thumbnailGridView.ContextMenuStrip = this.ItemContextMenu;
            verticalGridLayout1.ClientBounds = new System.Drawing.Rectangle(0, 0, 626, 324);
            verticalGridLayout1.GroupLabelMargin = new System.Windows.Forms.Padding(0);
            verticalGridLayout1.GroupLabelSize = new System.Drawing.Size(0, 39);
            verticalGridLayout1.Groups = null;
            verticalGridLayout1.ItemMargin = new System.Windows.Forms.Padding(5);
            verticalGridLayout1.ItemPadding = new System.Windows.Forms.Padding(5, 5, 5, 37);
            verticalGridLayout1.ThumbnailAreaSize = new System.Drawing.Size(0, 0);
            this._thumbnailGridView.ControlLayout = verticalGridLayout1;
            this._thumbnailGridView.Items = null;
            this._thumbnailGridView.ItemSize = new System.Drawing.Size(0, 0);
            this._thumbnailGridView.Name = "_thumbnailGridView";
            this._thumbnailGridView.SelectionBounds = new System.Drawing.Rectangle(0, 0, 0, 0);
            this.BackToolTip.SetToolTip(this._thumbnailGridView, resources.GetString("_thumbnailGridView.ToolTip"));
            this.ForwardToolTip.SetToolTip(this._thumbnailGridView, resources.GetString("_thumbnailGridView.ToolTip1"));
            this.ShowCodeToolTip.SetToolTip(this._thumbnailGridView, resources.GetString("_thumbnailGridView.ToolTip2"));
            // 
            // HistoryView
            // 
            resources.ApplyResources(this.HistoryView, "HistoryView");
            this.HistoryView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(242)))));
            this.HistoryView.CanGoBackInHistory = false;
            this.HistoryView.CanGoForwardInHistory = false;
            this.HistoryView.Name = "HistoryView";
            this.ShowCodeToolTip.SetToolTip(this.HistoryView, resources.GetString("HistoryView.ToolTip"));
            this.BackToolTip.SetToolTip(this.HistoryView, resources.GetString("HistoryView.ToolTip1"));
            this.ForwardToolTip.SetToolTip(this.HistoryView, resources.GetString("HistoryView.ToolTip2"));
            // 
            // BackToolTip
            // 
            this.BackToolTip.ToolTipTitle = "Back to";
            // 
            // ForwardToolTip
            // 
            this.ForwardToolTip.ToolTipTitle = "Forward to";
            // 
            // MoveTimer
            // 
            this.MoveTimer.Enabled = true;
            this.MoveTimer.Interval = 33;
            this.MoveTimer.Tick += new System.EventHandler(this.MoveTimer_Tick);
            // 
            // ImagesView
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ContextMenuStrip = this.ItemContextMenu;
            this.Controls.Add(this.StatusLabel);
            this.Controls.Add(this._thumbnailGridView);
            this.Controls.Add(this.HistoryView);
            this.Controls.Add(this.NameTextBox);
            this.Name = "ImagesView";
            this.ForwardToolTip.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.ShowCodeToolTip.SetToolTip(this, resources.GetString("$this.ToolTip1"));
            this.BackToolTip.SetToolTip(this, resources.GetString("$this.ToolTip2"));
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
        private System.Windows.Forms.ToolStripMenuItem CutMenuItem;
        private System.Windows.Forms.Label StatusLabel;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem RefreshMenuItem;
        private System.Windows.Forms.ToolStripMenuItem PreviousMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NextMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem ShowQueryMenuItem;
        private System.Windows.Forms.ContextMenuStrip PickDirectoryContextMenu;
        private System.Windows.Forms.ToolStripMenuItem PasteMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UpMenuItem;
        private ThumbnailGridView _thumbnailGridView;
        private HistoryView HistoryView;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.Timer MoveTimer;
    }
}
