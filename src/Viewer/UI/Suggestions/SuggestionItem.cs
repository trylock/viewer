using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Suggestions
{
    internal class SuggestionItem
    {
        /// <summary>
        /// Text of the suggestion
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Category of the item (this will be shown to the right of the <see cref="Text"/>)
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// Suggestion data set by the user.
        /// </summary>
        public object UserData { get; set; }
    }
}
