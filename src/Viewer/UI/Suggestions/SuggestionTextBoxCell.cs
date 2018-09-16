using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI.Suggestions
{
    /// <summary>
    /// DataGridView cell which can be used with <see cref="SuggestionView"/>.
    /// </summary>
    internal class SuggestionTextBoxCell : DataGridViewTextBoxCell
    {
        public override Type EditType => typeof(SuggestionTextBox);
    }

    internal class SuggestionTextBox : TextBox, IDataGridViewEditingControl
    {
        public DataGridView EditingControlDataGridView { get; set; }

        /// <summary>
        /// Suggestion view used by this textbox
        /// </summary>
        public SuggestionView Suggestions { get; }

        public object EditingControlFormattedValue
        {
            get => Text;
            set => Text = value.ToString();
        }

        public int EditingControlRowIndex { get; set; }
        public bool EditingControlValueChanged { get; set; }
        public Cursor EditingPanelCursor { get; }
        public bool RepositionEditingControlOnValueChange => false;

        public SuggestionTextBox()
        {
            Suggestions = new SuggestionView(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Suggestions?.Dispose();
            }
            base.Dispose(disposing);
        }

        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle style)
        {
            Font = style.Font;
            ForeColor = style.ForeColor;
            BackColor = style.BackColor;
        }

        public bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
        {
            if (Suggestions.Visible)
            {
                switch (key & Keys.KeyCode)
                {
                    case Keys.Left:
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Right:
                    case Keys.Escape:
                    case Keys.Home:
                    case Keys.End:
                    case Keys.PageDown:
                    case Keys.PageUp:
                        return true;
                }
            }
            else
            {
                switch (key & Keys.KeyCode)
                {
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Home:
                    case Keys.End:
                    case Keys.PageDown:
                    case Keys.PageUp:
                        return true;
                }
            }

            return !dataGridViewWantsInputKey;
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
        }

        protected override bool ProcessCmdKey(ref Message m, Keys key)
        {
            // Trigger the key down event for the enter and the tab keys so that user can accept
            // suggestions from the suggesion control.
            if (key == Keys.Enter || key == Keys.Tab)
            {
                var eventArgs = new KeyEventArgs(key);
                OnKeyDown(eventArgs);
            }
            return base.ProcessCmdKey(ref m, key);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            EditingControlValueChanged = true;
            EditingControlDataGridView.NotifyCurrentCellDirty(true);
            base.OnTextChanged(e);
        }
    }
}
