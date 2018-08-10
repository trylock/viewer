using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;

namespace Viewer.UI.Forms
{
    /// <inheritdoc />
    /// <summary>
    /// DataGridView date time cell
    /// </summary>
    public class DateTimeCell : DataGridViewTextBoxCell
    {
        public override Type EditType => typeof(DateTimeEditingControl);
        public override Type ValueType => typeof(DateTime);
        public override object DefaultNewRowValue => DateTime.Now;
        
        public DateTimeCell()
        {
            var format = CultureInfo.CurrentCulture.DateTimeFormat;
            Style.Format = format.ShortDatePattern + " " + format.ShortTimePattern;
        }

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

            var editingControl = DataGridView.EditingControl as DateTimeEditingControl;
            if (Value == null)
            {
                editingControl.Value = (DateTime) DefaultNewRowValue;
            }
            else
            {
                editingControl.Value = (DateTime) Value;
            }
        }
    }

    public class DateTimeEditingControl : DateTimePicker, IDataGridViewEditingControl
    {
        public DataGridView EditingControlDataGridView { get; set; }

        public object EditingControlFormattedValue
        {
            get => Value.ToShortDateString() + " " + Value.ToShortTimeString();
            set
            {
                if (value is string stringValue)
                {
                    try
                    {
                        Value = DateTime.Parse(stringValue, CultureInfo.CurrentCulture);
                    }
                    catch
                    {
                        Value = DateTime.Now;
                    }
                }
            }
        }

        public int EditingControlRowIndex { get; set; }
        public bool EditingControlValueChanged { get; set; }
        public Cursor EditingPanelCursor { get; }
        public bool RepositionEditingControlOnValueChange => false;

        public DateTimeEditingControl()
        {
            var format = CultureInfo.CurrentCulture.DateTimeFormat;
            Format = DateTimePickerFormat.Custom;
            CustomFormat = format.ShortDatePattern + " " + format.ShortTimePattern;
        }

        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle style)
        {
            Font = style.Font;
            CalendarForeColor = style.ForeColor;
            CalendarMonthBackground = style.BackColor;
        }

        public bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
        {
            switch (key & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageDown:
                case Keys.PageUp:
                    return true;
                default:
                    return !dataGridViewWantsInputKey;
            }
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
        }

        protected override void OnValueChanged(EventArgs e)
        {
            EditingControlValueChanged = true;
            EditingControlDataGridView.NotifyCurrentCellDirty(true);
            base.OnValueChanged(e);
        }
    }
}
