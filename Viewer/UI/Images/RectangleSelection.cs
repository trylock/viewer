using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Images
{
    public class RectangleSelection
    {
        /// <summary>
        /// First point of the selection
        /// </summary>
        public Point StartPoint { get; private set; }

        /// <summary>
        /// Is the selection currently active
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Start a new range selection at <paramref name="location"/>
        /// </summary>
        /// <param name="location"></param>
        public void StartSelection(Point location)
        {
            IsActive = true;
            StartPoint = location;
        }

        public void EndSelection()
        {
            IsActive = false;
        }

        /// <summary>
        /// Get selection bounds 
        /// </summary>
        /// <param name="endLocation"></param>
        /// <returns>Selection bounds if the selection is active or an empty rectangle</returns>
        public Rectangle GetBounds(Point endLocation)
        {
            if (!IsActive)
            {
                return Rectangle.Empty;
            }
            var minX = Math.Min(StartPoint.X, endLocation.X);
            var maxX = Math.Max(StartPoint.X, endLocation.X);
            var minY = Math.Min(StartPoint.Y, endLocation.Y);
            var maxY = Math.Max(StartPoint.Y, endLocation.Y);
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
