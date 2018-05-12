using Viewer.UI.Forms;

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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.LogEntryGridView = new Viewer.UI.Forms.BufferedDataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewButtonColumn1 = new System.Windows.Forms.DataGridViewButtonColumn();
            this.TypeColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.Line = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MessageColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.GroupColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RetryColumn = new System.Windows.Forms.DataGridViewButtonColumn();
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
            this.Line,
            this.Column,
            this.MessageColumn,
            this.GroupColumn,
            this.RetryColumn});
            this.LogEntryGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogEntryGridView.Location = new System.Drawing.Point(0, 0);
            this.LogEntryGridView.Name = "LogEntryGridView";
            this.LogEntryGridView.ReadOnly = true;
            this.LogEntryGridView.RowHeadersVisible = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.LogEntryGridView.RowsDefaultCellStyle = dataGridViewCellStyle1;
            this.LogEntryGridView.RowTemplate.Height = 23;
            this.LogEntryGridView.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.LogEntryGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.LogEntryGridView.Size = new System.Drawing.Size(756, 265);
            this.LogEntryGridView.TabIndex = 0;
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
            // TypeColumn
            // 
            this.TypeColumn.FillWeight = 17.85629F;
            this.TypeColumn.HeaderText = "";
            this.TypeColumn.Name = "TypeColumn";
            this.TypeColumn.ReadOnly = true;
            // 
            // Line
            // 
            this.Line.FillWeight = 28.93401F;
            this.Line.HeaderText = "Line";
            this.Line.Name = "Line";
            this.Line.ReadOnly = true;
            // 
            // Column
            // 
            this.Column.FillWeight = 29.65273F;
            this.Column.HeaderText = "Column";
            this.Column.Name = "Column";
            this.Column.ReadOnly = true;
            // 
            // MessageColumn
            // 
            this.MessageColumn.FillWeight = 178.5629F;
            this.MessageColumn.HeaderText = "Message";
            this.MessageColumn.Name = "MessageColumn";
            this.MessageColumn.ReadOnly = true;
            // 
            // GroupColumn
            // 
            this.GroupColumn.FillWeight = 62.49702F;
            this.GroupColumn.HeaderText = "Group";
            this.GroupColumn.Name = "GroupColumn";
            this.GroupColumn.ReadOnly = true;
            // 
            // RetryColumn
            // 
            this.RetryColumn.FillWeight = 62.49702F;
            this.RetryColumn.HeaderText = "Retry";
            this.RetryColumn.Name = "RetryColumn";
            this.RetryColumn.ReadOnly = true;
            // 
            // LogView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(756, 265);
            this.Controls.Add(this.LogEntryGridView);
            this.Name = "LogView";
            this.Text = "Log";
            ((System.ComponentModel.ISupportInitialize)(this.LogEntryGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewButtonColumn dataGridViewButtonColumn1;
        private BufferedDataGridView LogEntryGridView;
        private System.Windows.Forms.DataGridViewImageColumn TypeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn Line;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column;
        private System.Windows.Forms.DataGridViewTextBoxColumn MessageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn GroupColumn;
        private System.Windows.Forms.DataGridViewButtonColumn RetryColumn;
    }
}
