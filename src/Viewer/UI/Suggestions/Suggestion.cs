using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Suggestions
{
    internal class Suggestion
    {
        /// <summary>
        /// Text of the suggestion
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Category of the item (this will be shown to the right of the <see cref="Text"/>)
        /// </summary>
        public string Category { get; }
        
        /// <summary>
        /// Suggestion data set by the user.
        /// </summary>
        public object UserData { get; }

        public Suggestion(string text, string category, object userData)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Category = category ?? throw new ArgumentNullException(nameof(category));
            UserData = userData;
        }
    }
}
