using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Suggestions
{
    internal class SuggestionEventArgs : EventArgs
    {
        public SuggestionItem Value { get; }

        public SuggestionEventArgs(SuggestionItem value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
