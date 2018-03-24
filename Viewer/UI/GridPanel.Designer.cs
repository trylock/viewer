namespace Viewer.UI
{
    partial class GridPanel
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
            // GridPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.DoubleBuffered = true;
            this.Name = "GridPanel";
            this.Size = new System.Drawing.Size(656, 349);
            this.Click += new System.EventHandler(this.GridPanel_Click);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.GridPanel_Paint);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.GridPanel_MouseDoubleClick);
            this.MouseLeave += new System.EventHandler(this.GridPanel_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GridPanel_MouseMove);
            this.Resize += new System.EventHandler(this.GridPanel_Resize);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
