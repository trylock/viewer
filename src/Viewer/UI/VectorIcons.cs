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
        public static GraphicsPath PlayIcon { get; }
        public static GraphicsPath PauseIcon { get; }
        public static GraphicsPath ZoomOutIcon { get; }
        public static GraphicsPath ZoomInIcon { get; }
        public static GraphicsPath FullscreenIcon { get; }
        public static GraphicsPath WindowedIcon { get; }

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

            PlayIcon = new GraphicsPath();
            PlayIcon.AddLine(new Point(0, 0), new Point(7, 4));
            PlayIcon.AddLine(new Point(7, 4), new Point(0, 8));
            PlayIcon.CloseFigure();

            PauseIcon = new GraphicsPath();
            PauseIcon.AddRectangle(new Rectangle(0, 0, 2, 8));
            PauseIcon.AddRectangle(new Rectangle(5, 0, 2, 8));

            var dist = (float) (4 + 4 / Math.Sqrt(2));
            ZoomOutIcon = new GraphicsPath();
            ZoomOutIcon.AddArc(new Rectangle(0, 0, 8, 8), 0, 360);
            ZoomOutIcon.StartFigure();
            ZoomOutIcon.AddLine(new Point(2, 4), new Point(6, 4));
            ZoomOutIcon.StartFigure();
            ZoomOutIcon.AddLine(new PointF(dist, dist), new PointF(10, 10));
            ZoomOutIcon.StartFigure();
            const float offset = 0.5f;
            ZoomOutIcon.AddLine(new PointF(dist - offset, dist + offset), new PointF(10 - offset, 10 + offset));

            ZoomInIcon = new GraphicsPath();
            ZoomInIcon.AddPath(ZoomOutIcon, false);
            ZoomInIcon.StartFigure();
            ZoomInIcon.AddLine(new Point(4, 2), new Point(4, 6));

            FullscreenIcon = new GraphicsPath();
            FullscreenIcon.AddLine(new Point(0, 4), new Point(0, 0));
            FullscreenIcon.AddLine(new Point(0, 0), new Point(4, 0));
            FullscreenIcon.StartFigure();
            FullscreenIcon.AddLine(new Point(8, 0), new Point(12, 0));
            FullscreenIcon.AddLine(new Point(12, 0), new Point(12, 4));
            FullscreenIcon.StartFigure();
            FullscreenIcon.AddLine(new Point(12, 8), new Point(12, 12));
            FullscreenIcon.AddLine(new Point(12, 12), new Point(8, 12));
            FullscreenIcon.StartFigure();
            FullscreenIcon.AddLine(new Point(4, 12), new Point(0, 12));
            FullscreenIcon.AddLine(new Point(0, 12), new Point(0, 8));

            WindowedIcon = new GraphicsPath();
            WindowedIcon.AddLine(new Point(0, 4), new Point(4, 4));
            WindowedIcon.AddLine(new Point(4, 4), new Point(4, 0));
            WindowedIcon.StartFigure();
            WindowedIcon.AddLine(new Point(8, 0), new Point(8, 4));
            WindowedIcon.AddLine(new Point(8, 4), new Point(12, 4));
            WindowedIcon.StartFigure();
            WindowedIcon.AddLine(new Point(12, 8), new Point(8, 8));
            WindowedIcon.AddLine(new Point(8, 8), new Point(8, 12));
            WindowedIcon.StartFigure();
            WindowedIcon.AddLine(new Point(4, 12), new Point(4, 8));
            WindowedIcon.AddLine(new Point(4, 8), new Point(0, 8));
        }
    }
}
