using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public interface IValueFormatter<in T> where T : BaseValue
    {
        /// <summary>
        /// Format <paramref name="value"/> to string using default culture.
        /// </summary>
        /// <param name="value">Value to foramt</param>
        /// <returns>Formatted value</returns>
        string Format(T value);

        /// <summary>
        /// Format <paramref name="value"/> to string using <paramref name="culture"/>.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="culture">Culture info about the output</param>
        /// <returns>Formatted value</returns>
        string Format(T value, CultureInfo culture);
    }
}
