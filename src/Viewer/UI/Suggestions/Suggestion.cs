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

    /// <summary>
    /// Compare <see cref="Suggestion"/> based on its <see cref="Suggestion.UserData"/> property.
    /// </summary>
    /// <typeparam name="T">Type stored in <see cref="Suggestion.UserData"/></typeparam>
    internal class SuggestionComparer<T> : IComparer<Suggestion>, IEqualityComparer<Suggestion> 
        where T : class
    {
        private T GetItem(Suggestion suggestion)
        {
            return suggestion.UserData as T;
        }

        public int Compare(Suggestion x, Suggestion y)
        {
            return Comparer<T>.Default.Compare(GetItem(x), GetItem(y));
        }

        public bool Equals(Suggestion x, Suggestion y)
        {
            return EqualityComparer<T>.Default.Equals(GetItem(x), GetItem(y));
        }

        public int GetHashCode(Suggestion obj)
        {
            return EqualityComparer<T>.Default.GetHashCode(GetItem(obj));
        }
    }
}
