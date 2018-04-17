namespace Viewer.UI.Log
{
    partial class LogView
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
            this.LogEntryGridView = new BufferedDataGridView();
            this.TypeColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.MessageColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.GroupColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RetryColumn = new System.Windows.Forms.DataGridViewButtonColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewButtonColumn1 = new System.Windows.Forms.DataGridViewButtonColumn();
            ((System.ComponentModel.ISupportInitialize)(this.LogEntryGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // LogEntryGridView
            // 
            this.LogEntryGridView.AllowUserToAddRows = false;
            this.LogEntryGridView.AllowUserToDeleteRows = false;
            this.LogEntryGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.LogEntryGridView.BackgroundColor = System.Drawing.SystemColors.Control;
            this.LogEntryGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.LogEntryGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.LogEntryGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.TypeColumn,
            this.MessageColumn,
            this.GroupColumn,
            this.RetryColumn});
            this.LogEntryGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogEntryGridView.Location = new System.Drawing.Point(0, 0);
            this.LogEntryGridView.Name = "LogEntryGridView";
            this.LogEntryGridView.ReadOnly = true;
            this.LogEntryGridView.RowHeadersVisible = false;
            this.LogEntryGridView.RowTemplate.Height = 23;
            this.LogEntryGridView.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.LogEntryGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.LogEntryGridView.Size = new System.Drawing.Size(682, 265);
            this.LogEntryGridView.TabIndex = 0;
            // 
            // TypeColumn
            // 
            this.TypeColumn.FillWeight = 10F;
            this.TypeColumn.HeaderText = "";
            this.TypeColumn.Name = "TypeColumn";
            this.TypeColumn.ReadOnly = true;
            // 
            // MessageColumn
            // 
            this.MessageColumn.HeaderText = "Message";
            this.MessageColumn.Name = "MessageColumn";
            this.MessageColumn.ReadOnly = true;
            // 
            // GroupColumn
            // 
            this.GroupColumn.FillWeight = 35F;
            this.GroupColumn.HeaderText = "Group";
            this.GroupColumn.Name = "GroupColumn";
            this.GroupColumn.ReadOnly = true;
            // 
            // RetryColumn
            // 
            this.RetryColumn.FillWeight = 35F;
            this.RetryColumn.HeaderText = "Retry";
            this.RetryColumn.Name = "RetryColumn";
            this.RetryColumn.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Message";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Width = 324;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.FillWeight = 35F;
            this.dataGridViewTextBoxColumn2.HeaderText = "Group";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.Width = 114;
            // 
            // dataGridViewButtonColumn1
            // 
            this.dataGridViewButtonColumn1.FillWeight = 35F;
            this.dataGridViewButtonColumn1.HeaderText = "Retry";
            this.dataGridViewButtonColumn1.Name = "dataGridViewButtonColumn1";
            this.dataGridViewButtonColumn1.Width = 5;
            // 
            // LogView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(682, 265);
            this.Controls.Add(this.LogEntryGridView);
            this.Name = "LogView";
            this.Text = "Log";
            ((System.ComponentModel.ISupportInitialize)(this.LogEntryGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView LogEntryGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewButtonColumn dataGridViewButtonColumn1;
        private System.Windows.Forms.DataGridViewImageColumn TypeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MessageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn GroupColumn;
        private System.Windows.Forms.DataGridViewButtonColumn RetryColumn;
    }
}
