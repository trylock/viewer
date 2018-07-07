namespace Viewer.UI.Tasks
{
    partial class TaskLoaderView
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
            this.components = new System.ComponentModel.Container();
            this.TaskProgressBar = new System.Windows.Forms.ProgressBar();
            this.ProgressLabel = new System.Windows.Forms.Label();
            this.CancelTaskButton = new System.Windows.Forms.Button();
            this.TaskNameLabel = new System.Windows.Forms.Label();
            this.PollTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // TaskProgressBar
            // 
            this.TaskProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TaskProgressBar.Location = new System.Drawing.Point(14, 29);
            this.TaskProgressBar.Name = "TaskProgressBar";
            this.TaskProgressBar.Size = new System.Drawing.Size(497, 26);
            this.TaskProgressBar.TabIndex = 0;
            // 
            // ProgressLabel
            // 
            this.ProgressLabel.AutoSize = true;
            this.ProgressLabel.Location = new System.Drawing.Point(12, 9);
            this.ProgressLabel.Name = "ProgressLabel";
            this.ProgressLabel.Size = new System.Drawing.Size(89, 17);
            this.ProgressLabel.TabIndex = 1;
            this.ProgressLabel.Text = "0% complete";
            // 
            // CancelTaskButton
            // 
            this.CancelTaskButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelTaskButton.Location = new System.Drawing.Point(517, 29);
            this.CancelTaskButton.Name = "CancelTaskButton";
            this.CancelTaskButton.Size = new System.Drawing.Size(78, 26);
            this.CancelTaskButton.TabIndex = 2;
            this.CancelTaskButton.Text = "Cancel";
            this.CancelTaskButton.UseVisualStyleBackColor = true;
            this.CancelTaskButton.Click += new System.EventHandler(this.CancelTaskButton_Click);
            // 
            // TaskNameLabel
            // 
            this.TaskNameLabel.AutoSize = true;
            this.TaskNameLabel.Location = new System.Drawing.Point(11, 58);
            this.TaskNameLabel.Name = "TaskNameLabel";
            this.TaskNameLabel.Size = new System.Drawing.Size(75, 17);
            this.TaskNameLabel.TabIndex = 3;
            this.TaskNameLabel.Text = "Loading ...";
            // 
            // PollTimer
            // 
            this.PollTimer.Enabled = true;
            this.PollTimer.Tick += new System.EventHandler(this.PollTimer_Tick);
            // 
            // TaskLoaderView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(607, 93);
            this.Controls.Add(this.TaskNameLabel);
            this.Controls.Add(this.CancelTaskButton);
            this.Controls.Add(this.ProgressLabel);
            this.Controls.Add(this.TaskProgressBar);
            this.Name = "TaskLoaderView";
            this.Text = "TaskLoaderView";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TaskLoaderView_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar TaskProgressBar;
        private System.Windows.Forms.Label ProgressLabel;
        private System.Windows.Forms.Button CancelTaskButton;
        private System.Windows.Forms.Label TaskNameLabel;
        private System.Windows.Forms.Timer PollTimer;
    }
}