namespace Viewer.UI
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
            this.GridPanel = new Viewer.UI.GridPanel();
            this.SuspendLayout();
            // 
            // GridPanel
            // 
            this.GridPanel.AutoScroll = true;
            this.GridPanel.AutoScrollMinSize = new System.Drawing.Size(0, 200);
            this.GridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridPanel.Location = new System.Drawing.Point(0, 0);
            this.GridPanel.Name = "GridPanel";
            this.GridPanel.Size = new System.Drawing.Size(753, 405);
            this.GridPanel.TabIndex = 0;
            // 
            // ThumbnailGridControl
            // 
            this.AutoScroll = true;
            this.Controls.Add(this.GridPanel);
            this.DoubleBuffered = true;
            this.Name = "ThumbnailGridControl";
            this.Size = new System.Drawing.Size(753, 405);
            this.ResumeLayout(false);

        }

        #endregion

        private GridPanel GridPanel;
    }
}
