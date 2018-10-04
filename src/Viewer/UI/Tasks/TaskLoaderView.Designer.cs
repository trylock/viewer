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
            this.CurrentItemLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // TaskProgressBar
            // 
            this.TaskProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TaskProgressBar.Location = new System.Drawing.Point(10, 24);
            this.TaskProgressBar.Margin = new System.Windows.Forms.Padding(2);
            this.TaskProgressBar.Name = "TaskProgressBar";
            this.TaskProgressBar.Size = new System.Drawing.Size(373, 21);
            this.TaskProgressBar.TabIndex = 0;
            // 
            // ProgressLabel
            // 
            this.ProgressLabel.AutoSize = true;
            this.ProgressLabel.Location = new System.Drawing.Point(9, 7);
            this.ProgressLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.ProgressLabel.Name = "ProgressLabel";
            this.ProgressLabel.Size = new System.Drawing.Size(67, 13);
            this.ProgressLabel.TabIndex = 1;
            this.ProgressLabel.Text = "0% complete";
            // 
            // CancelTaskButton
            // 
            this.CancelTaskButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelTaskButton.Location = new System.Drawing.Point(388, 24);
            this.CancelTaskButton.Margin = new System.Windows.Forms.Padding(2);
            this.CancelTaskButton.Name = "CancelTaskButton";
            this.CancelTaskButton.Size = new System.Drawing.Size(58, 21);
            this.CancelTaskButton.TabIndex = 2;
            this.CancelTaskButton.TabStop = false;
            this.CancelTaskButton.Text = "Cancel";
            this.CancelTaskButton.UseVisualStyleBackColor = true;
            this.CancelTaskButton.Click += new System.EventHandler(this.CancelTaskButton_Click);
            // 
            // TaskNameLabel
            // 
            this.TaskNameLabel.AutoSize = true;
            this.TaskNameLabel.Location = new System.Drawing.Point(78, 47);
            this.TaskNameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.TaskNameLabel.Name = "TaskNameLabel";
            this.TaskNameLabel.Size = new System.Drawing.Size(57, 13);
            this.TaskNameLabel.TabIndex = 3;
            this.TaskNameLabel.Text = "Loading ...";
            // 
            // PollTimer
            // 
            this.PollTimer.Enabled = true;
            this.PollTimer.Tick += new System.EventHandler(this.PollTimer_Tick);
            // 
            // CurrentItemLabel
            // 
            this.CurrentItemLabel.AutoSize = true;
            this.CurrentItemLabel.Location = new System.Drawing.Point(10, 47);
            this.CurrentItemLabel.Name = "CurrentItemLabel";
            this.CurrentItemLabel.Size = new System.Drawing.Size(66, 13);
            this.CurrentItemLabel.TabIndex = 4;
            this.CurrentItemLabel.Text = "Current item:";
            // 
            // TaskLoaderView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(455, 80);
            this.Controls.Add(this.CurrentItemLabel);
            this.Controls.Add(this.TaskNameLabel);
            this.Controls.Add(this.CancelTaskButton);
            this.Controls.Add(this.ProgressLabel);
            this.Controls.Add(this.TaskProgressBar);
            this.Margin = new System.Windows.Forms.Padding(2);
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
        private System.Windows.Forms.Label CurrentItemLabel;
    }
}