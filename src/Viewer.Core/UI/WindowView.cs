using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.Core.UI
{
    public class WindowView : DockContent, IWindowView
    {
        public event EventHandler CloseView;
        public event EventHandler ViewGotFocus;
        
        public Keys ModifierKeyState => ModifierKeys;
        
        private bool IsAutoHide =>
            DockState == DockState.DockBottomAutoHide ||
            DockState == DockState.DockLeftAutoHide ||
            DockState == DockState.DockTopAutoHide ||
            DockState == DockState.DockRightAutoHide;
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // register event handlers
            FormClosed += OnFormClosed;
            GotFocus += OnGotFocus;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            HandleChangeTabShortcuts(keyData);
            HandleCloseTabShortcuts(keyData);
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void HandleCloseTabShortcuts(Keys keyData)
        {
            if (!keyData.HasFlag(Keys.Control) || !keyData.HasFlag(Keys.W))
            {
                return;
            }

            if (keyData.HasFlag(Keys.Shift)) // close all tabs in current pane
            {
                var windowsToClose = Pane.Contents.OfType<DockContent>().ToList();
                foreach (var content in windowsToClose)
                {
                    content.Close();
                }
            }
            else // close just this tab
            {
                Close();
            }
        }

        private void HandleChangeTabShortcuts(Keys keyData)
        {
            if (!keyData.HasFlag(Keys.Control) || !keyData.HasFlag(Keys.Tab))
            {
                return;
            }

            var contents = Pane.Contents;
            var index = contents.IndexOf(this);
            if (index >= 0)
            {
                Trace.Assert(contents.Count > 0);

                // select the next content index within this pane
                if (keyData.HasFlag(Keys.Shift)) // go to the previous tab
                {
                    --index;
                    if (index < 0)
                    {
                        index += contents.Count;
                    }
                }
                else // go to the next tab
                {
                    ++index;
                    if (index >= contents.Count)
                    {
                        index = 0;
                    }
                }

                // activate this content
                var content = contents[index] as DockContent;
                content?.Activate();
            }
        }

        public void EnsureVisible()
        {
            if (IsAutoHide)
            {
                DockPanel.ActiveAutoHideContent = this;
            }

            Activate();
        }

        public virtual void BeginLoading()
        {
        }

        public virtual void EndLoading()
        {
        }
        
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
