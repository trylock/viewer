using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.About
{
    [Export(typeof(IComponent))]
    public class AboutComponent : Component
    {
        public override void OnStartup(IViewerApplication app)
        {
            app.AddMenuItem(new[] { "Help", "Documentation" }, () =>
            {
                Process.Start("https://trylock.github.io/viewer/articles/intro.html");
            }, null);

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
