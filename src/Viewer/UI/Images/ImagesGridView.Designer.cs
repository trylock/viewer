﻿namespace Viewer.UI.Images
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
            this.ThumbnailSizeTrackBar = new System.Windows.Forms.TrackBar();
            this.ItemsCountLabel = new System.Windows.Forms.Label();
            this.ViewTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.ControlPanel = new System.Windows.Forms.Panel();
            this.ShowQueryButton = new System.Windows.Forms.Button();
            this.PollTimer = new System.Windows.Forms.Timer(this.components);
            this.ShowCodeToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ForwardButton = new Viewer.UI.Forms.IconButton();
            this.BackButton = new Viewer.UI.Forms.IconButton();
            this.GridView = new Viewer.UI.Images.GridView();
            this.ItemContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ThumbnailSizeTrackBar)).BeginInit();
            this.ViewTableLayout.SuspendLayout();
            this.ControlPanel.SuspendLayout();
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
            // ThumbnailSizeTrackBar
            // 
            this.ThumbnailSizeTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ThumbnailSizeTrackBar.Location = new System.Drawing.Point(490, 2);
            this.ThumbnailSizeTrackBar.Margin = new System.Windows.Forms.Padding(2);
            this.ThumbnailSizeTrackBar.Name = "ThumbnailSizeTrackBar";
            this.ThumbnailSizeTrackBar.Size = new System.Drawing.Size(130, 45);
            this.ThumbnailSizeTrackBar.TabIndex = 1;
            this.ThumbnailSizeTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.ThumbnailSizeTrackBar.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ThumbnailSizeTrackBar_KeyUp);
            this.ThumbnailSizeTrackBar.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ThumbnailSizeTrackBar_MouseUp);
            // 
            // ItemsCountLabel
            // 
            this.ItemsCountLabel.AutoSize = true;
            this.ItemsCountLabel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.ItemsCountLabel.Location = new System.Drawing.Point(7, 6);
            this.ItemsCountLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.ItemsCountLabel.Name = "ItemsCountLabel";
            this.ItemsCountLabel.Size = new System.Drawing.Size(41, 13);
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
            this.ViewTableLayout.Margin = new System.Windows.Forms.Padding(2);
            this.ViewTableLayout.Name = "ViewTableLayout";
            this.ViewTableLayout.RowCount = 2;
            this.ViewTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ViewTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.ViewTableLayout.Size = new System.Drawing.Size(626, 353);
            this.ViewTableLayout.TabIndex = 3;
            // 
            // ControlPanel
            // 
            this.ControlPanel.Controls.Add(this.ForwardButton);
            this.ControlPanel.Controls.Add(this.BackButton);
            this.ControlPanel.Controls.Add(this.ShowQueryButton);
            this.ControlPanel.Controls.Add(this.ThumbnailSizeTrackBar);
            this.ControlPanel.Controls.Add(this.ItemsCountLabel);
            this.ControlPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ControlPanel.Location = new System.Drawing.Point(2, 323);
            this.ControlPanel.Margin = new System.Windows.Forms.Padding(2);
            this.ControlPanel.Name = "ControlPanel";
            this.ControlPanel.Size = new System.Drawing.Size(622, 28);
            this.ControlPanel.TabIndex = 0;
            // 
            // ShowQueryButton
            // 
            this.ShowQueryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ShowQueryButton.BackgroundImage = global::Viewer.Properties.Resources.ShowCodeIcon;
            this.ShowQueryButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ShowQueryButton.FlatAppearance.BorderSize = 0;
            this.ShowQueryButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ShowQueryButton.Location = new System.Drawing.Point(458, 0);
            this.ShowQueryButton.Name = "ShowQueryButton";
            this.ShowQueryButton.Size = new System.Drawing.Size(27, 27);
            this.ShowQueryButton.TabIndex = 3;
            this.ShowCodeToolTip.SetToolTip(this.ShowQueryButton, "Show Code");
            this.ShowQueryButton.UseVisualStyleBackColor = true;
            this.ShowQueryButton.Click += new System.EventHandler(this.ShowQueryButton_Click);
            // 
            // PollTimer
            // 
            this.PollTimer.Tick += new System.EventHandler(this.PollTimer_Tick);
            // 
            // ForwardButton
            // 
            this.ForwardButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ForwardButton.Icon = global::Viewer.Properties.Resources.Forward;
            this.ForwardButton.IconColorTint = System.Drawing.Color.DimGray;
            this.ForwardButton.IconSize = new System.Drawing.Size(0, 0);
            this.ForwardButton.Location = new System.Drawing.Point(429, 3);
            this.ForwardButton.Name = "ForwardButton";
            this.ForwardButton.Size = new System.Drawing.Size(23, 23);
            this.ForwardButton.TabIndex = 5;
            this.ForwardButton.Text = "iconButton1";
            this.ForwardButton.Click += new System.EventHandler(this.ForwardButton_Click);
            // 
            // BackButton
            // 
            this.BackButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BackButton.Icon = global::Viewer.Properties.Resources.Back;
            this.BackButton.IconColorTint = System.Drawing.Color.DimGray;
            this.BackButton.IconSize = new System.Drawing.Size(0, 0);
            this.BackButton.Location = new System.Drawing.Point(400, 3);
            this.BackButton.Name = "BackButton";
            this.BackButton.Size = new System.Drawing.Size(23, 23);
            this.BackButton.TabIndex = 4;
            this.BackButton.Text = "iconButton1";
            this.BackButton.Click += new System.EventHandler(this.BackButton_Click);
            // 
            // GridView
            // 
            this.GridView.AutoScroll = true;
            this.GridView.ContextMenuStrip = this.ItemContextMenu;
            this.GridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridView.IsLoading = false;
            this.GridView.ItemPadding = new System.Drawing.Size(8, 8);
            this.GridView.Items = null;
            this.GridView.ItemSize = new System.Drawing.Size(0, 0);
            this.GridView.Location = new System.Drawing.Point(2, 2);
            this.GridView.Margin = new System.Windows.Forms.Padding(2);
            this.GridView.Name = "GridView";
            this.GridView.NameHeight = 25;
            this.GridView.NameSpace = 5;
            this.GridView.SelectionBounds = new System.Drawing.Rectangle(0, 0, 0, 0);
            this.GridView.Size = new System.Drawing.Size(622, 317);
            this.GridView.TabIndex = 2;
            this.GridView.Scroll += new System.Windows.Forms.ScrollEventHandler(this.GridView_Scroll);
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
            this.Controls.Add(this.ViewTableLayout);
            this.Controls.Add(this.NameTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
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
        private System.Windows.Forms.Label ItemsCountLabel;
        private System.Windows.Forms.TableLayoutPanel ViewTableLayout;
        private System.Windows.Forms.Panel ControlPanel;
        private System.Windows.Forms.Timer PollTimer;
        private GridView GridView;
        private System.Windows.Forms.Button ShowQueryButton;
        private System.Windows.Forms.ToolTip ShowCodeToolTip;
        private Forms.IconButton BackButton;
        private Forms.IconButton ForwardButton;
    }
}