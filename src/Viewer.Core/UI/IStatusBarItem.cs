using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.Core.UI
{
    /// <inheritdoc/>
    /// <summary>
    /// Label in the application status bar. Disosing this item will remove it from the status bar.
    /// You must not use any property or method after calling Dispose.
    /// </summary>
    public interface IStatusBarItem : IDisposable
    {
        /// <summary>
        /// Text shown to the user in the status bar.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Icon of this status bar item shown to the left of it. It can be null
        /// </summary>
        Image Image { get; set; }

        /// <summary>
        /// Alignment of this item (left or right).
        /// </summary>
        ToolStripItemAlignment Alignment { get; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Slider drawn in the application status bar.
    /// </summary>
    public interface IStatusBarSlider : IStatusBarItem
    {
        /// <summary>
        /// Event occurs whenever <see cref="Value"/> changes.
        /// </summary>
        event EventHandler ValueChanged;

        /// <summary>
        /// Slider value in the [0, 1] range. If you set a value outside of this range,
        /// <see cref="ArgumentOutOfRangeException"/> will be thrown.
        /// </summary>
        double Value { get; set; }
    }
}
