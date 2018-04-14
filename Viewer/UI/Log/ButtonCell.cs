using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI.Log
{
    public class ButtonCell : DataGridViewButtonCell
    {
        public event EventHandler<DataGridViewCellEventArgs> Click;

        /// <summary>
        /// true iff the button cell is enabled (i.e. drawn)
        /// </summary>
        public bool Enabled { get; set; } = true;

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates elementState, object value, object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            if (Enabled)
            {
                base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText,
                    cellStyle, advancedBorderStyle, paintParts);
            }
            else
            {
                if ((paintParts & DataGridViewPaintParts.Background) != 0)
                {
                    using (var brush = new SolidBrush(cellStyle.BackColor))
                    {
                        graphics.FillRectangle(brush, cellBounds);
                    }
                }
                PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);
            }
        }

        protected override void OnClick(DataGridViewCellEventArgs e)
        {
            base.OnClick(e);
            Click?.Invoke(this, e);
        }
    }
}
