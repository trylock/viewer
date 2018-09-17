using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Core
{
    public static class RectangleExtensions
    {
        /// <summary>
        /// Compute area of given rectangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns>Area of <paramref name="rectangle"/></returns>
        public static int Area(this Rectangle rectangle)
        {
            return rectangle.Size.Width * rectangle.Size.Height;
        }

        /// <summary>
        /// Transform <paramref name="rectangle"/> so that it is inside <paramref name="container"/>.
        /// The size of <paramref name="rectangle"/> will only be modified if
        /// <paramref name="container"/> is smaller than <paramref name="rectangle"/> is some axis.
        /// </summary>
        /// <param name="rectangle">Rectangle to transform</param>
        /// <param name="container">
        /// Container inside of which <paramref name="rectangle"/> should be placed.
        /// </param>
        /// <returns>Transformed rectangle</returns>
        public static Rectangle EnsureInside(this Rectangle rectangle, Rectangle container)
        {
            // if container is smaller than rectangle, modify rectangle size now
            rectangle.Size = new Size(
                Math.Min(rectangle.Width, container.Width),
                Math.Min(rectangle.Height, container.Height)
            );

            var deltaX = container.Right - rectangle.Right;
            if (deltaX >= 0)
            {
                // if the difference is negative, the rectangle is in container in this dimension
                deltaX = Math.Max(container.Left - rectangle.Left, 0);
            }

            var deltaY = container.Bottom - rectangle.Bottom;
            if (deltaY >= 0)
            {
                // if the difference is negative, the rectangle is in container in this dimension
                deltaY = Math.Max(container.Top - rectangle.Top, 0);
            }

            return new Rectangle(
                rectangle.X + deltaX, 
                rectangle.Y + deltaY,
                rectangle.Width, 
                rectangle.Height);
        }
    }
}
