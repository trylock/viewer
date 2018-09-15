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
            Viewer.UI.Forms.StateStyles stateStyles7 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles7 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles8 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles8 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles9 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles9 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles10 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles10 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles11 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles11 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles12 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles12 = new Viewer.UI.Forms.Styles();
            this.HistoryComboBox = new System.Windows.Forms.ComboBox();
            this.BackTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.ForwardTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.UpTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.GoUpButton = new Viewer.UI.Forms.VectorButton();
            this.GoForwardButton = new Viewer.UI.Forms.VectorButton();
            this.GoBackButton = new Viewer.UI.Forms.VectorButton();
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
            // ForwardTooltip
            // 
            this.ForwardTooltip.ToolTipTitle = "Go to the next query";
            // 
            // UpTooltip
            // 
            this.UpTooltip.ToolTipTitle = "Go to the parent directory";
            // 
            // GoUpButton
            // 
            styles7.FillColor = System.Drawing.Color.Empty;
            styles7.IsFillEnabled = false;
            styles7.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles7.StrokeColor = System.Drawing.Color.Empty;
            styles7.StrokeWidth = 0;
            stateStyles7.Disabled = styles7;
            stateStyles7.Hover = styles7;
            stateStyles7.Normal = styles7;
            this.GoUpButton.ButtonStyles = stateStyles7;
            this.GoUpButton.Image = null;
            styles8.FillColor = System.Drawing.Color.Black;
            styles8.IsFillEnabled = true;
            styles8.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles8.StrokeColor = System.Drawing.Color.Empty;
            styles8.StrokeWidth = 0;
            stateStyles8.Disabled = styles8;
            stateStyles8.Hover = styles8;
            stateStyles8.Normal = styles8;
            this.GoUpButton.ImageStyles = stateStyles8;
            this.GoUpButton.Location = new System.Drawing.Point(55, 3);
            this.GoUpButton.Name = "GoUpButton";
            this.GoUpButton.Padding = new System.Windows.Forms.Padding(5);
            this.GoUpButton.Size = new System.Drawing.Size(24, 24);
            this.GoUpButton.TabIndex = 6;
            this.GoUpButton.Text = "vectorButton1";
            this.UpTooltip.SetToolTip(this.GoUpButton, "Alt + Up Arrow");
            this.GoUpButton.Click += new System.EventHandler(this.GoUpButton_Click);
            // 
            // GoForwardButton
            // 
            styles9.FillColor = System.Drawing.Color.Empty;
            styles9.IsFillEnabled = false;
            styles9.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles9.StrokeColor = System.Drawing.Color.Empty;
            styles9.StrokeWidth = 0;
            stateStyles9.Disabled = styles9;
            stateStyles9.Hover = styles9;
            stateStyles9.Normal = styles9;
            this.GoForwardButton.ButtonStyles = stateStyles9;
            this.GoForwardButton.Image = null;
            styles10.FillColor = System.Drawing.Color.Black;
            styles10.IsFillEnabled = true;
            styles10.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles10.StrokeColor = System.Drawing.Color.Empty;
            styles10.StrokeWidth = 0;
            stateStyles10.Disabled = styles10;
            stateStyles10.Hover = styles10;
            stateStyles10.Normal = styles10;
            this.GoForwardButton.ImageStyles = stateStyles10;
            this.GoForwardButton.Location = new System.Drawing.Point(30, 3);
            this.GoForwardButton.Name = "GoForwardButton";
            this.GoForwardButton.Padding = new System.Windows.Forms.Padding(5);
            this.GoForwardButton.Size = new System.Drawing.Size(24, 24);
            this.GoForwardButton.TabIndex = 5;
            this.GoForwardButton.Text = "vectorButton1";
            this.ForwardTooltip.SetToolTip(this.GoForwardButton, "Alt + Right Arrow, MB5");
            this.GoForwardButton.Click += new System.EventHandler(this.GoForwardButton_Click);
            // 
            // GoBackButton
            // 
            styles11.FillColor = System.Drawing.Color.Empty;
            styles11.IsFillEnabled = false;
            styles11.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles11.StrokeColor = System.Drawing.Color.Empty;
            styles11.StrokeWidth = 0;
            stateStyles11.Disabled = styles11;
            stateStyles11.Hover = styles11;
            stateStyles11.Normal = styles11;
            this.GoBackButton.ButtonStyles = stateStyles11;
            this.GoBackButton.Image = null;
            styles12.FillColor = System.Drawing.Color.Black;
            styles12.IsFillEnabled = false;
            styles12.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;
            styles12.StrokeColor = System.Drawing.Color.FromArgb(((int)(((byte)(65)))), ((int)(((byte)(65)))), ((int)(((byte)(65)))));
            styles12.StrokeWidth = 2;
            stateStyles12.Disabled = styles12;
            stateStyles12.Hover = styles12;
            stateStyles12.Normal = styles12;
            this.GoBackButton.ImageStyles = stateStyles12;
            this.GoBackButton.Location = new System.Drawing.Point(3, 3);
            this.GoBackButton.Name = "GoBackButton";
            this.GoBackButton.Padding = new System.Windows.Forms.Padding(5);
            this.GoBackButton.Size = new System.Drawing.Size(24, 24);
            this.GoBackButton.TabIndex = 4;
            this.GoBackButton.Text = "vectorButton1";
            this.BackTooltip.SetToolTip(this.GoBackButton, "Alt + Left Arrow, MB4");
            this.GoBackButton.Click += new System.EventHandler(this.GoBackButton_Click);
            // 
            // HistoryView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(242)))));
            this.Controls.Add(this.GoUpButton);
            this.Controls.Add(this.GoForwardButton);
            this.Controls.Add(this.GoBackButton);
            this.Controls.Add(this.HistoryComboBox);
            this.Name = "HistoryView";
            this.Size = new System.Drawing.Size(688, 28);
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
