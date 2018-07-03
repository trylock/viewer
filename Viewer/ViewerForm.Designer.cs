namespace Viewer
{
    partial class ViewerForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ViewerMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ViewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FileDialog = new System.Windows.Forms.OpenFileDialog();
            this.ViewerMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // ViewerMenu
            // 
            this.ViewerMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.ViewerMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.ViewMenuItem});
            this.ViewerMenu.Location = new System.Drawing.Point(0, 0);
            this.ViewerMenu.Name = "ViewerMenu";
            this.ViewerMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.ViewerMenu.Size = new System.Drawing.Size(1000, 28);
            this.ViewerMenu.TabIndex = 1;
            this.ViewerMenu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // ViewMenuItem
            // 
            this.ViewMenuItem.Name = "ViewMenuItem";
            this.ViewMenuItem.Size = new System.Drawing.Size(53, 24);
            this.ViewMenuItem.Text = "View";
            // 
            // FileDialog
            // 
            this.FileDialog.FileName = "openFileDialog1";
            // 
            // ViewerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 498);
            this.Controls.Add(this.ViewerMenu);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.ViewerMenu;
            this.Name = "ViewerForm";
            this.Text = "Viewer";
            this.Resize += new System.EventHandler(this.ViewerForm_Resize);
            this.ViewerMenu.ResumeLayout(false);
            this.ViewerMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip ViewerMenu;
        private System.Windows.Forms.ToolStripMenuItem ViewMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog FileDialog;
    }
}

