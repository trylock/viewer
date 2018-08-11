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
    }
}
