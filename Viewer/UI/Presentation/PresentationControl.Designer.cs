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
            this.NextButton = new System.Windows.Forms.Button();
            this.PrevButton = new System.Windows.Forms.Button();
            this.PlayPauseButton = new System.Windows.Forms.Button();
            this.ControlPanel = new System.Windows.Forms.Panel();
            this.MaxDelayLabel = new System.Windows.Forms.Label();
            this.MinDelayLabel = new System.Windows.Forms.Label();
            this.SpeedTrackBar = new System.Windows.Forms.TrackBar();
            this.HideCursorTimer = new System.Windows.Forms.Timer(this.components);
            this.ControlPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // NextButton
            // 
            this.NextButton.BackColor = System.Drawing.Color.Transparent;
            this.NextButton.BackgroundImage = global::Viewer.Properties.Resources.RightArrowIcon;
            this.NextButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.NextButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.NextButton.FlatAppearance.BorderSize = 0;
            this.NextButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.NextButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.NextButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.NextButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.NextButton.ForeColor = System.Drawing.Color.White;
            this.NextButton.Location = new System.Drawing.Point(720, 0);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(80, 500);
            this.NextButton.TabIndex = 3;
            this.NextButton.TabStop = false;
            this.NextButton.UseVisualStyleBackColor = false;
            this.NextButton.Click += new System.EventHandler(this.NextButton_Click);
            this.NextButton.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.NextButton.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // PrevButton
            // 
            this.PrevButton.BackColor = System.Drawing.Color.Transparent;
            this.PrevButton.BackgroundImage = global::Viewer.Properties.Resources.LeftArrowIcon;
            this.PrevButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.PrevButton.Dock = System.Windows.Forms.DockStyle.Left;
            this.PrevButton.FlatAppearance.BorderSize = 0;
            this.PrevButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.PrevButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.PrevButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PrevButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.PrevButton.ForeColor = System.Drawing.Color.White;
            this.PrevButton.Location = new System.Drawing.Point(0, 0);
            this.PrevButton.Name = "PrevButton";
            this.PrevButton.Size = new System.Drawing.Size(80, 500);
            this.PrevButton.TabIndex = 2;
            this.PrevButton.TabStop = false;
            this.PrevButton.UseVisualStyleBackColor = false;
            this.PrevButton.Click += new System.EventHandler(this.PrevButton_Click);
            this.PrevButton.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.PrevButton.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // PlayPauseButton
            // 
            this.PlayPauseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.PlayPauseButton.BackColor = System.Drawing.Color.Transparent;
            this.PlayPauseButton.BackgroundImage = global::Viewer.Properties.Resources.PlayIcon;
            this.PlayPauseButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.PlayPauseButton.FlatAppearance.BorderSize = 0;
            this.PlayPauseButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.PlayPauseButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.PlayPauseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PlayPauseButton.Location = new System.Drawing.Point(237, 0);
            this.PlayPauseButton.Name = "PlayPauseButton";
            this.PlayPauseButton.Size = new System.Drawing.Size(60, 60);
            this.PlayPauseButton.TabIndex = 0;
            this.PlayPauseButton.UseVisualStyleBackColor = false;
            this.PlayPauseButton.Click += new System.EventHandler(this.PausePlayButton_Click);
            this.PlayPauseButton.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.PlayPauseButton.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // ControlPanel
            // 
            this.ControlPanel.BackColor = System.Drawing.SystemColors.Control;
            this.ControlPanel.Controls.Add(this.MaxDelayLabel);
            this.ControlPanel.Controls.Add(this.MinDelayLabel);
            this.ControlPanel.Controls.Add(this.SpeedTrackBar);
            this.ControlPanel.Controls.Add(this.PlayPauseButton);
            this.ControlPanel.Location = new System.Drawing.Point(263, 439);
            this.ControlPanel.Name = "ControlPanel";
            this.ControlPanel.Size = new System.Drawing.Size(300, 58);
            this.ControlPanel.TabIndex = 4;
            this.ControlPanel.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // MaxDelayLabel
            // 
            this.MaxDelayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MaxDelayLabel.AutoSize = true;
            this.MaxDelayLabel.BackColor = System.Drawing.SystemColors.Control;
            this.MaxDelayLabel.Location = new System.Drawing.Point(200, 38);
            this.MaxDelayLabel.Name = "MaxDelayLabel";
            this.MaxDelayLabel.Size = new System.Drawing.Size(31, 17);
            this.MaxDelayLabel.TabIndex = 3;
            this.MaxDelayLabel.Text = "10s";
            this.MaxDelayLabel.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // MinDelayLabel
            // 
            this.MinDelayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MinDelayLabel.AutoSize = true;
            this.MinDelayLabel.BackColor = System.Drawing.SystemColors.Control;
            this.MinDelayLabel.Location = new System.Drawing.Point(3, 38);
            this.MinDelayLabel.Name = "MinDelayLabel";
            this.MinDelayLabel.Size = new System.Drawing.Size(23, 17);
            this.MinDelayLabel.TabIndex = 2;
            this.MinDelayLabel.Text = "1s";
            this.MinDelayLabel.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // SpeedTrackBar
            // 
            this.SpeedTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SpeedTrackBar.Location = new System.Drawing.Point(0, 5);
            this.SpeedTrackBar.Minimum = 1;
            this.SpeedTrackBar.Name = "SpeedTrackBar";
            this.SpeedTrackBar.Size = new System.Drawing.Size(231, 56);
            this.SpeedTrackBar.TabIndex = 1;
            this.SpeedTrackBar.Value = 1;
            this.SpeedTrackBar.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.SpeedTrackBar.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // HideCursorTimer
            // 
            this.HideCursorTimer.Enabled = true;
            this.HideCursorTimer.Tick += new System.EventHandler(this.HideCursorTimer_Tick);
            // 
            // PresentationControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.ControlPanel);
            this.Controls.Add(this.NextButton);
            this.Controls.Add(this.PrevButton);
            this.DoubleBuffered = true;
            this.Name = "PresentationControl";
            this.Size = new System.Drawing.Size(800, 500);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PresentationControl_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.PresentationControl_MouseDoubleClick);
            this.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PresentationControl_MouseMove);
            this.Resize += new System.EventHandler(this.PresentationControl_Resize);
            this.ControlPanel.ResumeLayout(false);
            this.ControlPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedTrackBar)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button NextButton;
        private System.Windows.Forms.Button PrevButton;
        private System.Windows.Forms.Button PlayPauseButton;
        private System.Windows.Forms.Panel ControlPanel;
        private System.Windows.Forms.TrackBar SpeedTrackBar;
        private System.Windows.Forms.Label MinDelayLabel;
        private System.Windows.Forms.Label MaxDelayLabel;
        private System.Windows.Forms.Timer HideCursorTimer;
    }
}
