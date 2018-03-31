using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI
{
    public interface IWindowView
    {
        /// <summary>
        /// Event called after the view closed.
        /// Note: it is up to the view to define what does it mean for it to be closed
        ///       (i.e. it might just hide itself or it might actually close its form)
        /// </summary>
        event EventHandler CloseView;

        /// <summary>
        /// Make this window active
        /// </summary>
        void MakeActive();
    }
}
