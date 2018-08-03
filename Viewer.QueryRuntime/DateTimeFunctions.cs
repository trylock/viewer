using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Query;

namespace Viewer.QueryRuntime
{
    [Export(typeof(IFunction))]
    public class DateTimeFunction : IFunction
    {
        public string Name => "DateTime";

        public IReadOnlyList<TypeId> Arguments => new[] { TypeId.String };

        public BaseValue Call(IArgumentList arguments)
        {
            var date = arguments.Get<StringValue>(0);
            if (string.Equals("now", date.Value, StringComparison.OrdinalIgnoreCase))
            {
                return new DateTimeValue(DateTime.Now);
            }

            return DateTime.TryParse(date.Value, out var parsedDateTime) ? 
                new DateTimeValue(parsedDateTime) : 
                new DateTimeValue(null);
        }
    }
}
