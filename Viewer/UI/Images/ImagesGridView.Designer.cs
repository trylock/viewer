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
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.ItemContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.OpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.CopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.DeleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RenameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ThumbnailSizeTrackBar = new System.Windows.Forms.TrackBar();
            this.GridView = new Viewer.UI.Images.GridView();
            this.ItemsCountLabel = new System.Windows.Forms.Label();
            this.ViewTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.ControlPanel = new System.Windows.Forms.Panel();
            this.PollTimer = new System.Windows.Forms.Timer(this.components);
            this.ItemContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ThumbnailSizeTrackBar)).BeginInit();
            this.ViewTableLayout.SuspendLayout();
            this.ControlPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // NameTextBox
            // 
            this.NameTextBox.Location = new System.Drawing.Point(71, 40);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(115, 22);
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
            this.ItemContextMenu.Size = new System.Drawing.Size(164, 112);
            // 
            // OpenMenuItem
            // 
            this.OpenMenuItem.Name = "OpenMenuItem";
            this.OpenMenuItem.Size = new System.Drawing.Size(163, 24);
            this.OpenMenuItem.Text = "Open";
            this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(160, 6);
            // 
            // CopyMenuItem
            // 
            this.CopyMenuItem.Name = "CopyMenuItem";
            this.CopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.CopyMenuItem.Size = new System.Drawing.Size(163, 24);
            this.CopyMenuItem.Text = "Copy";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(160, 6);
            // 
            // DeleteMenuItem
            // 
            this.DeleteMenuItem.Name = "DeleteMenuItem";
            this.DeleteMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.DeleteMenuItem.Size = new System.Drawing.Size(163, 24);
            this.DeleteMenuItem.Text = "Delete";
            // 
            // RenameMenuItem
            // 
            this.RenameMenuItem.Name = "RenameMenuItem";
            this.RenameMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.RenameMenuItem.Size = new System.Drawing.Size(163, 24);
            this.RenameMenuItem.Text = "Rename";
            this.RenameMenuItem.Click += new System.EventHandler(this.RenameMenuItem_Click);
            // 
            // ThumbnailSizeTrackBar
            // 
            this.ThumbnailSizeTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ThumbnailSizeTrackBar.Location = new System.Drawing.Point(653, 3);
            this.ThumbnailSizeTrackBar.Name = "ThumbnailSizeTrackBar";
            this.ThumbnailSizeTrackBar.Size = new System.Drawing.Size(173, 56);
            this.ThumbnailSizeTrackBar.TabIndex = 1;
            this.ThumbnailSizeTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.ThumbnailSizeTrackBar.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ThumbnailSizeTrackBar_KeyUp);
            this.ThumbnailSizeTrackBar.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ThumbnailSizeTrackBar_MouseUp);
            // 
            // GridView
            // 
            this.GridView.AutoScroll = true;
            this.GridView.ContextMenuStrip = this.ItemContextMenu;
            this.GridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridView.ItemPadding = new System.Drawing.Size(8, 8);
            this.GridView.Items = null;
            this.GridView.ItemSize = new System.Drawing.Size(0, 0);
            this.GridView.Location = new System.Drawing.Point(3, 3);
            this.GridView.Name = "GridView";
            this.GridView.NameHeight = 25;
            this.GridView.NameSpace = 5;
            this.GridView.SelectionBounds = new System.Drawing.Rectangle(0, 0, 0, 0);
            this.GridView.Size = new System.Drawing.Size(829, 388);
            this.GridView.TabIndex = 2;
            this.GridView.Scroll += new System.Windows.Forms.ScrollEventHandler(this.GridView_Scroll);
            this.GridView.DoubleClick += new System.EventHandler(this.GridView_DoubleClick);
            this.GridView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.GridView_MouseDown);
            this.GridView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GridView_MouseMove);
            this.GridView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.GridView_MouseUp);
            // 
            // ItemsCountLabel
            // 
            this.ItemsCountLabel.AutoSize = true;
            this.ItemsCountLabel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.ItemsCountLabel.Location = new System.Drawing.Point(9, 8);
            this.ItemsCountLabel.Name = "ItemsCountLabel";
            this.ItemsCountLabel.Size = new System.Drawing.Size(53, 17);
            this.ItemsCountLabel.TabIndex = 2;
            this.ItemsCountLabel.Text = "0 Items";
            // 
            // ViewTableLayout
            // 
            this.ViewTableLayout.ColumnCount = 1;
            this.ViewTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ViewTableLayout.Controls.Add(this.ControlPanel, 0, 1);
            this.ViewTableLayout.Controls.Add(this.GridView, 0, 0);
            this.ViewTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ViewTableLayout.Location = new System.Drawing.Point(0, 0);
            this.ViewTableLayout.Name = "ViewTableLayout";
            this.ViewTableLayout.RowCount = 2;
            this.ViewTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ViewTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.ViewTableLayout.Size = new System.Drawing.Size(835, 434);
            this.ViewTableLayout.TabIndex = 3;
            // 
            // ControlPanel
            // 
            this.ControlPanel.Controls.Add(this.ThumbnailSizeTrackBar);
            this.ControlPanel.Controls.Add(this.ItemsCountLabel);
            this.ControlPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ControlPanel.Location = new System.Drawing.Point(3, 397);
            this.ControlPanel.Name = "ControlPanel";
            this.ControlPanel.Size = new System.Drawing.Size(829, 34);
            this.ControlPanel.TabIndex = 0;
            // 
            // PollTimer
            // 
            this.PollTimer.Tick += new System.EventHandler(this.PollTimer_Tick);
            // 
            // ImagesGridView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(835, 434);
            this.Controls.Add(this.ViewTableLayout);
            this.Controls.Add(this.NameTextBox);
            this.Name = "ImagesGridView";
            this.Text = "Images";
            this.ItemContextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ThumbnailSizeTrackBar)).EndInit();
            this.ViewTableLayout.ResumeLayout(false);
            this.ControlPanel.ResumeLayout(false);
            this.ControlPanel.PerformLayout();
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
        private System.Windows.Forms.TrackBar ThumbnailSizeTrackBar;
        private GridView GridView;
        private System.Windows.Forms.Label ItemsCountLabel;
        private System.Windows.Forms.TableLayoutPanel ViewTableLayout;
        private System.Windows.Forms.Panel ControlPanel;
        private System.Windows.Forms.Timer PollTimer;
    }
}
