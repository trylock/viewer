namespace Viewer.UI.UserSettings
{
    partial class SettingsView
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
            this.ProgramsLabel = new System.Windows.Forms.Label();
            this.ProgramsGridView = new Viewer.UI.Forms.BufferedDataGridView();
            this.SelectFileDialog = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.ProgramsGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // ProgramsLabel
            // 
            this.ProgramsLabel.AutoSize = true;
            this.ProgramsLabel.Location = new System.Drawing.Point(13, 9);
            this.ProgramsLabel.Name = "ProgramsLabel";
            this.ProgramsLabel.Size = new System.Drawing.Size(51, 13);
            this.ProgramsLabel.TabIndex = 1;
            this.ProgramsLabel.Text = "Programs";
            // 
            // ProgramsGridView
            // 
            this.ProgramsGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgramsGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.ProgramsGridView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(242)))));
            this.ProgramsGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ProgramsGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ProgramsGridView.Location = new System.Drawing.Point(12, 28);
            this.ProgramsGridView.Name = "ProgramsGridView";
            this.ProgramsGridView.RowHeadersVisible = false;
            this.ProgramsGridView.Size = new System.Drawing.Size(653, 369);
            this.ProgramsGridView.TabIndex = 0;
            this.ProgramsGridView.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.ProgramsGridView_CellBeginEdit);
            this.ProgramsGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.ProgramsGridView_CellValueChanged);
            this.ProgramsGridView.CurrentCellDirtyStateChanged += new System.EventHandler(this.ProgramsGridView_CurrentCellDirtyStateChanged);
            this.ProgramsGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ProgramsGridView_KeyDown);
            // 
            // SelectFileDialog
            // 
            this.SelectFileDialog.Filter = "Executable files|*.exe|All files|*.*";
            // 
            // SettingsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(242)))));
            this.ClientSize = new System.Drawing.Size(677, 409);
            this.Controls.Add(this.ProgramsLabel);
            this.Controls.Add(this.ProgramsGridView);
            this.Name = "SettingsView";
            this.Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)(this.ProgramsGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Forms.BufferedDataGridView ProgramsGridView;
        private System.Windows.Forms.Label ProgramsLabel;
        private System.Windows.Forms.OpenFileDialog SelectFileDialog;
    }
}