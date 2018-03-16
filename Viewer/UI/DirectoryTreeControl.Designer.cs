namespace Viewer.UI
{
    partial class DirectoryTreeControl
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
            this.TreeView = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // TreeView
            // 
            this.TreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeView.FullRowSelect = true;
            this.TreeView.HotTracking = true;
            this.TreeView.ItemHeight = 22;
            this.TreeView.Location = new System.Drawing.Point(0, 0);
            this.TreeView.Name = "TreeView";
            this.TreeView.ShowLines = false;
            this.TreeView.ShowPlusMinus = false;
            this.TreeView.Size = new System.Drawing.Size(236, 377);
            this.TreeView.TabIndex = 0;
            this.TreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.TreeView_NodeMouseClick);
            this.TreeView.MouseLeave += new System.EventHandler(this.TreeView_MouseLeave);
            this.TreeView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TreeView_MouseMove);
            // 
            // DirectoryTreeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.TreeView);
            this.Name = "DirectoryTreeControl";
            this.Size = new System.Drawing.Size(236, 377);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView TreeView;
    }
}
