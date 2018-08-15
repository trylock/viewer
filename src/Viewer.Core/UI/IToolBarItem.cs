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
}
