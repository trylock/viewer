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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsView));
            this.ProgramsLabel = new System.Windows.Forms.Label();
            this.SelectFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.SettingsTabs = new System.Windows.Forms.TabControl();
            this.ProgramsTab = new System.Windows.Forms.TabPage();
            this.ProgramsGridView = new Viewer.UI.Forms.BufferedDataGridView();
            this.ThumbnailGridTab = new System.Windows.Forms.TabPage();
            this.ScrollSnappingCheckBox = new System.Windows.Forms.CheckBox();
            this.SettingsTabs.SuspendLayout();
            this.ProgramsTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ProgramsGridView)).BeginInit();
            this.ThumbnailGridTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // ProgramsLabel
            // 
            resources.ApplyResources(this.ProgramsLabel, "ProgramsLabel");
            this.ProgramsLabel.Name = "ProgramsLabel";
            // 
            // SelectFileDialog
            // 
            resources.ApplyResources(this.SelectFileDialog, "SelectFileDialog");
            // 
            // SettingsTabs
            // 
            this.SettingsTabs.Controls.Add(this.ProgramsTab);
            this.SettingsTabs.Controls.Add(this.ThumbnailGridTab);
            resources.ApplyResources(this.SettingsTabs, "SettingsTabs");
            this.SettingsTabs.Multiline = true;
            this.SettingsTabs.Name = "SettingsTabs";
            this.SettingsTabs.SelectedIndex = 0;
            this.SettingsTabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            // 
            // ProgramsTab
            // 
            this.ProgramsTab.Controls.Add(this.ProgramsGridView);
            this.ProgramsTab.Controls.Add(this.ProgramsLabel);
            resources.ApplyResources(this.ProgramsTab, "ProgramsTab");
            this.ProgramsTab.Name = "ProgramsTab";
            this.ProgramsTab.UseVisualStyleBackColor = true;
            // 
            // ProgramsGridView
            // 
            this.ProgramsGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.ProgramsGridView.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(242)))));
            this.ProgramsGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ProgramsGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this.ProgramsGridView, "ProgramsGridView");
            this.ProgramsGridView.Name = "ProgramsGridView";
            this.ProgramsGridView.RowHeadersVisible = false;
            this.ProgramsGridView.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.ProgramsGridView_CellBeginEdit);
            this.ProgramsGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.ProgramsGridView_CellValueChanged);
            this.ProgramsGridView.CurrentCellDirtyStateChanged += new System.EventHandler(this.ProgramsGridView_CurrentCellDirtyStateChanged);
            this.ProgramsGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ProgramsGridView_KeyDown);
            // 
            // ThumbnailGridTab
            // 
            this.ThumbnailGridTab.Controls.Add(this.ScrollSnappingCheckBox);
            resources.ApplyResources(this.ThumbnailGridTab, "ThumbnailGridTab");
            this.ThumbnailGridTab.Name = "ThumbnailGridTab";
            this.ThumbnailGridTab.UseVisualStyleBackColor = true;
            // 
            // ScrollSnappingCheckBox
            // 
            resources.ApplyResources(this.ScrollSnappingCheckBox, "ScrollSnappingCheckBox");
            this.ScrollSnappingCheckBox.Name = "ScrollSnappingCheckBox";
            this.ScrollSnappingCheckBox.UseVisualStyleBackColor = true;
            this.ScrollSnappingCheckBox.CheckedChanged += new System.EventHandler(this.ScrollSnappingCheckBox_CheckedChanged);
            // 
            // SettingsView
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(242)))));
            this.Controls.Add(this.SettingsTabs);
            this.Name = "SettingsView";
            this.SettingsTabs.ResumeLayout(false);
            this.ProgramsTab.ResumeLayout(false);
            this.ProgramsTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ProgramsGridView)).EndInit();
            this.ThumbnailGridTab.ResumeLayout(false);
            this.ThumbnailGridTab.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Forms.BufferedDataGridView ProgramsGridView;
        private System.Windows.Forms.Label ProgramsLabel;
        private System.Windows.Forms.OpenFileDialog SelectFileDialog;
        private System.Windows.Forms.TabControl SettingsTabs;
        private System.Windows.Forms.TabPage ProgramsTab;
        private System.Windows.Forms.TabPage ThumbnailGridTab;
        private System.Windows.Forms.CheckBox ScrollSnappingCheckBox;
    }
}