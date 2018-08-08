using ScintillaNET;

namespace Viewer.UI.QueryEditor
{
    partial class QueryEditorView
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QueryEditorView));
            this.TablePanel = new System.Windows.Forms.TableLayoutPanel();
            this.QueryTextBox = new ScintillaNET.Scintilla();
            this.ControlBarPanel = new System.Windows.Forms.Panel();
            this.QueryViewLabel = new System.Windows.Forms.Label();
            this.QueryViewComboBox = new System.Windows.Forms.ComboBox();
            this.SaveButton = new System.Windows.Forms.Button();
            this.OpenButton = new System.Windows.Forms.Button();
            this.RunButton = new System.Windows.Forms.Button();
            this.OpenDialog = new System.Windows.Forms.OpenFileDialog();
            this.SaveDialog = new System.Windows.Forms.SaveFileDialog();
            this.SaveToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.OpenToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.RunToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.TablePanel.SuspendLayout();
            this.ControlBarPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // TablePanel
            // 
            this.TablePanel.ColumnCount = 1;
            this.TablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TablePanel.Controls.Add(this.QueryTextBox, 0, 0);
            this.TablePanel.Controls.Add(this.ControlBarPanel, 0, 1);
            this.TablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TablePanel.Location = new System.Drawing.Point(0, 0);
            this.TablePanel.Margin = new System.Windows.Forms.Padding(2);
            this.TablePanel.Name = "TablePanel";
            this.TablePanel.RowCount = 2;
            this.TablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.TablePanel.Size = new System.Drawing.Size(779, 393);
            this.TablePanel.TabIndex = 0;
            // 
            // QueryTextBox
            // 
            this.QueryTextBox.AllowDrop = true;
            this.QueryTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.QueryTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.QueryTextBox.Location = new System.Drawing.Point(2, 2);
            this.QueryTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.QueryTextBox.Name = "QueryTextBox";
            this.QueryTextBox.ScrollWidth = 200;
            this.QueryTextBox.Size = new System.Drawing.Size(775, 361);
            this.QueryTextBox.TabIndex = 0;
            this.QueryTextBox.TextChanged += new System.EventHandler(this.QueryTextBox_TextChanged);
            this.QueryTextBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.QueryTextBox_DragDrop);
            this.QueryTextBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.QueryTextBox_DragEnter);
            this.QueryTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.QueryTextBox_KeyDown);
            // 
            // ControlBarPanel
            // 
            this.ControlBarPanel.Controls.Add(this.QueryViewLabel);
            this.ControlBarPanel.Controls.Add(this.QueryViewComboBox);
            this.ControlBarPanel.Controls.Add(this.SaveButton);
            this.ControlBarPanel.Controls.Add(this.OpenButton);
            this.ControlBarPanel.Controls.Add(this.RunButton);
            this.ControlBarPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ControlBarPanel.Location = new System.Drawing.Point(2, 367);
            this.ControlBarPanel.Margin = new System.Windows.Forms.Padding(2);
            this.ControlBarPanel.Name = "ControlBarPanel";
            this.ControlBarPanel.Size = new System.Drawing.Size(775, 24);
            this.ControlBarPanel.TabIndex = 1;
            // 
            // QueryViewLabel
            // 
            this.QueryViewLabel.AutoSize = true;
            this.QueryViewLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.QueryViewLabel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.QueryViewLabel.Location = new System.Drawing.Point(65, 5);
            this.QueryViewLabel.Name = "QueryViewLabel";
            this.QueryViewLabel.Size = new System.Drawing.Size(49, 13);
            this.QueryViewLabel.TabIndex = 5;
            this.QueryViewLabel.Text = "VIEWS:";
            // 
            // QueryViewComboBox
            // 
            this.QueryViewComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.QueryViewComboBox.FormattingEnabled = true;
            this.QueryViewComboBox.Location = new System.Drawing.Point(120, 1);
            this.QueryViewComboBox.Name = "QueryViewComboBox";
            this.QueryViewComboBox.Size = new System.Drawing.Size(121, 21);
            this.QueryViewComboBox.TabIndex = 4;
            this.QueryViewComboBox.SelectionChangeCommitted += new System.EventHandler(this.QueryViewComboBox_SelectionChangeCommitted);
            // 
            // SaveButton
            // 
            this.SaveButton.BackgroundImage = global::Viewer.Properties.Resources.Save;
            this.SaveButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.SaveButton.FlatAppearance.BorderSize = 0;
            this.SaveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SaveButton.Location = new System.Drawing.Point(32, 0);
            this.SaveButton.Margin = new System.Windows.Forms.Padding(2);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(28, 24);
            this.SaveButton.TabIndex = 3;
            this.SaveToolTip.SetToolTip(this.SaveButton, "Ctrl + S");
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // OpenButton
            // 
            this.OpenButton.BackgroundImage = global::Viewer.Properties.Resources.Open;
            this.OpenButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.OpenButton.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.OpenButton.FlatAppearance.BorderSize = 0;
            this.OpenButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.OpenButton.Location = new System.Drawing.Point(2, 0);
            this.OpenButton.Margin = new System.Windows.Forms.Padding(2);
            this.OpenButton.Name = "OpenButton";
            this.OpenButton.Size = new System.Drawing.Size(25, 24);
            this.OpenButton.TabIndex = 2;
            this.OpenToolTip.SetToolTip(this.OpenButton, "Ctrl + O");
            this.OpenButton.UseVisualStyleBackColor = true;
            this.OpenButton.Click += new System.EventHandler(this.OpenButton_Click);
            // 
            // RunButton
            // 
            this.RunButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.RunButton.BackgroundImage = global::Viewer.Properties.Resources.Start;
            this.RunButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.RunButton.FlatAppearance.BorderSize = 0;
            this.RunButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RunButton.Location = new System.Drawing.Point(746, 0);
            this.RunButton.Margin = new System.Windows.Forms.Padding(2);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(26, 24);
            this.RunButton.TabIndex = 1;
            this.RunToolTip.SetToolTip(this.RunButton, "F5");
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // OpenDialog
            // 
            this.OpenDialog.FileName = "openFileDialog1";
            // 
            // SaveDialog
            // 
            this.SaveDialog.DefaultExt = "vql";
            // 
            // SaveToolTip
            // 
            this.SaveToolTip.ToolTipTitle = "Save";
            // 
            // OpenToolTip
            // 
            this.OpenToolTip.ToolTipTitle = "Open";
            // 
            // RunToolTip
            // 
            this.RunToolTip.ToolTipTitle = "Run";
            // 
            // QueryEditorView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(779, 393);
            this.Controls.Add(this.TablePanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "QueryEditorView";
            this.Text = "Query";
            this.TablePanel.ResumeLayout(false);
            this.ControlBarPanel.ResumeLayout(false);
            this.ControlBarPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel TablePanel;
        private ScintillaNET.Scintilla QueryTextBox;
        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.Panel ControlBarPanel;
        private System.Windows.Forms.Button OpenButton;
        private System.Windows.Forms.Button SaveButton;
        private System.Windows.Forms.OpenFileDialog OpenDialog;
        private System.Windows.Forms.SaveFileDialog SaveDialog;
        private System.Windows.Forms.ComboBox QueryViewComboBox;
        private System.Windows.Forms.Label QueryViewLabel;
        private System.Windows.Forms.ToolTip SaveToolTip;
        private System.Windows.Forms.ToolTip OpenToolTip;
        private System.Windows.Forms.ToolTip RunToolTip;
    }
}
