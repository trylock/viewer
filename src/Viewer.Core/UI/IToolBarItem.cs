using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Core.UI
{
    public interface IToolBarItem
    {
        /// <summary>
        /// Icon of the tool shown to the user.
        /// </summary>
        Image Image { get; set; }

        /// <summary>
        /// Text shown to the user in a tooltip.
        /// </summary>
        string ToolTipText { get; set; }

        /// <summary>
        /// true iff user can use this tool at the moment.
        /// </summary>
        bool Enabled { get; set; }
    }

    public class SelectedEventArgs : EventArgs
    {
        /// <summary>
        /// Selected value
        /// </summary>
        public string Value { get; }
        
        public SelectedEventArgs(string value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// A tool bar item which lets users select one item from a predefined collection of items.
    /// </summary>
    public interface IToolBarDropDown : IToolBarItem
    {
        /// <summary>
        /// Event occurs whenever user selects an item from the drop down
        /// </summary>
        event EventHandler<SelectedEventArgs> ItemSelected;

        /// <summary>
        /// Items in the toolbar.
        /// Setting this to null wil throw an <see cref="ArgumentNullException"/>.
        /// </summary>
        ICollection<string> Items { get; set; }
    }
}
