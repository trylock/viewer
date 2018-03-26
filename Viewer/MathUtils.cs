using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer
{
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
    }
}
