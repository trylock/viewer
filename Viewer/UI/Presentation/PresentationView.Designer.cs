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
            this.PresentationControl = new Viewer.UI.Presentation.PresentationControl();
            this.SuspendLayout();
            // 
            // PresentationControl
            // 
            this.PresentationControl.BackColor = System.Drawing.Color.Black;
            this.PresentationControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PresentationControl.Location = new System.Drawing.Point(0, 0);
            this.PresentationControl.Name = "PresentationControl";
            this.PresentationControl.Picture = null;
            this.PresentationControl.Size = new System.Drawing.Size(621, 348);
            this.PresentationControl.TabIndex = 0;
            // 
            // PresentationView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(621, 348);
            this.Controls.Add(this.PresentationControl);
            this.DoubleBuffered = true;
            this.Name = "PresentationView";
            this.ResumeLayout(false);

        }

        #endregion

        private PresentationControl PresentationControl;
    }
}
