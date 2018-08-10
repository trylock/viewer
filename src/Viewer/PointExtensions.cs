using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer
{
    public static class PointExtensions
    {
        /// <summary>
        /// Compute squared distance of 2 points: <paramref name="source"/> and <paramref name="destination"/>
        /// </summary>
        /// <param name="source">Source point</param>
        /// <param name="destination">Destination point</param>
        /// <returns>Distance between the 2 points squared</returns>
        public static double DistanceSquaredTo(this Point source, Point destination)
        {
            var distX = source.X - destination.X;
            var distY = source.Y - destination.Y;
            return distX * distX + distY * distY;
        }
    }
}
