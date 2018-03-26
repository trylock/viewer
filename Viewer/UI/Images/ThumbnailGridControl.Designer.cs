namespace Viewer.UI.Images
{
    partial class ThumbnailGridControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GridPanel = new Viewer.UI.Images.GridPanel();
            this.SuspendLayout();
            // 
            // GridPanel
            // 
            this.GridPanel.AutoScroll = true;
            this.GridPanel.BackColor = System.Drawing.Color.White;
            this.GridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridPanel.Location = new System.Drawing.Point(0, 0);
            this.GridPanel.Name = "GridPanel";
            this.GridPanel.Size = new System.Drawing.Size(735, 358);
            this.GridPanel.TabIndex = 0;
            this.GridPanel.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GridPanel_KeyDown);
            this.GridPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.GridPanel_MouseDown);
            this.GridPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GridPanel_MouseMove);
            this.GridPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.GridPanel_MouseUp);
            // 
            // ThumbnailGridControl
            // 
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(735, 358);
            this.Controls.Add(this.GridPanel);
            this.DoubleBuffered = true;
            this.Name = "ThumbnailGridControl";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ThumbnailGridControl_FormClosed);
            this.ResumeLayout(false);

        }

        #endregion

        private GridPanel GridPanel;
    }
}
