using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Query;

namespace Viewer.QueryRuntime
{
    /// <summary>
    /// DateTime function takes <see cref="StringValue"/> representation of date with or without
    /// time and produces <see cref="DateTimeValue"/>. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function expects this format: <c>yyyy-M-d H:m:s</c> (see list below). The time is
    /// assumed to be in the timezone of the local computer. Leading zeroes in month, day, hour,
    /// minute and second are optional (i.e., "2018-08-01 09-05-01" is the same as "2018-8-1 9-5-1").
    /// </para>
    /// <para>
    /// Moverover, a special string <c>now</c> (case insensitive) can be passed to the function and
    /// it returns current (local) time.
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///         <term>yyyy</term>
    ///         <description>the year (a 4 digit number)</description>
    ///     </item>
    ///     <item>
    ///         <term>M</term>
    ///         <description>the month (1 through 12)</description>
    ///     </item>
    ///     <item>
    ///         <term>d</term>
    ///         <description>the day of the month (1 through 31)</description>
    ///     </item>
    ///     <item>
    ///         <term>H</term>
    ///         <description>the hour (0 through 23)</description>
    ///     </item>
    ///     <item>
    ///         <term>m</term>
    ///         <description>the minute (0 through 59)</description>
    ///     </item>
    ///     <item>
    ///         <term>s</term>
    ///         <description>a second (0 through 59) </description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <c>datetime("2018-8-27 15:23:25")</c> returns date: 8th August 2018, 3:23 PM
    /// </example>
    [Export(typeof(IFunction))]
    public class DateTimeFunction : IFunction
    {
        public string Name => "DateTime";

        public IReadOnlyList<TypeId> Arguments => new[] { TypeId.String };
        
        private static readonly string[] Formats =
        {
            "yyyy-M-d H:m:s",
            "yyyy-M-d H:m", // seconds are optional
            "yyyy-M-d" // time is optional
        };

        public BaseValue Call(IExecutionContext arguments)
        {
            var date = arguments.Get<StringValue>(0);
            if (string.Equals("now", date.Value, StringComparison.OrdinalIgnoreCase))
            {
                return new DateTimeValue(DateTime.Now);
            }

            var isParsed = DateTime.TryParseExact(
                date.Value,
                Formats, 
                CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces, out var parsedDateTime);

            return isParsed ? new DateTimeValue(parsedDateTime) : new DateTimeValue(null);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// DateTime function will return its argument if the argument is a datetime value.
    /// </summary>
    [Export(typeof(IFunction))]
    public class DateTimeIdentityFunciton : IFunction
    {
        public string Name => "DateTime";

        public IReadOnlyList<TypeId> Arguments => new[] { TypeId.DateTime };

        public BaseValue Call(IExecutionContext arguments)
        {
            return arguments.Get<DateTimeValue>(0);
        }
    }

    [Export(typeof(IFunction))]
    public sealed class DateFunction : FunctionAlias<DateTimeFunction>
    {
        public override string Name => "date";
    }
}
