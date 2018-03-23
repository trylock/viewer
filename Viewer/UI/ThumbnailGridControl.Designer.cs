namespace Viewer.UI
{
    partial class ThumbnailGridControl
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
            this.SuspendLayout();
            // 
            // ThumbnailGridControl
            // 
            this.AutoScroll = true;
            this.DoubleBuffered = true;
            this.Name = "ThumbnailGridControl";
            this.Size = new System.Drawing.Size(774, 426);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ThumbnailGridControl_Paint);
            this.Resize += new System.EventHandler(this.ThumbnailGridControl_Resize);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
