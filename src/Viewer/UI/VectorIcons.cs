using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI
{
    /// <summary>
    /// Vector icons used by the application.
    /// </summary>
    internal static class VectorIcons
    {
        public static GraphicsPath GoBackIcon { get; }

        public static GraphicsPath GoForwardIcon { get; }

        public static GraphicsPath ArrowUpIcon { get; }

        static VectorIcons()
        {
            GoBackIcon = new GraphicsPath();
            GoBackIcon.AddLine(new Point(0, 8), new Point(8, 16));
            GoBackIcon.AddLine(new Point(0, 8), new Point(8, 0));

            GoForwardIcon = new GraphicsPath();
            GoForwardIcon.AddLine(new Point(0, 16), new Point(8, 8));
            GoForwardIcon.AddLine(new Point(8, 8), new Point(0, 0));

            ArrowUpIcon = new GraphicsPath();
            ArrowUpIcon.AddLine(new Point(1, 7), new Point(8, 0));
            ArrowUpIcon.AddLine(new Point(8, 0), new Point(15, 7));
            ArrowUpIcon.AddLine(new Point(8, 0), new Point(8, 16));
        }
    }
}
