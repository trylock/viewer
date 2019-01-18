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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HistoryView));
            Viewer.UI.Forms.StateStyles stateStyles1 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles1 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles2 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles2 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles3 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles3 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles4 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles4 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles5 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles5 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStyles6 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles6 = new Viewer.UI.Forms.Styles();
            this.HistoryComboBox = new System.Windows.Forms.ComboBox();
            this.BackTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.GoBackButton = new Viewer.UI.Forms.VectorButton();
            this.GoForwardButton = new Viewer.UI.Forms.VectorButton();
            this.GoUpButton = new Viewer.UI.Forms.VectorButton();
            this.ForwardTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.UpTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // HistoryComboBox
            // 
            this.HistoryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.HistoryComboBox.FormattingEnabled = true;
            resources.ApplyResources(this.HistoryComboBox, "HistoryComboBox");
            this.HistoryComboBox.Name = "HistoryComboBox";
            this.HistoryComboBox.SelectedIndexChanged += new System.EventHandler(this.HistoryComboBox_SelectedIndexChanged);
            this.HistoryComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HistoryComboBox_KeyDown);
            // 
            // BackTooltip
            // 
            this.BackTooltip.ToolTipTitle = "Previous query";
            // 
            // GoBackButton
            // 
            styles1.FillColor = System.Drawing.Color.Empty;
            styles1.IsFillEnabled = false;
            styles1.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles1.StrokeColor = System.Drawing.Color.Empty;
            styles1.StrokeWidth = 0;
            stateStyles1.Disabled = styles1;
            stateStyles1.Hover = styles1;
            stateStyles1.Normal = styles1;
            this.GoBackButton.ButtonStyles = stateStyles1;
            this.GoBackButton.Image = null;
            styles2.FillColor = System.Drawing.Color.Black;
            styles2.IsFillEnabled = false;
            styles2.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;
            styles2.StrokeColor = System.Drawing.Color.FromArgb(((int)(((byte)(65)))), ((int)(((byte)(65)))), ((int)(((byte)(65)))));
            styles2.StrokeWidth = 2;
            stateStyles2.Disabled = styles2;
            stateStyles2.Hover = styles2;
            stateStyles2.Normal = styles2;
            this.GoBackButton.ImageStyles = stateStyles2;
            resources.ApplyResources(this.GoBackButton, "GoBackButton");
            this.GoBackButton.Name = "GoBackButton";
            this.BackTooltip.SetToolTip(this.GoBackButton, resources.GetString("GoBackButton.ToolTip"));
            this.GoBackButton.Click += new System.EventHandler(this.GoBackButton_Click);
            // 
            // GoForwardButton
            // 
            styles3.FillColor = System.Drawing.Color.Empty;
            styles3.IsFillEnabled = false;
            styles3.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles3.StrokeColor = System.Drawing.Color.Empty;
            styles3.StrokeWidth = 0;
            stateStyles3.Disabled = styles3;
            stateStyles3.Hover = styles3;
            stateStyles3.Normal = styles3;
            this.GoForwardButton.ButtonStyles = stateStyles3;
            this.GoForwardButton.Image = null;
            styles4.FillColor = System.Drawing.Color.Black;
            styles4.IsFillEnabled = true;
            styles4.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles4.StrokeColor = System.Drawing.Color.Empty;
            styles4.StrokeWidth = 0;
            stateStyles4.Disabled = styles4;
            stateStyles4.Hover = styles4;
            stateStyles4.Normal = styles4;
            this.GoForwardButton.ImageStyles = stateStyles4;
            resources.ApplyResources(this.GoForwardButton, "GoForwardButton");
            this.GoForwardButton.Name = "GoForwardButton";
            this.BackTooltip.SetToolTip(this.GoForwardButton, resources.GetString("GoForwardButton.ToolTip"));
            this.ForwardTooltip.SetToolTip(this.GoForwardButton, resources.GetString("GoForwardButton.ToolTip1"));
            this.GoForwardButton.Click += new System.EventHandler(this.GoForwardButton_Click);
            // 
            // GoUpButton
            // 
            styles5.FillColor = System.Drawing.Color.Empty;
            styles5.IsFillEnabled = false;
            styles5.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles5.StrokeColor = System.Drawing.Color.Empty;
            styles5.StrokeWidth = 0;
            stateStyles5.Disabled = styles5;
            stateStyles5.Hover = styles5;
            stateStyles5.Normal = styles5;
            this.GoUpButton.ButtonStyles = stateStyles5;
            this.GoUpButton.Image = null;
            styles6.FillColor = System.Drawing.Color.Black;
            styles6.IsFillEnabled = true;
            styles6.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles6.StrokeColor = System.Drawing.Color.Empty;
            styles6.StrokeWidth = 0;
            stateStyles6.Disabled = styles6;
            stateStyles6.Hover = styles6;
            stateStyles6.Normal = styles6;
            this.GoUpButton.ImageStyles = stateStyles6;
            resources.ApplyResources(this.GoUpButton, "GoUpButton");
            this.GoUpButton.Name = "GoUpButton";
            this.BackTooltip.SetToolTip(this.GoUpButton, resources.GetString("GoUpButton.ToolTip"));
            this.UpTooltip.SetToolTip(this.GoUpButton, resources.GetString("GoUpButton.ToolTip1"));
            this.GoUpButton.Click += new System.EventHandler(this.GoUpButton_Click);
            // 
            // ForwardTooltip
            // 
            this.ForwardTooltip.ToolTipTitle = "Next query";
            // 
            // UpTooltip
            // 
            this.UpTooltip.ToolTipTitle = "Parent directory";
            // 
            // HistoryView
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(242)))));
            this.Controls.Add(this.GoUpButton);
            this.Controls.Add(this.GoForwardButton);
            this.Controls.Add(this.GoBackButton);
            this.Controls.Add(this.HistoryComboBox);
            this.Name = "HistoryView";
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
