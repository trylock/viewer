using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace ViewerTheme
{
    public class FullFeaturedFloatWindow : FloatWindow
    {
        public FullFeaturedFloatWindow(DockPanel dockPanel, DockPane pane) : base(dockPanel, pane)
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowInTaskbar = true;
            Owner = null;
        }

        public FullFeaturedFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds) : base(dockPanel, pane, bounds)
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowInTaskbar = true;
            Owner = null;
        }
    }

    public class FullFeaturedFloatWindowFactory : DockPanelExtender.IFloatWindowFactory
    {
        public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane)
        {
            return new FullFeaturedFloatWindow(dockPanel, pane);
        }

        public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
        {
            return new FullFeaturedFloatWindow(dockPanel, pane, bounds);
        }
    }
}
