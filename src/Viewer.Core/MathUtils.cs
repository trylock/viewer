using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Core
{
    /// <summary>
    /// Utility functions on numbers.
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Divide <paramref name="num"/> with <paramref name="denom"/> and round the result up.
        /// I.e.: compute "ceil(num / denom)"
        /// </summary>
        /// <param name="num">Numerator</param>
        /// <param name="denom">Denominator</param>
        /// <returns>Result of the division rounded up</returns>
        public static int RoundUpDiv(int num, int denom)
        {
            if (num < 0 && denom < 0)
            {
                num = -num;
                denom = -denom;
            }

            if (num >= 0 && denom >= 0)
            {
                return (num + denom - 1) / denom;
            }

            // at this point, exactly one is negative so the divison will be correct
            return num / denom;
        }

        /// <summary>
        /// Compute linear interpolation between <paramref name="min"/> and <paramref name="max"/> using <paramref name="param"/> as a weight.
        /// </summary>
        /// <param name="min">Minimal value</param>
        /// <param name="max">Maximal value</param>
        /// <param name="param">Interpolation parameter</param>
        /// <returns>Interpolated value</returns>
        public static double Lerp(double min, double max, double param)
        {
            return min * (1.0 - param) + max * param;
        }

        /// <summary>
        /// Clamp value between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>
        ///     <paramref name="min"/> if <paramref name="value"/> is less than <paramref name="min"/>,
        ///     <paramref name="max"/> if <paramref name="value"/> is more than <paramref name="max"/>,
        ///     <paramref name="value"/> otherwise
        /// </returns>
        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
