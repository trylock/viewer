using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace ViewerTheme
{
    internal class ViewerDockPaneStrip : DockPaneStripBase
    {
        public ViewerDockPaneStrip(DockPane pane) : base(pane)
        {
        }

        protected override int MeasureHeight()
        {
            throw new NotImplementedException();
        }

        protected override void EnsureTabVisible(IDockContent content)
        {
            throw new NotImplementedException();
        }

        protected override int HitTest(Point point)
        {
            throw new NotImplementedException();
        }

        public override GraphicsPath GetOutline(int index)
        {
            throw new NotImplementedException();
        }

        protected override Rectangle GetTabBounds(Tab tab)
        {
            throw new NotImplementedException();
        }
    }

    internal class ViewerDockPaneStripFactory : DockPanelExtender.IDockPaneStripFactory
    {
        public DockPaneStripBase CreateDockPaneStrip(DockPane pane)
        {
            return new ViewerDockPaneStrip(pane);
        }
    }
}
