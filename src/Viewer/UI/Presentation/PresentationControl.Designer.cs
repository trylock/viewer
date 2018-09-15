using Viewer.UI.Forms;

namespace Viewer.UI.Presentation
{
    partial class PresentationControl
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
            Viewer.UI.Forms.StateStyles stateStylesGroup3 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles3 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup4 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles4 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup5 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles5 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup6 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles6 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup7 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles7 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup8 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles8 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup1 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles1 = new Viewer.UI.Forms.Styles();
            Viewer.UI.Forms.StateStyles stateStylesGroup2 = new Viewer.UI.Forms.StateStyles();
            Viewer.UI.Forms.Styles styles2 = new Viewer.UI.Forms.Styles();
            this.NextButton = new System.Windows.Forms.Button();
            this.PrevButton = new System.Windows.Forms.Button();
            this.ControlPanel = new System.Windows.Forms.Panel();
            this.ZoomInButton = new Viewer.UI.Forms.VectorButton();
            this.ZoomOutButton = new Viewer.UI.Forms.VectorButton();
            this.PlayPauseButton = new Viewer.UI.Forms.VectorButton();
            this.MaxDelayLabel = new System.Windows.Forms.Label();
            this.MinDelayLabel = new System.Windows.Forms.Label();
            this.SpeedTrackBar = new System.Windows.Forms.TrackBar();
            this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.TogglePlayToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ZoomOutToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ZoomInToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ToggleFullscreenToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.NextImageToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.PrevImageToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.Preview = new Viewer.UI.Presentation.PreviewControl();
            this.ToggleFullscreenButton = new Viewer.UI.Forms.VectorButton();
            this.ControlPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // NextButton
            // 
            this.NextButton.BackColor = System.Drawing.Color.Transparent;
            this.NextButton.BackgroundImage = global::Viewer.Properties.Resources.RightArrowIcon;
            this.NextButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.NextButton.FlatAppearance.BorderSize = 0;
            this.NextButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.NextButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.NextButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.NextButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.NextButton.ForeColor = System.Drawing.Color.White;
            this.NextButton.Location = new System.Drawing.Point(430, 220);
            this.NextButton.Margin = new System.Windows.Forms.Padding(2);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(60, 60);
            this.NextButton.TabIndex = 3;
            this.NextButton.TabStop = false;
            this.NextImageToolTip.SetToolTip(this.NextButton, "Right Arrow, Scroll Up");
            this.NextButton.UseVisualStyleBackColor = false;
            this.NextButton.Click += new System.EventHandler(this.NextButton_Click);
            // 
            // PrevButton
            // 
            this.PrevButton.BackColor = System.Drawing.Color.Transparent;
            this.PrevButton.BackgroundImage = global::Viewer.Properties.Resources.LeftArrowIcon;
            this.PrevButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.PrevButton.FlatAppearance.BorderSize = 0;
            this.PrevButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.PrevButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.PrevButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PrevButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.PrevButton.ForeColor = System.Drawing.Color.White;
            this.PrevButton.Location = new System.Drawing.Point(10, 220);
            this.PrevButton.Margin = new System.Windows.Forms.Padding(2);
            this.PrevButton.Name = "PrevButton";
            this.PrevButton.Size = new System.Drawing.Size(60, 60);
            this.PrevButton.TabIndex = 2;
            this.PrevButton.TabStop = false;
            this.PrevImageToolTip.SetToolTip(this.PrevButton, "Left Arrow, Scroll Down");
            this.PrevButton.UseVisualStyleBackColor = false;
            this.PrevButton.Click += new System.EventHandler(this.PrevButton_Click);
            // 
            // ControlPanel
            // 
            this.ControlPanel.BackColor = System.Drawing.SystemColors.Control;
            this.ControlPanel.Controls.Add(this.ToggleFullscreenButton);
            this.ControlPanel.Controls.Add(this.ZoomInButton);
            this.ControlPanel.Controls.Add(this.ZoomOutButton);
            this.ControlPanel.Controls.Add(this.PlayPauseButton);
            this.ControlPanel.Controls.Add(this.MaxDelayLabel);
            this.ControlPanel.Controls.Add(this.MinDelayLabel);
            this.ControlPanel.Controls.Add(this.SpeedTrackBar);
            this.ControlPanel.Location = new System.Drawing.Point(119, 457);
            this.ControlPanel.Margin = new System.Windows.Forms.Padding(2);
            this.ControlPanel.Name = "ControlPanel";
            this.ControlPanel.Size = new System.Drawing.Size(229, 41);
            this.ControlPanel.TabIndex = 4;
            // 
            // ZoomInButton
            // 
            styles3.FillColor = System.Drawing.Color.Empty;
            styles3.IsFillEnabled = false;
            styles3.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles3.StrokeColor = System.Drawing.Color.Empty;
            styles3.StrokeWidth = 0;
            stateStylesGroup3.Disabled = styles3;
            stateStylesGroup3.Hover = styles3;
            stateStylesGroup3.Normal = styles3;
            this.ZoomInButton.ButtonStyles = stateStylesGroup3;
            this.ZoomInButton.Image = null;
            styles4.FillColor = System.Drawing.Color.Empty;
            styles4.IsFillEnabled = false;
            styles4.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles4.StrokeColor = System.Drawing.Color.Black;
            styles4.StrokeWidth = 1;
            stateStylesGroup4.Disabled = styles4;
            stateStylesGroup4.Hover = styles4;
            stateStylesGroup4.Normal = styles4;
            this.ZoomInButton.ImageStyles = stateStylesGroup4;
            this.ZoomInButton.Location = new System.Drawing.Point(178, 0);
            this.ZoomInButton.Name = "ZoomInButton";
            this.ZoomInButton.Padding = new System.Windows.Forms.Padding(6);
            this.ZoomInButton.Size = new System.Drawing.Size(24, 41);
            this.ZoomInButton.TabIndex = 10;
            this.ZoomInButton.Text = "vectorButton1";
            this.ZoomInToolTip.SetToolTip(this.ZoomInButton, "Ctrl + Mouse Wheel Up");
            this.ZoomInButton.Click += new System.EventHandler(this.ZoomInButton_Click);
            // 
            // ZoomOutButton
            // 
            styles5.FillColor = System.Drawing.Color.Empty;
            styles5.IsFillEnabled = false;
            styles5.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles5.StrokeColor = System.Drawing.Color.Empty;
            styles5.StrokeWidth = 0;
            stateStylesGroup5.Disabled = styles5;
            stateStylesGroup5.Hover = styles5;
            stateStylesGroup5.Normal = styles5;
            this.ZoomOutButton.ButtonStyles = stateStylesGroup5;
            this.ZoomOutButton.Image = null;
            styles6.FillColor = System.Drawing.Color.Empty;
            styles6.IsFillEnabled = false;
            styles6.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles6.StrokeColor = System.Drawing.Color.Black;
            styles6.StrokeWidth = 1;
            stateStylesGroup6.Disabled = styles6;
            stateStylesGroup6.Hover = styles6;
            stateStylesGroup6.Normal = styles6;
            this.ZoomOutButton.ImageStyles = stateStylesGroup6;
            this.ZoomOutButton.Location = new System.Drawing.Point(152, 0);
            this.ZoomOutButton.Name = "ZoomOutButton";
            this.ZoomOutButton.Padding = new System.Windows.Forms.Padding(6);
            this.ZoomOutButton.Size = new System.Drawing.Size(24, 41);
            this.ZoomOutButton.TabIndex = 9;
            this.ZoomOutButton.Text = "vectorButton1";
            this.ZoomOutToolTip.SetToolTip(this.ZoomOutButton, "Ctrl + Mouse Wheel Down");
            this.ZoomOutButton.Click += new System.EventHandler(this.ZoomOutButton_Click);
            // 
            // PlayPauseButton
            // 
            styles7.FillColor = System.Drawing.Color.Empty;
            styles7.IsFillEnabled = false;
            styles7.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles7.StrokeColor = System.Drawing.Color.Empty;
            styles7.StrokeWidth = 0;
            stateStylesGroup7.Disabled = styles7;
            stateStylesGroup7.Hover = styles7;
            stateStylesGroup7.Normal = styles7;
            this.PlayPauseButton.ButtonStyles = stateStylesGroup7;
            this.PlayPauseButton.Image = null;
            styles8.FillColor = System.Drawing.Color.Empty;
            styles8.IsFillEnabled = false;
            styles8.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles8.StrokeColor = System.Drawing.Color.Black;
            styles8.StrokeWidth = 1;
            stateStylesGroup8.Disabled = styles8;
            stateStylesGroup8.Hover = styles8;
            stateStylesGroup8.Normal = styles8;
            this.PlayPauseButton.ImageStyles = stateStylesGroup8;
            this.PlayPauseButton.Location = new System.Drawing.Point(126, 0);
            this.PlayPauseButton.Name = "PlayPauseButton";
            this.PlayPauseButton.Padding = new System.Windows.Forms.Padding(6);
            this.PlayPauseButton.Size = new System.Drawing.Size(24, 41);
            this.PlayPauseButton.TabIndex = 8;
            this.PlayPauseButton.Text = "vectorButton1";
            this.TogglePlayToolTip.SetToolTip(this.PlayPauseButton, "Space");
            this.PlayPauseButton.Click += new System.EventHandler(this.PlayPauseButton_Click);
            // 
            // MaxDelayLabel
            // 
            this.MaxDelayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MaxDelayLabel.AutoSize = true;
            this.MaxDelayLabel.BackColor = System.Drawing.SystemColors.Control;
            this.MaxDelayLabel.Location = new System.Drawing.Point(96, 23);
            this.MaxDelayLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.MaxDelayLabel.Name = "MaxDelayLabel";
            this.MaxDelayLabel.Size = new System.Drawing.Size(24, 13);
            this.MaxDelayLabel.TabIndex = 3;
            this.MaxDelayLabel.Text = "10s";
            // 
            // MinDelayLabel
            // 
            this.MinDelayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MinDelayLabel.AutoSize = true;
            this.MinDelayLabel.BackColor = System.Drawing.SystemColors.Control;
            this.MinDelayLabel.Location = new System.Drawing.Point(2, 23);
            this.MinDelayLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.MinDelayLabel.Name = "MinDelayLabel";
            this.MinDelayLabel.Size = new System.Drawing.Size(18, 13);
            this.MinDelayLabel.TabIndex = 2;
            this.MinDelayLabel.Text = "1s";
            // 
            // SpeedTrackBar
            // 
            this.SpeedTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SpeedTrackBar.Location = new System.Drawing.Point(2, 2);
            this.SpeedTrackBar.Margin = new System.Windows.Forms.Padding(2);
            this.SpeedTrackBar.Minimum = 1;
            this.SpeedTrackBar.Name = "SpeedTrackBar";
            this.SpeedTrackBar.Size = new System.Drawing.Size(118, 45);
            this.SpeedTrackBar.TabIndex = 1;
            this.SpeedTrackBar.Value = 1;
            // 
            // UpdateTimer
            // 
            this.UpdateTimer.Enabled = true;
            this.UpdateTimer.Interval = 16;
            this.UpdateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
            // 
            // TogglePlayToolTip
            // 
            this.TogglePlayToolTip.ToolTipTitle = "Toggle Play";
            // 
            // ZoomOutToolTip
            // 
            this.ZoomOutToolTip.ToolTipTitle = "Zoom Out";
            // 
            // ZoomInToolTip
            // 
            this.ZoomInToolTip.ToolTipTitle = "Zoom In";
            // 
            // ToggleFullscreenToolTip
            // 
            this.ToggleFullscreenToolTip.ToolTipTitle = "Toggle Fullscreen";
            // 
            // NextImageToolTip
            // 
            this.NextImageToolTip.ToolTipTitle = "Next";
            // 
            // PrevImageToolTip
            // 
            this.PrevImageToolTip.ToolTipTitle = "Prev";
            // 
            // Preview
            // 
            this.Preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Preview.Location = new System.Drawing.Point(0, 0);
            this.Preview.Name = "Preview";
            this.Preview.Picture = null;
            this.Preview.Size = new System.Drawing.Size(500, 500);
            this.Preview.TabIndex = 5;
            this.Preview.Text = "previewControl1";
            this.Preview.Zoom = 1D;
            this.Preview.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.Preview_MouseDoubleClick);
            this.Preview.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Preview_MouseDown);
            this.Preview.MouseLeave += new System.EventHandler(this.HideControlsHandler);
            this.Preview.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Preview_MouseMove);
            this.Preview.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Preview_MouseUp);
            this.Preview.Resize += new System.EventHandler(this.Preview_Resize);
            // 
            // ToggleFullscreenButton
            // 
            styles1.FillColor = System.Drawing.Color.Empty;
            styles1.IsFillEnabled = false;
            styles1.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles1.StrokeColor = System.Drawing.Color.Empty;
            styles1.StrokeWidth = 0;
            stateStylesGroup1.Disabled = styles1;
            stateStylesGroup1.Hover = styles1;
            stateStylesGroup1.Normal = styles1;
            this.ToggleFullscreenButton.ButtonStyles = stateStylesGroup1;
            this.ToggleFullscreenButton.Image = null;
            styles2.FillColor = System.Drawing.Color.Empty;
            styles2.IsFillEnabled = false;
            styles2.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            styles2.StrokeColor = System.Drawing.Color.Black;
            styles2.StrokeWidth = 1;
            stateStylesGroup2.Disabled = styles2;
            stateStylesGroup2.Hover = styles2;
            stateStylesGroup2.Normal = styles2;
            this.ToggleFullscreenButton.ImageStyles = stateStylesGroup2;
            this.ToggleFullscreenButton.Location = new System.Drawing.Point(202, -1);
            this.ToggleFullscreenButton.Name = "ToggleFullscreenButton";
            this.ToggleFullscreenButton.Padding = new System.Windows.Forms.Padding(6);
            this.ToggleFullscreenButton.Size = new System.Drawing.Size(24, 41);
            this.ToggleFullscreenButton.TabIndex = 11;
            this.ToggleFullscreenButton.Text = "vectorButton1";
            this.ToggleFullscreenToolTip.SetToolTip(this.ToggleFullscreenButton, "F5, F, double click");
            this.ToggleFullscreenButton.Click += new System.EventHandler(this.ToggleFullscreenButton_Click);
            // 
            // PresentationControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.PrevButton);
            this.Controls.Add(this.NextButton);
            this.Controls.Add(this.ControlPanel);
            this.Controls.Add(this.Preview);
            this.DoubleBuffered = true;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "PresentationControl";
            this.Size = new System.Drawing.Size(500, 500);
            this.ControlPanel.ResumeLayout(false);
            this.ControlPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedTrackBar)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button NextButton;
        private System.Windows.Forms.Button PrevButton;
        private System.Windows.Forms.Panel ControlPanel;
        private System.Windows.Forms.TrackBar SpeedTrackBar;
        private System.Windows.Forms.Label MinDelayLabel;
        private System.Windows.Forms.Label MaxDelayLabel;
        private System.Windows.Forms.Timer UpdateTimer;
        private System.Windows.Forms.ToolTip ToggleFullscreenToolTip;
        private System.Windows.Forms.ToolTip ZoomInToolTip;
        private System.Windows.Forms.ToolTip ZoomOutToolTip;
        private System.Windows.Forms.ToolTip TogglePlayToolTip;
        private PreviewControl Preview;
        private System.Windows.Forms.ToolTip NextImageToolTip;
        private System.Windows.Forms.ToolTip PrevImageToolTip;
        private VectorButton PlayPauseButton;
        private VectorButton ZoomOutButton;
        private VectorButton ZoomInButton;
        private VectorButton ToggleFullscreenButton;
    }
}
