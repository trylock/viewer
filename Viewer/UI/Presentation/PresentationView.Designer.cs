namespace Viewer.UI.Presentation
{
    partial class PresentationView
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
            // PresentationView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(621, 348);
            this.DoubleBuffered = true;
            this.Name = "PresentationView";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PresentationView_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationView_KeyDown);
            this.Resize += new System.EventHandler(this.PresentationView_Resize);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
