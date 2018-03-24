using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI
{
    public static class RectangleExtensions
    {
        public static Rectangle Shrink(this Rectangle bounds, int size)
        {
            return new Rectangle(bounds.X + size, bounds.Y + size, bounds.Width - size * 2, bounds.Height - size * 2);
        }
    }
}
