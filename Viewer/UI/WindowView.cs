using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI
{
    public class WindowView : DockContent, IWindowView
    {
        public event EventHandler CloseView;
        public event EventHandler ViewGotFocus;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // register event handlers
            FormClosed += OnFormClosed;
            GotFocus += OnGotFocus;
        }

        public void EnsureVisible()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(EnsureVisibleInternal));
            }
            else
            {
                EnsureVisibleInternal();
            }
        }

        private void EnsureVisibleInternal()
        {
            if (IsAutoHide)
            {
                DockPanel.ActiveAutoHideContent = this;
            }

            Activate();
        }

        private bool IsAutoHide => 
            DockState == DockState.DockBottomAutoHide ||
            DockState == DockState.DockLeftAutoHide ||
            DockState == DockState.DockTopAutoHide ||
            DockState == DockState.DockRightAutoHide;
        
        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            CloseView?.Invoke(sender, e);
        }

        private void OnGotFocus(object sender, EventArgs e)
        {
            ViewGotFocus?.Invoke(sender, e);
        }

    }
}
