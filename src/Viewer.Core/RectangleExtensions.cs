using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Configuration;
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
        /// Move rectangle <paramref name="rectangle"/> to <paramref name="container"/>
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        private static Point MoveRectangleIn(Rectangle rectangle, Rectangle container)
        {
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

            return new Point(deltaX, deltaY);
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

            var delta = MoveRectangleIn(rectangle, container);
            return new Rectangle(
                rectangle.X + delta.X, 
                rectangle.Y + delta.Y,
                rectangle.Width, 
                rectangle.Height);
        }

        /// <summary>
        /// Dual method to <see cref="EnsureInside"/> but it transforms 
        /// <paramref name="container"/> instread of <paramref name="rectangle"/>
        /// </summary>
        /// <param name="container"></param>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Rectangle EnsureContains(this Rectangle container, Rectangle rectangle)
        {
            container.Size = new Size(
                Math.Max(container.Width, rectangle.Width),
                Math.Max(container.Height, rectangle.Height)
            );
            
            var delta = MoveRectangleIn(rectangle, container);
            return new Rectangle(
                container.X - delta.X,
                container.Y - delta.Y,
                container.Width,
                container.Height);
        }
        
        /// <summary>
        /// Compute set minus on 2 rectangles.
        /// </summary>
        /// <param name="self">Left hand side of the minus operator</param>
        /// <param name="other">Right hand size of the minus operator</param>
        /// <returns>
        /// Area which is in <paramref name="self"/> but not in <paramref name="other"/>.
        /// It is returned as list of rectangles. At most 4 rectangles will be returned.
        /// </returns>
        public static IEnumerable<Rectangle> Except(
            this Rectangle self, 
            Rectangle other)
        {
            // compute intersection
            var intersection = Rectangle.Intersect(self, other);
            if (intersection.IsEmpty || other.Size.IsEmpty)
            {
                yield return self;
                yield break;
            }

            var left = Rectangle.FromLTRB(self.Left, self.Top, intersection.Left, self.Bottom);
            var right = Rectangle.FromLTRB(intersection.Right, self.Top, self.Right, self.Bottom);
            var bottom = Rectangle.FromLTRB(
                intersection.Left, 
                intersection.Bottom, 
                intersection.Right, 
                self.Bottom);
            var top = Rectangle.FromLTRB(
                intersection.Left, 
                self.Top, 
                intersection.Right, 
                intersection.Top);

            if (left.Area() > 0)
            {
                yield return left;
            }

            if (right.Area() > 0)
            {
                yield return right;
            }

            if (top.Area() > 0)
            {
                yield return top;
            }

            if (bottom.Area() > 0)
            {
                yield return bottom;
            }
        }
    }
}
