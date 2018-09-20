using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    /// <summary>
    /// Compare <see cref="T:Viewer.Data.BaseValue" /> types.
    /// </summary>
    public class ValueComparer : IComparer<BaseValue>
    {
        /// <summary>
        /// Default instance of the value comparer
        /// </summary>
        public static ValueComparer Default { get; } = new ValueComparer();

        private static readonly NumberVisitor _numberVisitor = new NumberVisitor();
        private static readonly StringVisitor _stringVisitor = new StringVisitor();

        /// <summary>
        /// Convert value to a number or null
        /// </summary>
        private class NumberVisitor : IValueVisitor<double?>
        {
            public double? Visit(IntValue value)
            {
                return value.Value;
            }

            public double? Visit(RealValue value)
            {
                return value.Value;
            }

            public double? Visit(StringValue value)
            {
                if (double.TryParse(value.Value, out var numeric))
                {
                    return numeric;
                }
                return null;
            }

            public double? Visit(DateTimeValue value)
            {
                return null;
            }

            public double? Visit(ImageValue value)
            {
                return null;
            }
        }

        private class StringVisitor : IValueVisitor<string>
        {
            public string Visit(IntValue value)
            {
                return value.ToString();
            }

            public string Visit(RealValue value)
            {
                return value.ToString();
            }

            public string Visit(StringValue value)
            {
                return value.Value;
            }

            public string Visit(DateTimeValue value)
            {
                return value.ToString();
            }

            public string Visit(ImageValue value)
            {
                return value.ToString();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Compare 2 <see cref="BaseValue"/>s. See the following comparison algorithm. 
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///             If both values are null or <see cref="BaseValue.IsNull"/> is true, return 0
        ///         </description>
        ///         <description>
        ///             If <paramref name="x"/> is null or <paramref name="x"/>.<see cref="BaseValue.IsNull"/>
        ///             is true, return <c>1</c>.
        ///         </description>
        ///         <description>
        ///             If <paramref name="y"/> is null or <paramref name="y"/>.<see cref="BaseValue.IsNull"/>
        ///             is true, return <c>-1</c>.
        ///         </description>
        ///         <description>
        ///             If <paramref name="x"/> and <paramref name="y"/> are convertible to a number
        ///             (i.e., <see cref="IntValue"/> or <see cref="RealValue"/>), compare them as
        ///             <see cref="double"/>s.
        ///         </description>
        ///         <description>
        ///             If only <paramref name="x"/> is convertible to a number, return -1.
        ///         </description>
        ///         <description>
        ///             If only <paramref name="y"/> is convertible to a number, return 1.
        ///         </description>
        ///         <description>
        ///             If <paramref name="x"/> and <paramref name="y"/> are <see cref="DateTimeValue"/>s,
        ///             compare then as <see cref="DateTime"/>.
        ///         </description>
        ///         <description>
        ///             Otherwise, compare <paramref name="x"/> and <paramref name="y"/> as <see cref="string"/>s.
        ///         </description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(BaseValue x, BaseValue y)
        {
            // sort null values last
            var isXNull = x?.IsNull ?? true;
            var isYNull = y?.IsNull ?? true;
            if (isXNull && isYNull)
            {
                return 0;
            }
            else if (isXNull)
            {
                return 1;
            }
            else if (isYNull)
            {
                return -1;
            }

            // sort numbers (int, real) first
            var numberX = x.Accept(_numberVisitor);
            var numberY = y.Accept(_numberVisitor);
            if (numberX != null && numberY != null)
            {
                return Comparer<double>.Default.Compare((double)numberX, (double)numberY);
            }
            
            if (numberX != null)
            {
                return -1; // only x is a number
            }

            if (numberY != null)
            {
                return 1; // only y is a number
            }

            // sort dates 
            if (x is DateTimeValue xDate && y is DateTimeValue yDate)
            {
                return Comparer<DateTime>.Default.Compare(
                    (DateTime)xDate.Value, 
                    (DateTime)yDate.Value);
            }

            var xValue = x.Accept(_stringVisitor);
            var yValue = y.Accept(_stringVisitor);

            // sort strings (string, DateTime) 
            return Comparer<string>.Default.Compare(xValue, yValue);
        }
    }
}
