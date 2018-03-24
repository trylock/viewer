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
            this.SplitContainer = new System.Windows.Forms.SplitContainer();
            this.directoryTreeControl1 = new Viewer.UI.DirectoryTreeControl();
            this.thumbnailGridControl1 = new Viewer.UI.ThumbnailGridControl();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer)).BeginInit();
            this.SplitContainer.Panel1.SuspendLayout();
            this.SplitContainer.Panel2.SuspendLayout();
            this.SplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // SplitContainer
            // 
            this.SplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.SplitContainer.Location = new System.Drawing.Point(0, 0);
            this.SplitContainer.Name = "SplitContainer";
            // 
            // SplitContainer.Panel1
            // 
            this.SplitContainer.Panel1.Controls.Add(this.directoryTreeControl1);
            // 
            // SplitContainer.Panel2
            // 
            this.SplitContainer.Panel2.Controls.Add(this.thumbnailGridControl1);
            this.SplitContainer.Size = new System.Drawing.Size(967, 593);
            this.SplitContainer.SplitterDistance = 289;
            this.SplitContainer.TabIndex = 0;
            // 
            // directoryTreeControl1
            // 
            this.directoryTreeControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.directoryTreeControl1.Location = new System.Drawing.Point(0, 0);
            this.directoryTreeControl1.Name = "directoryTreeControl1";
            this.directoryTreeControl1.Size = new System.Drawing.Size(289, 593);
            this.directoryTreeControl1.TabIndex = 0;
            // 
            // thumbnailGridControl1
            // 
            this.thumbnailGridControl1.AutoScroll = true;
            this.thumbnailGridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.thumbnailGridControl1.ItemPadding = new System.Drawing.Size(8, 8);
            this.thumbnailGridControl1.ItemSize = new System.Drawing.Size(0, 0);
            this.thumbnailGridControl1.Location = new System.Drawing.Point(0, 0);
            this.thumbnailGridControl1.Name = "thumbnailGridControl1";
            this.thumbnailGridControl1.Size = new System.Drawing.Size(674, 593);
            this.thumbnailGridControl1.TabIndex = 0;
            // 
            // ViewerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(967, 593);
            this.Controls.Add(this.SplitContainer);
            this.Name = "ViewerForm";
            this.Text = "Viewer";
            this.SplitContainer.Panel1.ResumeLayout(false);
            this.SplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer)).EndInit();
            this.SplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer SplitContainer;
        private UI.DirectoryTreeControl directoryTreeControl1;
        private UI.ThumbnailGridControl thumbnailGridControl1;
    }
}

