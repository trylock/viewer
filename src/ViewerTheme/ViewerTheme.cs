using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2015;

namespace ViewerTheme
{
    public class ViewerLightTheme : VS2015LightTheme
    {
        public ViewerLightTheme()
        {
            Extender.WindowSplitterControlFactory = new WindowSplitterControlFactory();
            //Extender.DockPaneStripFactory = new ViewerDockPaneStripFactory();
        }
    }
}
