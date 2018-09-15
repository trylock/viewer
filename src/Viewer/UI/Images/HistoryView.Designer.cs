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
            Viewer.UI.Forms.StateStyles stateStylesGroup1 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles1 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup2 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles2 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup3 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles3 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup4 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles4 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup5 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles5 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup6 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles6 = new Viewer.UI.Forms.Styles();
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
            styles1.FillColor = System.Drawing.Color.Empty;
            styles1.IsFillEnabled = false;
            styles1.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles1.StrokeColor = System.Drawing.Color.Empty;
            styles1.StrokeWidth = 0;
            stateStylesGroup1.Disabled = styles1;
            stateStylesGroup1.Hover = styles1;
            stateStylesGroup1.Normal = styles1;
            this.GoBackButton.ButtonStyles = stateStylesGroup1;
            this.GoBackButton.Image = null;
            styles2.FillColor = System.Drawing.Color.Black;
            styles2.IsFillEnabled = false;
            styles2.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;
            styles2.StrokeColor = System.Drawing.Color.FromArgb(((int)(((byte)(65)))), ((int)(((byte)(65)))), ((int)(((byte)(65)))));
            styles2.StrokeWidth = 2;
            stateStylesGroup2.Disabled = styles2;
            stateStylesGroup2.Hover = styles2;
            stateStylesGroup2.Normal = styles2;
            this.GoBackButton.ImageStyles = stateStylesGroup2;
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
            styles3.FillColor = System.Drawing.Color.Empty;
            styles3.IsFillEnabled = false;
            styles3.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles3.StrokeColor = System.Drawing.Color.Empty;
            styles3.StrokeWidth = 0;
            stateStylesGroup3.Disabled = styles3;
            stateStylesGroup3.Hover = styles3;
            stateStylesGroup3.Normal = styles3;
            this.GoForwardButton.ButtonStyles = stateStylesGroup3;
            this.GoForwardButton.Image = null;
            styles4.FillColor = System.Drawing.Color.Black;
            styles4.IsFillEnabled = true;
            styles4.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles4.StrokeColor = System.Drawing.Color.Empty;
            styles4.StrokeWidth = 0;
            stateStylesGroup4.Disabled = styles4;
            stateStylesGroup4.Hover = styles4;
            stateStylesGroup4.Normal = styles4;
            this.GoForwardButton.ImageStyles = stateStylesGroup4;
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
            styles5.FillColor = System.Drawing.Color.Empty;
            styles5.IsFillEnabled = false;
            styles5.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles5.StrokeColor = System.Drawing.Color.Empty;
            styles5.StrokeWidth = 0;
            stateStylesGroup5.Disabled = styles5;
            stateStylesGroup5.Hover = styles5;
            stateStylesGroup5.Normal = styles5;
            this.GoUpButton.ButtonStyles = stateStylesGroup5;
            this.GoUpButton.Image = null;
            styles6.FillColor = System.Drawing.Color.Black;
            styles6.IsFillEnabled = true;
            styles6.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles6.StrokeColor = System.Drawing.Color.Empty;
            styles6.StrokeWidth = 0;
            stateStylesGroup6.Disabled = styles6;
            stateStylesGroup6.Hover = styles6;
            stateStylesGroup6.Normal = styles6;
            this.GoUpButton.ImageStyles = stateStylesGroup6;
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
