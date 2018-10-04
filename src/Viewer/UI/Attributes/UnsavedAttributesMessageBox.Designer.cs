namespace Viewer.UI.Attributes
{
    partial class UnsavedAttributesMessageBox
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
            this.MessageLabel = new System.Windows.Forms.Label();
            this.SaveButton = new System.Windows.Forms.Button();
            this.RevertButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.DecisionPanel = new System.Windows.Forms.Panel();
            this.IconPictureBox = new System.Windows.Forms.PictureBox();
            this.DecisionPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.IconPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // MessageLabel
            // 
            this.MessageLabel.AutoSize = true;
            this.MessageLabel.Location = new System.Drawing.Point(69, 24);
            this.MessageLabel.Name = "MessageLabel";
            this.MessageLabel.Size = new System.Drawing.Size(227, 26);
            this.MessageLabel.TabIndex = 0;
            this.MessageLabel.Text = "Some attributes have not been saved to a file. \r\nWhat do you wish to do with them" +
    "?";
            // 
            // SaveButton
            // 
            this.SaveButton.Location = new System.Drawing.Point(59, 8);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(75, 23);
            this.SaveButton.TabIndex = 1;
            this.SaveButton.Text = "Save";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // RevertButton
            // 
            this.RevertButton.Location = new System.Drawing.Point(140, 8);
            this.RevertButton.Name = "RevertButton";
            this.RevertButton.Size = new System.Drawing.Size(75, 23);
            this.RevertButton.TabIndex = 2;
            this.RevertButton.Text = "Revert";
            this.RevertButton.UseVisualStyleBackColor = true;
            this.RevertButton.Click += new System.EventHandler(this.RevertButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Location = new System.Drawing.Point(221, 8);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 3;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // DecisionPanel
            // 
            this.DecisionPanel.BackColor = System.Drawing.SystemColors.Control;
            this.DecisionPanel.Controls.Add(this.SaveButton);
            this.DecisionPanel.Controls.Add(this.CancelButton);
            this.DecisionPanel.Controls.Add(this.RevertButton);
            this.DecisionPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.DecisionPanel.Location = new System.Drawing.Point(0, 66);
            this.DecisionPanel.Name = "DecisionPanel";
            this.DecisionPanel.Size = new System.Drawing.Size(308, 39);
            this.DecisionPanel.TabIndex = 4;
            // 
            // IconPictureBox
            // 
            this.IconPictureBox.Location = new System.Drawing.Point(13, 13);
            this.IconPictureBox.Name = "IconPictureBox";
            this.IconPictureBox.Size = new System.Drawing.Size(40, 37);
            this.IconPictureBox.TabIndex = 5;
            this.IconPictureBox.TabStop = false;
            // 
            // UnsavedAttributesMessageBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(308, 105);
            this.Controls.Add(this.IconPictureBox);
            this.Controls.Add(this.DecisionPanel);
            this.Controls.Add(this.MessageLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UnsavedAttributesMessageBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Unsaved attributes";
            this.DecisionPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.IconPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label MessageLabel;
        private System.Windows.Forms.Button SaveButton;
        private System.Windows.Forms.Button RevertButton;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Panel DecisionPanel;
        private System.Windows.Forms.PictureBox IconPictureBox;
    }
}