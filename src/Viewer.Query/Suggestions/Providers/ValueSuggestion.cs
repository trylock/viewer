using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.Query.Suggestions.Providers
{
    /// <summary>
    /// Value suggestion is a <see cref="ReplaceSuggestion"/> which replaces caret token with a
    /// <see cref="BaseValue"/>. 
    /// </summary>
    public class ValueSuggestion : ReplaceSuggestion
    {
        private static string FormatValue(BaseValue value)
        {
            if (value is DateTimeValue)
            {
                return "date(" + value.ToString() + ")";
            }

            return value.ToString();
        }

        public ValueSuggestion(CaretToken caretToken, BaseValue value) 
            : base(caretToken,
                FormatValue(value), 
                value.ToString(CultureInfo.CurrentCulture), 
                value.Type.ToString())
        {
        }
    }
}
