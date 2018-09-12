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
            this.HistoryComboBox = new System.Windows.Forms.ComboBox();
            this.GoBackButton = new Viewer.UI.Forms.IconButton();
            this.GoForwardButton = new Viewer.UI.Forms.IconButton();
            this.GoUpButton = new Viewer.UI.Forms.IconButton();
            this.BackTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.ForwardTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.UpTooltip = new System.Windows.Forms.ToolTip(this.components);
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
            // GoBackButton
            // 
            this.GoBackButton.Icon = global::Viewer.Properties.Resources.LeftArrow;
            this.GoBackButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.GoBackButton.IconDisabledColor = System.Drawing.Color.DarkGray;
            this.GoBackButton.IconSize = new System.Drawing.Size(18, 18);
            this.GoBackButton.Location = new System.Drawing.Point(4, 4);
            this.GoBackButton.Name = "GoBackButton";
            this.GoBackButton.Size = new System.Drawing.Size(21, 21);
            this.GoBackButton.TabIndex = 1;
            this.GoBackButton.Text = "Go Back";
            this.BackTooltip.SetToolTip(this.GoBackButton, "Alt + Left arrow");
            this.GoBackButton.Click += new System.EventHandler(this.GoBackButton_Click);
            // 
            // GoForwardButton
            // 
            this.GoForwardButton.Icon = global::Viewer.Properties.Resources.RightArrow;
            this.GoForwardButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.GoForwardButton.IconDisabledColor = System.Drawing.Color.DarkGray;
            this.GoForwardButton.IconSize = new System.Drawing.Size(18, 18);
            this.GoForwardButton.Location = new System.Drawing.Point(29, 3);
            this.GoForwardButton.Name = "GoForwardButton";
            this.GoForwardButton.Size = new System.Drawing.Size(21, 21);
            this.GoForwardButton.TabIndex = 2;
            this.GoForwardButton.Text = "Go forward";
            this.ForwardTooltip.SetToolTip(this.GoForwardButton, "Alt + Right arrow");
            this.GoForwardButton.Click += new System.EventHandler(this.GoForwardButton_Click);
            // 
            // GoUpButton
            // 
            this.GoUpButton.Icon = global::Viewer.Properties.Resources.UpArrow;
            this.GoUpButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.GoUpButton.IconDisabledColor = System.Drawing.Color.White;
            this.GoUpButton.IconSize = new System.Drawing.Size(16, 16);
            this.GoUpButton.Location = new System.Drawing.Point(55, 4);
            this.GoUpButton.Name = "GoUpButton";
            this.GoUpButton.Size = new System.Drawing.Size(21, 21);
            this.GoUpButton.TabIndex = 3;
            this.GoUpButton.Text = "iconButton1";
            this.UpTooltip.SetToolTip(this.GoUpButton, "Alt + Up arrow");
            this.GoUpButton.Click += new System.EventHandler(this.GoUpButton_Click);
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
        private Forms.IconButton GoBackButton;
        private Forms.IconButton GoForwardButton;
        private Forms.IconButton GoUpButton;
        private System.Windows.Forms.ToolTip ForwardTooltip;
        private System.Windows.Forms.ToolTip UpTooltip;
        private System.Windows.Forms.ToolTip BackTooltip;
    }
}
