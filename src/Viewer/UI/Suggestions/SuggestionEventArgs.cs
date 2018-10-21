using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Suggestions
{
    internal class SuggestionEventArgs : EventArgs
    {
        public Suggestion Value { get; }

        public SuggestionEventArgs(Suggestion value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
