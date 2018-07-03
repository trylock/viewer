using ScintillaNET;

namespace Viewer.UI.Query
{
    partial class QueryView
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
            this.TablePanel = new System.Windows.Forms.TableLayoutPanel();
            this.QueryTextBox = new ScintillaNET.Scintilla();
            this.ControlBarPanel = new System.Windows.Forms.Panel();
            this.RunButton = new System.Windows.Forms.Button();
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
            this.TablePanel.Name = "TablePanel";
            this.TablePanel.RowCount = 2;
            this.TablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.TablePanel.Size = new System.Drawing.Size(627, 335);
            this.TablePanel.TabIndex = 0;
            // 
            // QueryTextBox
            // 
            this.QueryTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.QueryTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.QueryTextBox.Location = new System.Drawing.Point(3, 3);
            this.QueryTextBox.Name = "QueryTextBox";
            this.QueryTextBox.ScrollWidth = 200;
            this.QueryTextBox.Size = new System.Drawing.Size(621, 294);
            this.QueryTextBox.TabIndex = 0;
            this.QueryTextBox.TextChanged += new System.EventHandler(this.QueryTextBox_TextChanged);
            this.QueryTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.QueryTextBox_KeyDown);
            // 
            // ControlBarPanel
            // 
            this.ControlBarPanel.Controls.Add(this.RunButton);
            this.ControlBarPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ControlBarPanel.Location = new System.Drawing.Point(3, 303);
            this.ControlBarPanel.Name = "ControlBarPanel";
            this.ControlBarPanel.Size = new System.Drawing.Size(621, 29);
            this.ControlBarPanel.TabIndex = 1;
            // 
            // RunButton
            // 
            this.RunButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.RunButton.Location = new System.Drawing.Point(536, 0);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(82, 29);
            this.RunButton.TabIndex = 1;
            this.RunButton.Text = "Run";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // QueryView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(627, 335);
            this.Controls.Add(this.TablePanel);
            this.Name = "QueryView";
            this.Text = "Query";
            this.TablePanel.ResumeLayout(false);
            this.ControlBarPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel TablePanel;
        private ScintillaNET.Scintilla QueryTextBox;
        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.Panel ControlBarPanel;
    }
}
