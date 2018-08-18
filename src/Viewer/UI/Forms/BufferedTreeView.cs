using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI.Forms
{
    /// <inheritdoc />
    /// <summary>
    /// Buffered TreeView is a TreeView component with enabled double buffering. This eliminates
    /// flickering. This code has been downloaded from https://stackoverflow.com/a/10364283/1073673
    /// It is platform specific as it calls native win32 api.
    /// </summary>
    public class BufferedTreeView : TreeView
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            SendMessage(Handle, TVM_SETEXTENDEDSTYLE, (IntPtr)TVS_EX_DOUBLEBUFFER, (IntPtr)TVS_EX_DOUBLEBUFFER);
            base.OnHandleCreated(e);
        }

        // Pinvoke:
        private const int TVM_SETEXTENDEDSTYLE = 0x1100 + 44;
        private const int TVS_EX_DOUBLEBUFFER = 0x0004;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    }
}
