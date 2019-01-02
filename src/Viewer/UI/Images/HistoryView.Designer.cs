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
            this.GoBackButton = new Viewer.UI.Forms.VectorButton();
            this.GoForwardButton = new Viewer.UI.Forms.VectorButton();
            this.GoUpButton = new Viewer.UI.Forms.VectorButton();
            this.ForwardTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.UpTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // HistoryComboBox
            // 
            resources.ApplyResources(this.HistoryComboBox, "HistoryComboBox");
            this.HistoryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.HistoryComboBox.FormattingEnabled = true;
            this.HistoryComboBox.Name = "HistoryComboBox";
            this.ForwardTooltip.SetToolTip(this.HistoryComboBox, resources.GetString("HistoryComboBox.ToolTip"));
            this.BackTooltip.SetToolTip(this.HistoryComboBox, resources.GetString("HistoryComboBox.ToolTip1"));
            this.UpTooltip.SetToolTip(this.HistoryComboBox, resources.GetString("HistoryComboBox.ToolTip2"));
            this.HistoryComboBox.SelectedIndexChanged += new System.EventHandler(this.HistoryComboBox_SelectedIndexChanged);
            this.HistoryComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HistoryComboBox_KeyDown);
            // 
            // BackTooltip
            // 
            this.BackTooltip.ToolTipTitle = "Předchozí dotaz";
            // 
            // GoBackButton
            // 
            resources.ApplyResources(this.GoBackButton, "GoBackButton");
            styles7.FillColor = System.Drawing.Color.Empty;
            styles7.IsFillEnabled = false;
            styles7.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles7.StrokeColor = System.Drawing.Color.Empty;
            styles7.StrokeWidth = 0;
            stateStyles7.Disabled = styles7;
            stateStyles7.Hover = styles7;
            stateStyles7.Normal = styles7;
            this.GoBackButton.ButtonStyles = stateStyles7;
            this.GoBackButton.Image = null;
            styles8.FillColor = System.Drawing.Color.Black;
            styles8.IsFillEnabled = false;
            styles8.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;
            styles8.StrokeColor = System.Drawing.Color.FromArgb(((int)(((byte)(65)))), ((int)(((byte)(65)))), ((int)(((byte)(65)))));
            styles8.StrokeWidth = 2;
            stateStyles8.Disabled = styles8;
            stateStyles8.Hover = styles8;
            stateStyles8.Normal = styles8;
            this.GoBackButton.ImageStyles = stateStyles8;
            this.GoBackButton.Name = "GoBackButton";
            this.BackTooltip.SetToolTip(this.GoBackButton, resources.GetString("GoBackButton.ToolTip"));
            this.ForwardTooltip.SetToolTip(this.GoBackButton, resources.GetString("GoBackButton.ToolTip1"));
            this.UpTooltip.SetToolTip(this.GoBackButton, resources.GetString("GoBackButton.ToolTip2"));
            this.GoBackButton.Click += new System.EventHandler(this.GoBackButton_Click);
            // 
            // GoForwardButton
            // 
            resources.ApplyResources(this.GoForwardButton, "GoForwardButton");
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
            this.GoForwardButton.Name = "GoForwardButton";
            this.BackTooltip.SetToolTip(this.GoForwardButton, resources.GetString("GoForwardButton.ToolTip"));
            this.ForwardTooltip.SetToolTip(this.GoForwardButton, resources.GetString("GoForwardButton.ToolTip1"));
            this.UpTooltip.SetToolTip(this.GoForwardButton, resources.GetString("GoForwardButton.ToolTip2"));
            this.GoForwardButton.Click += new System.EventHandler(this.GoForwardButton_Click);
            // 
            // GoUpButton
            // 
            resources.ApplyResources(this.GoUpButton, "GoUpButton");
            styles11.FillColor = System.Drawing.Color.Empty;
            styles11.IsFillEnabled = false;
            styles11.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles11.StrokeColor = System.Drawing.Color.Empty;
            styles11.StrokeWidth = 0;
            stateStyles11.Disabled = styles11;
            stateStyles11.Hover = styles11;
            stateStyles11.Normal = styles11;
            this.GoUpButton.ButtonStyles = stateStyles11;
            this.GoUpButton.Image = null;
            styles12.FillColor = System.Drawing.Color.Black;
            styles12.IsFillEnabled = true;
            styles12.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles12.StrokeColor = System.Drawing.Color.Empty;
            styles12.StrokeWidth = 0;
            stateStyles12.Disabled = styles12;
            stateStyles12.Hover = styles12;
            stateStyles12.Normal = styles12;
            this.GoUpButton.ImageStyles = stateStyles12;
            this.GoUpButton.Name = "GoUpButton";
            this.BackTooltip.SetToolTip(this.GoUpButton, resources.GetString("GoUpButton.ToolTip"));
            this.ForwardTooltip.SetToolTip(this.GoUpButton, resources.GetString("GoUpButton.ToolTip1"));
            this.UpTooltip.SetToolTip(this.GoUpButton, resources.GetString("GoUpButton.ToolTip2"));
            this.GoUpButton.Click += new System.EventHandler(this.GoUpButton_Click);
            // 
            // ForwardTooltip
            // 
            this.ForwardTooltip.ToolTipTitle = "Následující dotaz";
            // 
            // UpTooltip
            // 
            this.UpTooltip.ToolTipTitle = "Rodičovský adresář";
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
            this.UpTooltip.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.BackTooltip.SetToolTip(this, resources.GetString("$this.ToolTip1"));
            this.ForwardTooltip.SetToolTip(this, resources.GetString("$this.ToolTip2"));
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
