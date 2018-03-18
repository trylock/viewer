namespace Viewer.UI
{
    partial class ThumbnailControl
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
            this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.TablePanel = new System.Windows.Forms.TableLayoutPanel();
            this.ThumbnailPictureBox = new System.Windows.Forms.PictureBox();
            this.FileNameLabel = new System.Windows.Forms.Label();
            this.LayoutPanel.SuspendLayout();
            this.TablePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ThumbnailPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // LayoutPanel
            // 
            this.LayoutPanel.ColumnCount = 1;
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.LayoutPanel.Controls.Add(this.TablePanel, 0, 0);
            this.LayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.LayoutPanel.Name = "LayoutPanel";
            this.LayoutPanel.RowCount = 2;
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.LayoutPanel.Size = new System.Drawing.Size(230, 230);
            this.LayoutPanel.TabIndex = 0;
            // 
            // TablePanel
            // 
            this.TablePanel.ColumnCount = 1;
            this.TablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TablePanel.Controls.Add(this.ThumbnailPictureBox, 0, 0);
            this.TablePanel.Controls.Add(this.FileNameLabel, 0, 1);
            this.TablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TablePanel.Location = new System.Drawing.Point(3, 3);
            this.TablePanel.Name = "TablePanel";
            this.TablePanel.RowCount = 2;
            this.TablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.TablePanel.Size = new System.Drawing.Size(224, 224);
            this.TablePanel.TabIndex = 0;
            // 
            // ThumbnailPictureBox
            // 
            this.ThumbnailPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ThumbnailPictureBox.Location = new System.Drawing.Point(3, 3);
            this.ThumbnailPictureBox.Name = "ThumbnailPictureBox";
            this.ThumbnailPictureBox.Size = new System.Drawing.Size(218, 183);
            this.ThumbnailPictureBox.TabIndex = 0;
            this.ThumbnailPictureBox.TabStop = false;
            this.ThumbnailPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.ThumbnailPictureBox_Paint);
            // 
            // FileNameLabel
            // 
            this.FileNameLabel.AutoSize = true;
            this.FileNameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FileNameLabel.Location = new System.Drawing.Point(3, 189);
            this.FileNameLabel.Name = "FileNameLabel";
            this.FileNameLabel.Size = new System.Drawing.Size(218, 35);
            this.FileNameLabel.TabIndex = 1;
            this.FileNameLabel.Text = "file name";
            this.FileNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ThumbnailControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.LayoutPanel);
            this.Name = "ThumbnailControl";
            this.Size = new System.Drawing.Size(230, 230);
            this.LayoutPanel.ResumeLayout(false);
            this.TablePanel.ResumeLayout(false);
            this.TablePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ThumbnailPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel LayoutPanel;
        private System.Windows.Forms.TableLayoutPanel TablePanel;
        private System.Windows.Forms.PictureBox ThumbnailPictureBox;
        private System.Windows.Forms.Label FileNameLabel;
    }
}
