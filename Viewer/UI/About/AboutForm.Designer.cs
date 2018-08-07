namespace Viewer.UI.About
{
    partial class AboutForm
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
            this.Icon8CopyrightNoticeLabel = new System.Windows.Forms.Label();
            this.Icon8Link = new System.Windows.Forms.LinkLabel();
            this.ApplicationNameLabel = new System.Windows.Forms.Label();
            this.VersionLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Icon8CopyrightNoticeLabel
            // 
            this.Icon8CopyrightNoticeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Icon8CopyrightNoticeLabel.AutoSize = true;
            this.Icon8CopyrightNoticeLabel.Location = new System.Drawing.Point(19, 78);
            this.Icon8CopyrightNoticeLabel.Name = "Icon8CopyrightNoticeLabel";
            this.Icon8CopyrightNoticeLabel.Size = new System.Drawing.Size(120, 13);
            this.Icon8CopyrightNoticeLabel.TabIndex = 0;
            this.Icon8CopyrightNoticeLabel.Text = "Icons downloaded from:";
            // 
            // Icon8Link
            // 
            this.Icon8Link.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Icon8Link.AutoSize = true;
            this.Icon8Link.Location = new System.Drawing.Point(136, 78);
            this.Icon8Link.Name = "Icon8Link";
            this.Icon8Link.Size = new System.Drawing.Size(61, 13);
            this.Icon8Link.TabIndex = 1;
            this.Icon8Link.TabStop = true;
            this.Icon8Link.Text = "icons8.com";
            this.Icon8Link.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.Icon8Link_LinkClicked);
            // 
            // ApplicationNameLabel
            // 
            this.ApplicationNameLabel.AutoSize = true;
            this.ApplicationNameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.ApplicationNameLabel.Location = new System.Drawing.Point(15, 13);
            this.ApplicationNameLabel.Name = "ApplicationNameLabel";
            this.ApplicationNameLabel.Size = new System.Drawing.Size(170, 20);
            this.ApplicationNameLabel.TabIndex = 2;
            this.ApplicationNameLabel.Text = "Multifilter Photo Viewer";
            // 
            // VersionLabel
            // 
            this.VersionLabel.AutoSize = true;
            this.VersionLabel.Location = new System.Drawing.Point(19, 37);
            this.VersionLabel.Name = "VersionLabel";
            this.VersionLabel.Size = new System.Drawing.Size(45, 13);
            this.VersionLabel.TabIndex = 3;
            this.VersionLabel.Text = "Version:";
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(211, 109);
            this.Controls.Add(this.VersionLabel);
            this.Controls.Add(this.ApplicationNameLabel);
            this.Controls.Add(this.Icon8Link);
            this.Controls.Add(this.Icon8CopyrightNoticeLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.Text = "About";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Icon8CopyrightNoticeLabel;
        private System.Windows.Forms.LinkLabel Icon8Link;
        private System.Windows.Forms.Label ApplicationNameLabel;
        private System.Windows.Forms.Label VersionLabel;
    }
}