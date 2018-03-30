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
        /// <summary>
        /// Shrink given rectangle by <paramref name="size"/> units from each side.
        /// That is, width and height of the rectangle will be 2 * <paramref name="size"/> less.
        /// This will also move the rectangle by <paramref name="size"/> units in each axis (i.e. its center won't change)
        /// </summary>
        /// <param name="bounds">Rectangle to shrink</param>
        /// <param name="size">Determines how much the rectangle will be shrinked</param>
        /// <returns>
        ///     Shrinked rectangle.
        ///     If the <paramref name="size"/> is greater than width or height, size of the 
        ///     resulting rectangle in this axis will be 0 and its position will be moved to 
        ///     the center.
        /// </returns>
        public static Rectangle Shrink(this Rectangle bounds, int size)
        {
            var x = bounds.X + size;
            var y = bounds.Y + size;
            var width = bounds.Width - size * 2;
            var height = bounds.Height - size * 2;
            if (width < 0)
            {
                width = 0;
                x = bounds.X + bounds.Width / 2;
            }

            if (height < 0)
            {
                height = 0;
                y = bounds.Y + bounds.Height / 2;
            }
            return new Rectangle(x, y, width, height);
        }
    }
}
