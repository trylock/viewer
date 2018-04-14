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
        /// Event called when this view gets focus.
        /// </summary>
        event EventHandler ViewGotFocus;
        
        /// <summary>
        /// Make sure the window is visible to the user.
        /// Thread-safety: can be called from different threads
        /// </summary>
        void EnsureVisible();

        /// <summary>
        /// Execute <paramref name="method"/> on the thread of this view.
        /// </summary>
        /// <param name="method">Method to execute</param>
        /// <returns></returns>
        IAsyncResult BeginInvoke(Delegate method);
    }
}
