namespace Viewer.UI.Images
{
    partial class HistoryView
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
            Viewer.UI.Forms.VectorStylesGroup vectorStylesGroup1 = new Viewer.UI.Forms.VectorStylesGroup();
            Viewer.UI.Forms.VectorStyles vectorStyles1 = new Viewer.UI.Forms.VectorStyles();
            Viewer.UI.Forms.VectorStylesGroup vectorStylesGroup2 = new Viewer.UI.Forms.VectorStylesGroup();
            Viewer.UI.Forms.VectorStyles vectorStyles2 = new Viewer.UI.Forms.VectorStyles();
            Viewer.UI.Forms.VectorStylesGroup vectorStylesGroup3 = new Viewer.UI.Forms.VectorStylesGroup();
            Viewer.UI.Forms.VectorStyles vectorStyles3 = new Viewer.UI.Forms.VectorStyles();
            Viewer.UI.Forms.VectorStylesGroup vectorStylesGroup4 = new Viewer.UI.Forms.VectorStylesGroup();
            Viewer.UI.Forms.VectorStyles vectorStyles4 = new Viewer.UI.Forms.VectorStyles();
            Viewer.UI.Forms.VectorStylesGroup vectorStylesGroup5 = new Viewer.UI.Forms.VectorStylesGroup();
            Viewer.UI.Forms.VectorStyles vectorStyles5 = new Viewer.UI.Forms.VectorStyles();
            Viewer.UI.Forms.VectorStylesGroup vectorStylesGroup6 = new Viewer.UI.Forms.VectorStylesGroup();
            Viewer.UI.Forms.VectorStyles vectorStyles6 = new Viewer.UI.Forms.VectorStyles();
            this.HistoryComboBox = new System.Windows.Forms.ComboBox();
            this.BackTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.GoBackButton = new Viewer.UI.Forms.VectorButton();
            this.ForwardTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.GoForwardButton = new Viewer.UI.Forms.VectorButton();
            this.UpTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.GoUpButton = new Viewer.UI.Forms.VectorButton();
            this.SuspendLayout();
            // 
            // HistoryComboBox
            // 
            this.HistoryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.HistoryComboBox.FormattingEnabled = true;
            this.HistoryComboBox.Location = new System.Drawing.Point(82, 4);
            this.HistoryComboBox.Name = "HistoryComboBox";
            this.HistoryComboBox.Size = new System.Drawing.Size(603, 21);
            this.HistoryComboBox.TabIndex = 0;
            this.HistoryComboBox.SelectedIndexChanged += new System.EventHandler(this.HistoryComboBox_SelectedIndexChanged);
            this.HistoryComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HistoryComboBox_KeyDown);
            // 
            // BackTooltip
            // 
            this.BackTooltip.ToolTipTitle = "Go to the previous query ";
            // 
            // GoBackButton
            // 
            vectorStyles1.FillColor = System.Drawing.Color.Empty;
            vectorStyles1.IsFillEnabled = false;
            vectorStyles1.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            vectorStyles1.StrokeColor = System.Drawing.Color.Empty;
            vectorStyles1.StrokeWidth = 0;
            vectorStylesGroup1.Disabled = vectorStyles1;
            vectorStylesGroup1.Hover = vectorStyles1;
            vectorStylesGroup1.Normal = vectorStyles1;
            this.GoBackButton.ButtonStyles = vectorStylesGroup1;
            this.GoBackButton.Image = null;
            vectorStyles2.FillColor = System.Drawing.Color.Black;
            vectorStyles2.IsFillEnabled = false;
            vectorStyles2.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;
            vectorStyles2.StrokeColor = System.Drawing.Color.FromArgb(((int)(((byte)(65)))), ((int)(((byte)(65)))), ((int)(((byte)(65)))));
            vectorStyles2.StrokeWidth = 2;
            vectorStylesGroup2.Disabled = vectorStyles2;
            vectorStylesGroup2.Hover = vectorStyles2;
            vectorStylesGroup2.Normal = vectorStyles2;
            this.GoBackButton.ImageStyles = vectorStylesGroup2;
            this.GoBackButton.Location = new System.Drawing.Point(3, 3);
            this.GoBackButton.Name = "GoBackButton";
            this.GoBackButton.Padding = new System.Windows.Forms.Padding(5);
            this.GoBackButton.Size = new System.Drawing.Size(24, 24);
            this.GoBackButton.TabIndex = 4;
            this.GoBackButton.Text = "vectorButton1";
            this.BackTooltip.SetToolTip(this.GoBackButton, "Alt + Left Arrow, MB4");
            this.GoBackButton.Click += new System.EventHandler(this.GoBackButton_Click);
            // 
            // ForwardTooltip
            // 
            this.ForwardTooltip.ToolTipTitle = "Go to the next query";
            // 
            // GoForwardButton
            // 
            vectorStyles3.FillColor = System.Drawing.Color.Empty;
            vectorStyles3.IsFillEnabled = false;
            vectorStyles3.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            vectorStyles3.StrokeColor = System.Drawing.Color.Empty;
            vectorStyles3.StrokeWidth = 0;
            vectorStylesGroup3.Disabled = vectorStyles3;
            vectorStylesGroup3.Hover = vectorStyles3;
            vectorStylesGroup3.Normal = vectorStyles3;
            this.GoForwardButton.ButtonStyles = vectorStylesGroup3;
            this.GoForwardButton.Image = null;
            vectorStyles4.FillColor = System.Drawing.Color.Black;
            vectorStyles4.IsFillEnabled = true;
            vectorStyles4.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            vectorStyles4.StrokeColor = System.Drawing.Color.Empty;
            vectorStyles4.StrokeWidth = 0;
            vectorStylesGroup4.Disabled = vectorStyles4;
            vectorStylesGroup4.Hover = vectorStyles4;
            vectorStylesGroup4.Normal = vectorStyles4;
            this.GoForwardButton.ImageStyles = vectorStylesGroup4;
            this.GoForwardButton.Location = new System.Drawing.Point(30, 3);
            this.GoForwardButton.Name = "GoForwardButton";
            this.GoForwardButton.Padding = new System.Windows.Forms.Padding(5);
            this.GoForwardButton.Size = new System.Drawing.Size(24, 24);
            this.GoForwardButton.TabIndex = 5;
            this.GoForwardButton.Text = "vectorButton1";
            this.ForwardTooltip.SetToolTip(this.GoForwardButton, "Alt + Right Arrow, MB5");
            this.GoForwardButton.Click += new System.EventHandler(this.GoForwardButton_Click);
            // 
            // UpTooltip
            // 
            this.UpTooltip.ToolTipTitle = "Go to the parent directory";
            // 
            // GoUpButton
            // 
            vectorStyles5.FillColor = System.Drawing.Color.Empty;
            vectorStyles5.IsFillEnabled = false;
            vectorStyles5.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            vectorStyles5.StrokeColor = System.Drawing.Color.Empty;
            vectorStyles5.StrokeWidth = 0;
            vectorStylesGroup5.Disabled = vectorStyles5;
            vectorStylesGroup5.Hover = vectorStyles5;
            vectorStylesGroup5.Normal = vectorStyles5;
            this.GoUpButton.ButtonStyles = vectorStylesGroup5;
            this.GoUpButton.Image = null;
            vectorStyles6.FillColor = System.Drawing.Color.Black;
            vectorStyles6.IsFillEnabled = true;
            vectorStyles6.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            vectorStyles6.StrokeColor = System.Drawing.Color.Empty;
            vectorStyles6.StrokeWidth = 0;
            vectorStylesGroup6.Disabled = vectorStyles6;
            vectorStylesGroup6.Hover = vectorStyles6;
            vectorStylesGroup6.Normal = vectorStyles6;
            this.GoUpButton.ImageStyles = vectorStylesGroup6;
            this.GoUpButton.Location = new System.Drawing.Point(55, 3);
            this.GoUpButton.Name = "GoUpButton";
            this.GoUpButton.Padding = new System.Windows.Forms.Padding(5);
            this.GoUpButton.Size = new System.Drawing.Size(24, 24);
            this.GoUpButton.TabIndex = 6;
            this.GoUpButton.Text = "vectorButton1";
            this.UpTooltip.SetToolTip(this.GoUpButton, "Alt + Up Arrow");
            this.GoUpButton.Click += new System.EventHandler(this.GoUpButton_Click);
            // 
            // HistoryView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.GoUpButton);
            this.Controls.Add(this.GoForwardButton);
            this.Controls.Add(this.GoBackButton);
            this.Controls.Add(this.HistoryComboBox);
            this.Name = "HistoryView";
            this.Size = new System.Drawing.Size(688, 29);
            this.Layout += new System.Windows.Forms.LayoutEventHandler(this.HistoryView_Layout);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox HistoryComboBox;
        private System.Windows.Forms.ToolTip ForwardTooltip;
        private System.Windows.Forms.ToolTip UpTooltip;
        private System.Windows.Forms.ToolTip BackTooltip;
        private Forms.VectorButton GoBackButton;
        private Forms.VectorButton GoForwardButton;
        private Forms.VectorButton GoUpButton;
    }
}
