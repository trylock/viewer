using ScintillaNET;

namespace Viewer.UI.QueryEditor
{
    partial class QueryEditorView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QueryEditorView));
            this.OpenDialog = new System.Windows.Forms.OpenFileDialog();
            this.SaveDialog = new System.Windows.Forms.SaveFileDialog();
            this.EditorToolStrip = new System.Windows.Forms.ToolStrip();
            this.OpenButton = new System.Windows.Forms.ToolStripButton();
            this.QueryViewsDropDown = new System.Windows.Forms.ToolStripDropDownButton();
            this.SaveButton = new System.Windows.Forms.ToolStripButton();
            this.RunButton = new System.Windows.Forms.ToolStripButton();
            this.PollTimer = new System.Windows.Forms.Timer(this.components);
            this.QueryTextBox = new Viewer.UI.QueryEditor.EditorControl();
            this.EditorToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // OpenDialog
            // 
            this.OpenDialog.DefaultExt = "vql";
            this.OpenDialog.Filter = "Query Views (*.vql) |*.vql|All files (*.*)|*.*";
            // 
            // SaveDialog
            // 
            this.SaveDialog.DefaultExt = "vql";
            this.SaveDialog.Filter = "Query Views (*.vql) |*.vql|All files (*.*)|*.*";
            // 
            // EditorToolStrip
            // 
            this.EditorToolStrip.AllowItemReorder = true;
            this.EditorToolStrip.BackColor = System.Drawing.SystemColors.Control;
            this.EditorToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.EditorToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenButton,
            this.QueryViewsDropDown,
            this.SaveButton,
            this.RunButton});
            this.EditorToolStrip.Location = new System.Drawing.Point(0, 0);
            this.EditorToolStrip.Name = "EditorToolStrip";
            this.EditorToolStrip.Padding = new System.Windows.Forms.Padding(3);
            this.EditorToolStrip.Size = new System.Drawing.Size(779, 29);
            this.EditorToolStrip.TabIndex = 1;
            this.EditorToolStrip.Text = "toolStrip1";
            // 
            // OpenButton
            // 
            this.OpenButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.OpenButton.Image = global::Viewer.Properties.Resources.Open;
            this.OpenButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.OpenButton.Name = "OpenButton";
            this.OpenButton.Size = new System.Drawing.Size(23, 20);
            this.OpenButton.Text = "Open query";
            this.OpenButton.ToolTipText = "Open query (Ctrl + O)";
            this.OpenButton.Click += new System.EventHandler(this.OpenButton_Click);
            // 
            // QueryViewsDropDown
            // 
            this.QueryViewsDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.QueryViewsDropDown.Image = global::Viewer.Properties.Resources.View;
            this.QueryViewsDropDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.QueryViewsDropDown.Name = "QueryViewsDropDown";
            this.QueryViewsDropDown.Size = new System.Drawing.Size(29, 20);
            this.QueryViewsDropDown.Text = "Open query view";
            // 
            // SaveButton
            // 
            this.SaveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SaveButton.Image = global::Viewer.Properties.Resources.Save;
            this.SaveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(23, 20);
            this.SaveButton.Text = "Save query";
            this.SaveButton.ToolTipText = "Save query (Ctrl + S)";
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // RunButton
            // 
            this.RunButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.RunButton.Image = global::Viewer.Properties.Resources.Start;
            this.RunButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(23, 20);
            this.RunButton.Text = "Run query";
            this.RunButton.ToolTipText = "Run query (F5, Ctrl + Enter)";
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // PollTimer
            // 
            this.PollTimer.Enabled = true;
            this.PollTimer.Interval = 40;
            this.PollTimer.Tick += new System.EventHandler(this.PollTimer_Tick);
            // 
            // QueryTextBox
            // 
            this.QueryTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.QueryTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.QueryTextBox.Location = new System.Drawing.Point(0, 28);
            this.QueryTextBox.Name = "QueryTextBox";
            this.QueryTextBox.ScrollWidth = 1;
            this.QueryTextBox.Size = new System.Drawing.Size(779, 365);
            this.QueryTextBox.TabIndex = 0;
            this.QueryTextBox.UpdateUI += new System.EventHandler<ScintillaNET.UpdateUIEventArgs>(this.QueryTextBox_UpdateUI);
            this.QueryTextBox.TextChanged += new System.EventHandler(this.QueryTextBox_TextChanged);
            this.QueryTextBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.QueryTextBox_DragDrop);
            this.QueryTextBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.QueryTextBox_DragEnter);
            this.QueryTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.QueryTextBox_KeyDown);
            // 
            // QueryEditorView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(779, 393);
            this.Controls.Add(this.EditorToolStrip);
            this.Controls.Add(this.QueryTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "QueryEditorView";
            this.Text = "Query";
            this.EditorToolStrip.ResumeLayout(false);
            this.EditorToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.OpenFileDialog OpenDialog;
        private System.Windows.Forms.SaveFileDialog SaveDialog;
        private EditorControl QueryTextBox;
        private System.Windows.Forms.ToolStrip EditorToolStrip;
        private System.Windows.Forms.ToolStripButton OpenButton;
        private System.Windows.Forms.ToolStripButton SaveButton;
        private System.Windows.Forms.ToolStripButton RunButton;
        private System.Windows.Forms.ToolStripDropDownButton QueryViewsDropDown;
        private System.Windows.Forms.Timer PollTimer;
    }
}
