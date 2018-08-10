using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI
{
    public static class ValueFormat
    {
        private static readonly string[] UnitPrefix = { "", "k", "M", "G", "T" };

        private static string FormatValue(double value, int numberBase, string unit)
        {
            int order = 0;
            while (value >= numberBase && order - 1 < UnitPrefix.Length)
            {
                value /= numberBase;
                ++order;
            }

            return value.ToString("F2") + " " + UnitPrefix[order] + unit;
        }

        public static string FormatByteSize(long sizeInBytes)
        {
            return FormatValue(sizeInBytes, 1024, "B");
        }
    }
}
