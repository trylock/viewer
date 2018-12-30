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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.Icon8CopyrightNoticeLabel = new System.Windows.Forms.Label();
            this.Icon8Link = new System.Windows.Forms.LinkLabel();
            this.ApplicationNameLabel = new System.Windows.Forms.Label();
            this.VersionLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Icon8CopyrightNoticeLabel
            // 
            resources.ApplyResources(this.Icon8CopyrightNoticeLabel, "Icon8CopyrightNoticeLabel");
            this.Icon8CopyrightNoticeLabel.Name = "Icon8CopyrightNoticeLabel";
            // 
            // Icon8Link
            // 
            resources.ApplyResources(this.Icon8Link, "Icon8Link");
            this.Icon8Link.Name = "Icon8Link";
            this.Icon8Link.TabStop = true;
            this.Icon8Link.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.Icon8Link_LinkClicked);
            // 
            // ApplicationNameLabel
            // 
            resources.ApplyResources(this.ApplicationNameLabel, "ApplicationNameLabel");
            this.ApplicationNameLabel.Name = "ApplicationNameLabel";
            // 
            // VersionLabel
            // 
            resources.ApplyResources(this.VersionLabel, "VersionLabel");
            this.VersionLabel.Name = "VersionLabel";
            // 
            // AboutForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.VersionLabel);
            this.Controls.Add(this.ApplicationNameLabel);
            this.Controls.Add(this.Icon8Link);
            this.Controls.Add(this.Icon8CopyrightNoticeLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.TopMost = true;
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