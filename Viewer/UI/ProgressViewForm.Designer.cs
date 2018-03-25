namespace Viewer.UI
{
    partial class ProgressViewForm
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
            this.Progress = new System.Windows.Forms.ProgressBar();
            this.CurrentTaskNameLabel = new System.Windows.Forms.Label();
            this.CancelProgressButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Progress
            // 
            this.Progress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Progress.Location = new System.Drawing.Point(12, 38);
            this.Progress.Name = "Progress";
            this.Progress.Size = new System.Drawing.Size(458, 34);
            this.Progress.TabIndex = 0;
            // 
            // CurrentTaskNameLabel
            // 
            this.CurrentTaskNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CurrentTaskNameLabel.AutoSize = true;
            this.CurrentTaskNameLabel.Location = new System.Drawing.Point(12, 18);
            this.CurrentTaskNameLabel.Name = "CurrentTaskNameLabel";
            this.CurrentTaskNameLabel.Size = new System.Drawing.Size(101, 17);
            this.CurrentTaskNameLabel.TabIndex = 1;
            this.CurrentTaskNameLabel.Text = "Current task ...";
            // 
            // CancelProgressButton
            // 
            this.CancelProgressButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelProgressButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelProgressButton.Location = new System.Drawing.Point(395, 78);
            this.CancelProgressButton.Name = "CancelProgressButton";
            this.CancelProgressButton.Size = new System.Drawing.Size(75, 23);
            this.CancelProgressButton.TabIndex = 2;
            this.CancelProgressButton.Text = "Cancel";
            this.CancelProgressButton.UseVisualStyleBackColor = true;
            this.CancelProgressButton.Click += new System.EventHandler(this.CancelProgressButton_Click);
            // 
            // ProgressViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelProgressButton;
            this.ClientSize = new System.Drawing.Size(482, 113);
            this.Controls.Add(this.CancelProgressButton);
            this.Controls.Add(this.CurrentTaskNameLabel);
            this.Controls.Add(this.Progress);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 120);
            this.Name = "ProgressViewForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ProgressView";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ProgressViewForm_FormClosing);
            this.Shown += new System.EventHandler(this.ProgressViewForm_Shown);
            this.VisibleChanged += new System.EventHandler(this.ProgressViewForm_VisibleChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar Progress;
        private System.Windows.Forms.Label CurrentTaskNameLabel;
        private System.Windows.Forms.Button CancelProgressButton;
    }
}