using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.About
{
    [Export(typeof(IComponent))]
    public class AboutComponent : IComponent
    {
        public void OnStartup(IViewerApplication app)
        {
            app.AddMenuItem(new []{ "Help", "About" }, () =>
            {
                var form = new AboutForm();
                form.ShowDialog();
            }, null);
        }

        public IDockContent Deserialize(string persistString)
        {
            return null;
        }
    }
}
