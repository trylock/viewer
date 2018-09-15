using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2013;

namespace ViewerTheme
{
    internal class SplitterControl : VS2013WindowSplitterControl
    {
        public SplitterControl(ISplitterHost host) : base(host)
        {
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // The splitter is coverring other windows on high DPI settings (anything above
            // 100 %). 
            if (Dock == DockStyle.Right || Dock == DockStyle.Left)
                Width = SplitterSize;
            else if (Dock == DockStyle.Bottom || Dock == DockStyle.Top)
                Height = SplitterSize;
        }
    }

    internal class WindowSplitterControlFactory : DockPanelExtender.IWindowSplitterControlFactory
    {
        public SplitterBase CreateSplitterControl(ISplitterHost host)
        {
            return new SplitterControl(host);
        }
    }
}
