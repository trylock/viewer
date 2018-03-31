using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI
{
    public abstract class WindowView : DockContent, IWindowView
    {
        public event EventHandler CloseView;
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // register event handlers
            FormClosed += OnFormClosed;
        }

        public void MakeActive()
        {
            Activate();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            CloseView?.Invoke(sender, e);
        }
    }
}
